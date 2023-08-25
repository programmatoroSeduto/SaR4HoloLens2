
import psycopg2
from fastapi import status
from main_app.environment import environment
from main_app.api_logging.logging import log
from main_app.interfaces import db_interface
from .transaction_base import api_transaction_base
import json
from main_app.api_models import api_hl2_upload_request, api_hl2_upload_response, data_hl2_waypoint, data_hl2_path
from api_transactions.api_security_transactions.ud_security_support import ud_security_support




    


"""
input:
{
    'JSON_WAYPOINTS' : '',
    'DEVICE_ID' : '',
    'U_REFERENCE_POSITION_ID' : '',
    'SESSION_TOKEN_INHERITED_ID' : '',
    'ALIGNMENT_TUNING_THRESHOLD_VL' : 1.3,
    'ALIGNMENT_TUNING_TOLERANCE_VL' : 0.01,
    'ALIGNMENT_QUALITY_NEW_POINTS_A' : 1.00,
    'ALIGNMENT_QUALITY_NEW_POINTS_B' : 4.75
}
output:
    REQUEST_POSITION_ID, ALIGNED_POSITION_ID
"""
api_transaction_hl2_upload_sql_exec_waypoints = """
DROP TYPE IF EXISTS json_schema;
CREATE TYPE json_schema AS (
	pos_id int,
	area_id int,
	v vector(3),
	wp_timestamp TIMESTAMP
);
WITH 
request_data AS (
SELECT
    pos_id,
    area_id,
    v,
    wp_timestamp
FROM JSON_POPULATE_RECORDSET(NULL::json_schema,
%(JSON_WAYPOINTS)s)
) -- SELECT * FROM request_data;
, session_data AS (
SELECT 
	F_HL2_QUALITY_WAYPOINTS_PK AS align_with_fk,
	LOCAL_POSITION_ID AS pos_id,
	LOCAL_AREA_INDEX_ID AS area_id,
	to_vector3( UX_VL, UY_VL, UZ_VL )::vector(3) AS v,
	COALESCE(WAYPOINT_CREATED_TS, CREATED_TS) AS wp_timestamp
FROM sar.F_HL2_STAGING_WAYPOINTS
WHERE 1=1
AND NOT(ALIGNMENT_TYPE_FL)
AND U_REFERENCE_POSITION_ID = %(U_REFERENCE_POSITION_ID)s
AND (
	SESSION_TOKEN_INHERITED_ID IS NULL
	OR
	SESSION_TOKEN_INHERITED_ID = %(SESSION_TOKEN_INHERITED_ID)s
)
) -- SELECT * FROM session_data;
, cross_data AS (
SELECT 
	request_data.pos_id AS req_pos_id,
	session_data.pos_id AS loc_pos_id,
	( request_data.v <-> session_data.v ) AS dist,
	request_data.v AS req_v,
	session_data.v AS loc_v,
	session_data.align_with_fk AS align_with_fk,
	request_data.area_id AS req_area_id,
	session_data.area_id AS loc_area_id,
	request_data.wp_timestamp AS req_timestamp,
	session_data.wp_timestamp AS loc_timestamp
FROM request_data 
LEFT JOIN session_data ON (1=1)
) -- SELECT * FROM cross_data ORDER BY req_pos_id, dist, loc_pos_id;
, analysis_data AS (
SELECT 
	req_pos_id,
	loc_pos_id,
	align_with_fk,
	req_area_id,
	loc_area_id,
	req_v,
	loc_v,
	cross_data.dist AS dist,
	CASE 
		WHEN cross_data.dist IS NULL THEN FALSE
		WHEN cross_data.dist <= th.threshold_vl - th.toll THEN TRUE
		WHEN cross_data.dist >= th.threshold_vl + th.toll THEN FALSE 
		ELSE (RANDOM()>0.5)::BOOLEAN
	END AS WP_IS_REDUNDANT_FL,
	CASE 
		WHEN cross_data.dist IS NULL THEN 100.0
		WHEN cross_data.dist <= th.threshold_vl - th.toll 
			THEN ROUND(max_of( 
				((th.threshold_vl - cross_data.dist) / th.threshold_vl)::NUMERIC, 
				0::NUMERIC ) * 100, 2)
		WHEN cross_data.dist >= th.threshold_vl + th.toll
			THEN ROUND(max_of( 
				(1 - %(ALIGNMENT_QUALITY_NEW_POINTS_A)s*exp( %(ALIGNMENT_QUALITY_NEW_POINTS_B)s*(th.threshold_vl - cross_data.dist) ))::NUMERIC,
				0::NUMERIC ) * 100, 2)
		ELSE 0.001
	END AS QUALITY_VL,
	req_timestamp,
	loc_timestamp
FROM cross_data
LEFT JOIN ( SELECT 
	%(ALIGNMENT_TUNING_THRESHOLD_VL)s::FLOAT AS threshold_vl,
	%(ALIGNMENT_TUNING_TOLERANCE_VL)s::FLOAT AS toll 
) AS th ON (1=1)
) -- SELECT * FROM analysis_data ORDER BY req_pos_id, dist, loc_pos_id;
, classification_data AS (
SELECT DISTINCT
	req_pos_id,
	FIRST_VALUE(align_with_fk)
		OVER ( PARTITION BY req_pos_id ORDER BY dist ASC )
		AS align_with_fk,
	FIRST_VALUE(loc_pos_id)
		OVER ( PARTITION BY req_pos_id ORDER BY dist ASC )
		AS align_with_loc_pos_id,
	FIRST_VALUE(req_v)
		OVER ( PARTITION BY req_pos_id ORDER BY dist ASC )
		AS req_v,
	FIRST_VALUE(loc_v)
		OVER ( PARTITION BY req_pos_id ORDER BY dist ASC )
		AS loc_v,
	FIRST_VALUE(dist)
		OVER ( PARTITION BY req_pos_id ORDER BY dist ASC )
		AS dist,
	FIRST_VALUE(WP_IS_REDUNDANT_FL) 
		OVER ( PARTITION BY req_pos_id ORDER BY dist ASC )
		AS WP_IS_REDUNDANT_FL,
	FIRST_VALUE(QUALITY_VL)
		OVER ( PARTITION BY req_pos_id ORDER BY dist ASC )
		AS QUALITY_VL,
	FIRST_VALUE(req_timestamp)
		OVER ( PARTITION BY req_pos_id ORDER BY dist ASC )
		AS req_timestamp,
	FIRST_VALUE(loc_timestamp)
		OVER ( PARTITION BY req_pos_id ORDER BY dist ASC )
		AS loc_timestamp
FROM analysis_data
) -- SELECT * FROM classification_data;
, wps_renamings AS (
SELECT
	req_pos_id AS REQUEST_POSITION_ID,
	CASE 
		WHEN WP_IS_REDUNDANT_FL THEN align_with_loc_pos_id
		ELSE req_pos_id
	END AS ALIGNED_POSITION_ID
FROM classification_data
) -- SELECT * FROM wps_renamings;
, set_wps_new AS (
SELECT 
	%(DEVICE_ID)s AS DEVICE_ID,
	%(SESSION_TOKEN_ID)s AS SESSION_TOKEN_ID,
	%(SESSION_TOKEN_INHERITED_ID)s SESSION_TOKEN_INHERITED_ID,
	(max_session_id.max_id + ROW_NUMBER() OVER ()) AS LOCAL_POSITION_ID,
	req_pos_id AS REQUEST_POSITION_ID,
	%(U_REFERENCE_POSITION_ID)s AS U_REFERENCE_POSITION_ID,
	component_of(req_v, 1) AS UX_VL,
	component_of(req_v, 2) AS UY_VL,
	component_of(req_v, 3) AS UZ_VL,
	FALSE AS U_SOURCE_FROM_SERVER_FL,
	0 AS LOCAL_AREA_INDEX_ID,
	req_timestamp AS WAYPOINT_CREATED_TS,
	NULL::BIGINT AS ALIGNMENT_ALIGNED_WITH_WAYPOINT_FK,
	FALSE AS ALIGNMENT_TYPE_FL,
	QUALITY_VL AS ALIGNMENT_QUALITY_VL,
	dist AS ALIGNMENT_DISTANCE_VL,
	align_with_fk AS ALIGNMENT_DISTANCE_FROM_WAYPOINT_FK 
FROM classification_data
LEFT JOIN (
	SELECT 
		MAX(pos_id) AS max_id
	FROM session_data
) AS max_session_id ON (1=1)
WHERE NOT(WP_IS_REDUNDANT_FL)
) -- SELECT * FROM set_wps_new;
, set_wps_aligned AS (
SELECT 
	%(DEVICE_ID)s AS DEVICE_ID,
	%(SESSION_TOKEN_ID)s AS SESSION_TOKEN_ID,
	%(SESSION_TOKEN_INHERITED_ID)s SESSION_TOKEN_INHERITED_ID,
	align_with_loc_pos_id AS LOCAL_POSITION_ID,
	req_pos_id AS REQUEST_POSITION_ID,
	%(U_REFERENCE_POSITION_ID)s AS U_REFERENCE_POSITION_ID,
	component_of(loc_v, 1) AS UX_VL,
	component_of(loc_v, 2) AS UY_VL,
	component_of(loc_v, 3) AS UZ_VL,
	TRUE AS U_SOURCE_FROM_SERVER_FL,
	0 AS LOCAL_AREA_INDEX_ID,
	loc_timestamp AS WAYPOINT_CREATED_TS,
	align_with_fk AS ALIGNMENT_ALIGNED_WITH_WAYPOINT_FK,
	TRUE AS ALIGNMENT_TYPE_FL,
	QUALITY_VL AS ALIGNMENT_QUALITY_VL,
	dist AS ALIGNMENT_DISTANCE_VL,
	align_with_fk AS ALIGNMENT_DISTANCE_FROM_WAYPOINT_FK 
FROM classification_data
WHERE WP_IS_REDUNDANT_FL
AND align_with_loc_pos_id NOT IN (
	SELECT DISTINCT
		LOCAL_POSITION_ID
	FROM sar.F_HL2_STAGING_WAYPOINTS
	WHERE 1=1
	AND SESSION_TOKEN_ID = %(SESSION_TOKEN_ID)s
	AND U_REFERENCE_POSITION_ID = %(U_REFERENCE_POSITION_ID)s
)
) -- SELECT * FROM set_wps_new UNION ALL SELECT * FROM set_wps_aligned;
, insert_new AS (
INSERT INTO sar.F_HL2_STAGING_WAYPOINTS (DEVICE_ID, SESSION_TOKEN_ID, SESSION_TOKEN_INHERITED_ID, LOCAL_POSITION_ID, REQUEST_POSITION_ID, U_REFERENCE_POSITION_ID,UX_VL, UY_VL, UZ_VL, U_SOURCE_FROM_SERVER_FL, LOCAL_AREA_INDEX_ID,WAYPOINT_CREATED_TS, ALIGNMENT_ALIGNED_WITH_WAYPOINT_FK,ALIGNMENT_TYPE_FL, ALIGNMENT_QUALITY_VL, ALIGNMENT_DISTANCE_VL,ALIGNMENT_DISTANCE_FROM_WAYPOINT_FK)
SELECT * FROM set_wps_new
RETURNING *
)
, insert_history AS (
INSERT INTO sar.F_HL2_STAGING_WAYPOINTS (DEVICE_ID, SESSION_TOKEN_ID, SESSION_TOKEN_INHERITED_ID, LOCAL_POSITION_ID, REQUEST_POSITION_ID, U_REFERENCE_POSITION_ID,UX_VL, UY_VL, UZ_VL, U_SOURCE_FROM_SERVER_FL, LOCAL_AREA_INDEX_ID,WAYPOINT_CREATED_TS, ALIGNMENT_ALIGNED_WITH_WAYPOINT_FK,ALIGNMENT_TYPE_FL, ALIGNMENT_QUALITY_VL, ALIGNMENT_DISTANCE_VL,ALIGNMENT_DISTANCE_FROM_WAYPOINT_FK)
SELECT * FROM set_wps_aligned
RETURNING *
)
SELECT
	REQUEST_POSITION_ID,
	ALIGNED_POSITION_ID
FROM wps_renamings
WHERE REQUEST_POSITION_ID <> ALIGNED_POSITION_ID;
"""




    


"""
input:
{
    'JSON_PATHS' : '',
    'DEVICE_ID' : '',
    'U_REFERENCE_POSITION_ID' : '',
    'SESSION_TOKEN_INHERITED_ID' : ''
}
output:
    CREATED_PATHS
"""
api_transaction_hl2_upload_sql_exec_paths = """
DROP TYPE IF EXISTS json_schema;
CREATE TYPE json_schema AS (
	wp1 BIGINT,
	wp2 BIGINT,
	dist FLOAT,
	pt_timestamp TIMESTAMP
);
WITH 
request_data AS (
SELECT
    wp1,
    wp2,
    dist,
    pt_timestamp
FROM JSON_POPULATE_RECORDSET(NULL::json_schema,
%(JSON_PATHS)s)
) -- SELECT * FROM request_data;
, waypoints_base AS (
SELECT 
	*
FROM sar.F_HL2_STAGING_WAYPOINTS
WHERE 1=1
AND U_REFERENCE_POSITION_ID = %(U_REFERENCE_POSITION_ID)s
AND SESSION_TOKEN_ID = %(SESSION_TOKEN_ID)s
) -- SELECT * FROM waypoints_base;
, paths_renamed AS (
SELECT 
	wp1map.SESSION_TOKEN_ID AS SESSION_TOKEN_ID,
	wp1map.SESSION_TOKEN_INHERITED_ID AS SESSION_TOKEN_INHERITED_ID,
	wp1map.U_REFERENCE_POSITION_ID AS U_REFERENCE_POSITION_ID,
	wp1map.F_HL2_QUALITY_WAYPOINTS_PK AS WAYPOINT_1_STAGING_FK,
	wp2map.F_HL2_QUALITY_WAYPOINTS_PK AS WAYPOINT_2_STAGING_FK,
	COALESCE(
		request_data.dist,
		dist( wp1map.UX_VL, wp1map.UY_VL, wp1map.UZ_VL, wp2map.UX_VL, wp2map.UY_VL, wp2map.UZ_VL )
	) AS PATH_DISTANCE,
	COALESCE(
		request_data.pt_timestamp,
		CURRENT_TIMESTAMP
	) AS CREATED_TS
FROM request_data
LEFT JOIN waypoints_base 
	AS wp1map
	ON( request_data.wp1 = wp1map.REQUEST_POSITION_ID )
LEFT JOIN waypoints_base 
	AS wp2map
	ON( request_data.wp2 = wp2map.REQUEST_POSITION_ID )
) -- SELECT * FROM paths_renamed;
, paths_renamed_filtered AS (
SELECT 
	* 
FROM paths_renamed
WHERE ( WAYPOINT_1_STAGING_FK, WAYPOINT_2_STAGING_FK ) NOT IN (
	SELECT 
		WAYPOINT_1_STAGING_FK, WAYPOINT_2_STAGING_FK
	FROM sar.F_HL2_STAGING_PATHS
	WHERE 1=1
	AND SESSION_TOKEN_ID = %(SESSION_TOKEN_ID)s
	AND SESSION_TOKEN_INHERITED_ID = %(SESSION_TOKEN_INHERITED_ID)s
	AND U_REFERENCE_POSITION_ID = %(U_REFERENCE_POSITION_ID)s
	UNION ALL 
	SELECT 
		WAYPOINT_2_STAGING_FK, WAYPOINT_1_STAGING_FK
	FROM sar.F_HL2_STAGING_PATHS
	WHERE 1=1
	AND SESSION_TOKEN_ID = %(SESSION_TOKEN_ID)s
	AND SESSION_TOKEN_INHERITED_ID = %(SESSION_TOKEN_INHERITED_ID)s
	AND U_REFERENCE_POSITION_ID = %(U_REFERENCE_POSITION_ID)s
)
) -- SELECT * FROM paths_renamed_filtered;
, insert_step AS (
INSERT INTO sar.F_HL2_STAGING_PATHS (
	DEVICE_ID,
	SESSION_TOKEN_ID,
	SESSION_TOKEN_INHERITED_ID,
	U_REFERENCE_POSITION_ID,
	WAYPOINT_1_STAGING_FK,
	WAYPOINT_2_STAGING_FK,
	PATH_DISTANCE,
	CREATED_TS )
SELECT
	%(DEVICE_ID)s AS DEVICE_ID,
	*
FROM paths_renamed_filtered
RETURNING *
) -- SELECT * FROM insert_step;
SELECT COUNT(*) AS CREATED_PATHS FROM insert_step;
"""




    



api_transaction_hl2_upload_sql_exec_log = """
INSERT INTO sar.F_ACTIVITY_LOG (
    LOG_TYPE_DS, LOG_TYPE_ACCESS_FL, LOG_SUCCESS_FL, LOG_WARNING_FL, LOG_ERROR_FL, LOG_SECURITY_FAULT_FL,
    LOG_SOURCE_ID,
    LOG_DETAILS_DS,
    LOG_DATA
)
VALUES (
    %(LOG_TYPE_DS)s, %(LOG_TYPE_ACCESS_FL)s, %(LOG_SUCCESS_FL)s, %(LOG_WARNING_FL)s, false, %(LOG_SECURITY_FAULT_FL)s,
    'api',
    %(LOG_DETAILS_DS)s,
    %(LOG_DATA)s
)
"""




    



class api_transaction_hl2_upload(api_transaction_base):
    ''' HoloLens2 Upload transaction

    This transaction allows HoloLens2 to integrate its measurements
    inside the measures of the stagn area table. 
    '''

    def __init__(self, env: environment, request:api_hl2_upload_request) -> None:
        ''' Create the transaction
        
        '''
        super().__init__(env)
        
        # request comes from the API
        self.request:api_hl2_upload_request = request
        # response is built during check phase
        self.response:api_hl2_upload_response = None

        # CHECK response from the daabase
        self.__res:list = None
        self.__res_count:int = -1
        self.__res_schema:list = None

        # logging
        self.__log_detail_ds:str = ""
        self.__log_error:bool = False
        self.__log_unsecure_request:bool = False

        # transaction custom data
        self.security_handle:ud_security_support = ud_security_support(env)
        self.inherits_session = None
        self.tuning_threshold = 1.3
        self.tuning_tollerance = 0.01
        self.quality_a = 1.00
        self.quality_b = 4.75
        self.renamings_found = False
        self.renamings = list()




    



    def check( self ) -> None:
        ''' transaction check phase
        
        '''
        if self.request.based_on == "":
            self.__build_response(
                res_status=status.HTTP_400_BAD_REQUEST,
                res_status_description="bad request",
                log_detail='based_on in request cannot be empty'
            )
            return

        # fake session ID checks
        found, inherited, self.inherits_session = self.__check_session_exists()
        if not found:
            self.__build_response(
                res_status=status.HTTP_404_NOT_FOUND,
                res_status_description="not found",
                log_detail='session is missing in staging'
            )
            return
        found, _, fake_session_token, salt = self.security_handle.has_fake_token(
            self.request.user_id,
            self.request.device_id,
            self.request.session_token
        )
        if fake_session_token != self.request.based_on:
            self.__build_response(
                res_status=status.HTTP_400_BAD_REQUEST,
                res_status_description="bad request",
                log_detail='reference to a session, but using a unknown fake token',
                unsecure_request=True
            )
            return

        self.__build_response(
            res_status=status.HTTP_200_OK,
            res_status_description="success",
            log_detail=''
        )




    



    def execute( self ):
        ''' transaction execution phase
        
        '''
        if not self.__check_done:
            raise Exception("Missing CHECK step")
        
        try:
            if self.__log_error:
                self.__exec_fail()
            else:
                self.__exec_success()
        except Exception as e:
            self.log.err("Execution error during EXEC phase! {e}", src="aaaaaa")
            self.db.execute("ROLLBACK TRANSACTION;")




    



    def __exec_success( self ):
        global api_transaction_hl2_upload_sql_exec_waypoints
        global api_transaction_hl2_upload_sql_exec_paths

        cur = self.db.get_cursor()
        cur.execute("BEGIN TRANSACTION;")

        # upload waypoints
        # https://stackoverflow.com/questions/65622045/pydantic-convert-to-jsonable-dict-not-full-json-string
        self.renamings_found, self.renamings, _, _ = self.__extract_from_db(
            api_transaction_hl2_upload_sql_exec_waypoints,
            {
                'JSON_WAYPOINTS' : json.dumps([ json.loads(wp.model_dump_json()) for wp in self.request.waypoints ]),
                'DEVICE_ID' : self.request.device_id,
                'U_REFERENCE_POSITION_ID' : self.request.ref_id,
                'SESSION_TOKEN_INHERITED_ID' : self.inherits_session,
                'ALIGNMENT_TUNING_THRESHOLD_VL' : self.tuning_threshold,
                'ALIGNMENT_TUNING_TOLERANCE_VL' : self.tuning_tollerance,
                'ALIGNMENT_QUALITY_NEW_POINTS_A' : self.quality_a,
                'ALIGNMENT_QUALITY_NEW_POINTS_B' : self.quality_b
            }
        )

        # upload links
        _, _, _, _ = self.__extract_from_db(
            api_transaction_hl2_upload_sql_exec_paths,
            {
                'JSON_PATHS' : json.dumps([ json.loads(wp.model_dump_json()) for wp in self.request.paths ]),
                'DEVICE_ID' : self.request.device_id,
                'U_REFERENCE_POSITION_ID' : self.request.ref_id,
                'SESSION_TOKEN_INHERITED_ID' : self.inherits_session,
            }
        )

        # ... build response ...

        cur.execute("COMMIT TRANSACTION;")




    



    def __exec_fail( self ):
        global api_transaction_hl2_upload_sql_exec_log
        
        cur = self.db.get_cursor()
        cur.execute("BEGIN TRANSACTION;")

        cur.execute(
            api_transaction_hl2_upload_sql_exec_log,
            {
                'LOG_TYPE_DS' : 'hololens2 upload',
                'LOG_TYPE_ACCESS_FL' : False,
                'LOG_SUCCESS_FL' : False,
                'LOG_WARNING_FL' : False,
                'LOG_SECURITY_FAULT_FL' :  self.__log_unsecure_request,
                'LOG_DETAILS_DS' : self.__log_detail_ds,
                'LOG_DATA' : self.dict_to_field(dict(self.request)),
            }
        )

        cur.execute("COMMIT TRANSACTION;")




    



    def __check_session_exists( self ) -> (bool, bool, str):
        ''' check if the session exists and return its real inherited token if found
        
        '''
        found, data, _, _ = self.__extract_from_db(
            """
            SELECT 
                ( SESSION_TOKEN_INHERITED_ID IS NOT NULL )::BOOLEAN AS HAS_INHERITED_SESSION_FL,
                SESSION_TOKEN_INHERITED_ID
            FROM sar.F_HL2_STAGING_WAYPOINTS
            WHERE 1=1
            AND LOCAL_POSITION_ID = 0
            AND U_REFERENCE_POSITION_ID = %(U_REFERENCE_POSITION_ID)s
            AND SESSION_TOKEN_ID = %(SESSION_TOKEN_ID)s;
            """,
            {
                'U_REFERENCE_POSITION_ID' : self.request.ref_id,
                'SESSION_TOKEN_ID' : self.request.session_token
            }
        )

        if not found:
            return ( False, False, '' )
        else:
            return ( True, data[0]['HAS_INHERITED_SESSION_FL'], data[0]['SESSION_TOKEN_INHERITED_ID'] )




    


    
    def __build_response( self, 
        res_status:int, 
        res_status_description:str, 
        log_detail:str, 
        unsecure_request:bool=False 
    ) -> api_hl2_upload_response:
        self.__log_detail_ds = log_detail
        self.response = api_hl2_upload_response(
            timestamp_received = self.request.timestamp,
            status = res_status,
            status_detail = res_status_description,
        )
        self.__log_error = ( res_status not in ( 
            status.HTTP_200_OK, 
            status.HTTP_202_ACCEPTED, 
            status.HTTP_100_CONTINUE 
            ) )
        self.__log_unsecure_request = unsecure_request

        self.__check_done = True
        return self.response




    



    def __extract_from_db(self, query, query_data):
        ''' results as a dictionary
        
        RETURNS
            ( is res not empty?, res_data, res_schema, res_count )
        '''
        cur = self.db.get_cursor()
        cur.execute(query, query_data)
        res_data_raw = cur.fetchall()
        res_schema = [ str(col.name).upper() for col in cur.description ]
        res_count = cur.rowcount

        if res_count == 0:
            return ( False, None, list(), 0 )
        
        res_data = list()
        for row in res_data_raw:
            res_data.append(self.to_dict(res_schema, row))
        
        return ( True, res_data, res_schema, res_count )