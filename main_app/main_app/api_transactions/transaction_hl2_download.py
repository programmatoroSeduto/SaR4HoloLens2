
import psycopg2
from fastapi import status
from main_app.environment import environment
from main_app.api_logging.logging import log
from main_app.interfaces import db_interface
from .transaction_base import api_transaction_base
from main_app.api_models import api_hl2_download_request, api_hl2_download_response, data_hl2_waypoint, data_hl2_path
import json
from api_transactions.api_security_transactions.ud_security_support import ud_security_support




api_transaction_hl2_download_sql_exec_get_waypoints = """
DROP TYPE IF EXISTS json_schema;
CREATE TYPE json_schema AS (
	local_id int
);
WITH all_points AS (
SELECT 
ROW_NUMBER() OVER ( PARTITION BY LOCAL_POSITION_ID ORDER BY PREFERRED_FL DESC, F_HL2_QUALITY_WAYPOINTS_PK DESC ) AS rowno,
*
FROM (
    SELECT
    CASE
        WHEN SESSION_TOKEN_ID = %(SESSION_TOKEN_ID)s THEN 1
        ELSE 0
    END AS PREFERRED_FL,
    *
    FROM sar.F_HL2_STAGING_WAYPOINTS
    ) AS tab
WHERE 1=1
AND NOT(ALIGNMENT_TYPE_FL)
AND U_REFERENCE_POSITION_ID = %(U_REFERENCE_POSITION_ID)s
AND ( 
	SESSION_TOKEN_INHERITED_ID IS NULL 
	OR 
	SESSION_TOKEN_INHERITED_ID = %(SESSION_TOKEN_INHERITED_ID)s
	)
) -- SELECT * FROM all_points;
, excluded_from_paths_analysis AS (
SELECT DISTINCT
	local_id AS LOCAL_POSITION_ID
FROM JSON_POPULATE_RECORDSET(NULL::json_schema,
%(JSON_EXCLUDE_WAYPOINTS)s
)
)
, known_wps AS (
SELECT DISTINCT  
	LOCAL_POSITION_ID
FROM sar.F_HL2_STAGING_WAYPOINTS
WHERE 1=1
AND U_REFERENCE_POSITION_ID = %(U_REFERENCE_POSITION_ID)s
AND SESSION_TOKEN_ID = %(SESSION_TOKEN_ID)s
) -- SELECT * FROM known_wps;
, wps_set_to_return AS (
SELECT DISTINCT
    *
FROM all_points
WHERE rowno = 1
AND dist( UX_VL, UY_VL, UZ_VL, %(POS_X)s, %(POS_Y)s, %(POS_Z)s ) <= %(RADIUS)s
AND SESSION_TOKEN_ID <> %(SESSION_TOKEN_ID)s
AND LOCAL_POSITION_ID NOT IN ( SELECT * FROM known_wps )
AND LOCAL_POSITION_ID NOT IN ( SELECT * FROM excluded_from_paths_analysis )
) -- SELECT * FROM wps_set_to_return;
, selected_paths AS ( 
SELECT DISTINCT
    WAYPOINT_1_STAGING_FK,
    wp1s.LOCAL_POSITION_ID AS LOCAL_POSITION_1_ID,
    wp1s.UX_VL AS UX1_VL, wp1s.UY_VL AS UY1_VL, wp1s.UZ_VL AS UZ1_VL,
    WAYPOINT_2_STAGING_FK,
    wp2s.LOCAL_POSITION_ID AS LOCAL_POSITION_2_ID,
    wp2s.UX_VL AS UX2_VL, wp2s.UY_VL AS UY2_VL, wp2s.UZ_VL AS UZ2_VL,
    COALESCE(pth.PATH_DISTANCE, dist( 
        wp1s.UX_VL, wp1s.UY_VL, wp1s.UZ_VL, 
        wp2s.UX_VL, wp2s.UY_VL, wp2s.UZ_VL )) AS PATH_DISTANCE
FROM sar.F_HL2_STAGING_PATHS
	AS pth
LEFT JOIN all_points
	AS wp1s
	ON ( wp1s.F_HL2_QUALITY_WAYPOINTS_PK = pth.WAYPOINT_1_STAGING_FK )
LEFT JOIN all_points
	AS wp2s
	ON ( wp2s.F_HL2_QUALITY_WAYPOINTS_PK = pth.WAYPOINT_2_STAGING_FK )
WHERE 1=1
AND wp1s.LOCAL_POSITION_ID IN (SELECT DISTINCT LOCAL_POSITION_ID FROM wps_set_to_return)
AND wp2s.LOCAL_POSITION_ID IN (SELECT DISTINCT LOCAL_POSITION_ID FROM wps_set_to_return)
) -- SELECT * FROM selected_paths;
, waypoints_filtered_by_path AS (
SELECT DISTINCT 
	tab.F_HL2_QUALITY_WAYPOINTS_PK,
	tab.LOCAL_POSITION_ID,
	tab.UX_VL, tab.UY_VL, tab.UZ_VL,
	wps_set_to_return.CREATED_TS
FROM (
SELECT 
	WAYPOINT_1_STAGING_FK AS F_HL2_QUALITY_WAYPOINTS_PK,
	LOCAL_POSITION_1_ID AS LOCAL_POSITION_ID,
	UX1_VL AS UX_VL, UY1_VL AS UY_VL, UZ1_VL AS UZ_VL
FROM selected_paths
UNION 
SELECT 
	WAYPOINT_2_STAGING_FK AS F_HL2_QUALITY_WAYPOINTS_PK,
	LOCAL_POSITION_2_ID AS LOCAL_POSITION_ID,
	UX2_VL AS UX_VL, UY2_VL AS UY_VL, UZ2_VL AS UZ_VL
FROM selected_paths
) AS tab 
JOIN wps_set_to_return
	ON ( tab.F_HL2_QUALITY_WAYPOINTS_PK = wps_set_to_return.F_HL2_QUALITY_WAYPOINTS_PK )
WHERE tab.LOCAL_POSITION_ID <> 0
) -- SELECT DISTINCT * FROM waypoints_filtered_by_path;
, wps_final_set_to_return AS (
SELECT DISTINCT
    %(DEVICE_ID)s AS DEVICE_ID,
    %(SESSION_TOKEN_ID)s AS SESSION_TOKEN_ID,
    %(SESSION_TOKEN_INHERITED_ID)s AS SESSION_TOKEN_INHERITED_ID,
    %(U_REFERENCE_POSITION_ID)s AS U_REFERENCE_POSITION_ID,
    LOCAL_POSITION_ID,
    UX_VL, UY_VL, UZ_VL,
    CREATED_TS AS WAYPOINT_CREATED_TS,
    F_HL2_QUALITY_WAYPOINTS_PK AS ALIGNMENT_ALIGNED_WITH_WAYPOINT_FK,
    TRUE AS ALIGNMENT_TYPE_FL,
    100 AS ALIGNMENT_QUALITY_VL,
    0.0 AS ALIGNMENT_DISTANCE_VL,
    F_HL2_QUALITY_WAYPOINTS_PK AS ALIGNMENT_DISTANCE_FROM_WAYPOINT_FK,
    TRUE AS U_SOURCE_FROM_SERVER_FL,
    LOCAL_POSITION_ID AS REQUEST_POSITION_ID
FROM waypoints_filtered_by_path
) -- SELECT * FROM wps_final_set_to_return;
, insert_step AS (
INSERT INTO sar.F_HL2_STAGING_WAYPOINTS (
    DEVICE_ID, SESSION_TOKEN_ID, SESSION_TOKEN_INHERITED_ID, U_REFERENCE_POSITION_ID,
    LOCAL_POSITION_ID, UX_VL, UY_VL, UZ_VL, WAYPOINT_CREATED_TS, ALIGNMENT_ALIGNED_WITH_WAYPOINT_FK,
    ALIGNMENT_TYPE_FL, ALIGNMENT_QUALITY_VL, ALIGNMENT_DISTANCE_VL, 
    ALIGNMENT_DISTANCE_FROM_WAYPOINT_FK, U_SOURCE_FROM_SERVER_FL, REQUEST_POSITION_ID
)
SELECT * FROM wps_final_set_to_return 
RETURNING
    *
)
SELECT
	F_HL2_QUALITY_WAYPOINTS_PK ,
	LOCAL_POSITION_ID ,
	UX_VL, UY_VL, UZ_VL,
	WAYPOINT_CREATED_TS
FROM insert_step
;
"""




api_transaction_hl2_download_sql_exec_get_max_id = """
SELECT 
    MAX(LOCAL_POSITION_ID) AS MAX_LOCAL_POSITION_ID
FROM sar.F_HL2_STAGING_WAYPOINTS
WHERE 1=1
AND NOT(ALIGNMENT_TYPE_FL)
AND U_REFERENCE_POSITION_ID = %(U_REFERENCE_POSITION_ID)s
AND ( 
	SESSION_TOKEN_INHERITED_ID IS NULL 
	OR 
	SESSION_TOKEN_INHERITED_ID = %(SESSION_TOKEN_INHERITED_ID)s
	)
"""




api_transaction_hl2_download_sql_exec_get_paths = """
WITH all_points AS (
SELECT 
ROW_NUMBER() OVER ( PARTITION BY LOCAL_POSITION_ID ORDER BY PREFERRED_FL DESC, F_HL2_QUALITY_WAYPOINTS_PK DESC ) AS rowno,
*
FROM (
    SELECT
    CASE
        WHEN SESSION_TOKEN_ID = %(SESSION_TOKEN_ID)s THEN 1
        ELSE 0
    END AS PREFERRED_FL,
    *
    FROM sar.F_HL2_STAGING_WAYPOINTS
    ) AS tab
WHERE 1=1
AND NOT(ALIGNMENT_TYPE_FL)
AND U_REFERENCE_POSITION_ID = %(U_REFERENCE_POSITION_ID)s
AND ( 
	SESSION_TOKEN_INHERITED_ID IS NULL 
	OR 
	SESSION_TOKEN_INHERITED_ID = %(SESSION_TOKEN_INHERITED_ID)s
	)
) -- SELECT * FROM all_points;
, known_wps AS (
SELECT DISTINCT  
	LOCAL_POSITION_ID
FROM sar.F_HL2_STAGING_WAYPOINTS
WHERE 1=1
AND U_REFERENCE_POSITION_ID = %(U_REFERENCE_POSITION_ID)s
AND SESSION_TOKEN_ID = %(SESSION_TOKEN_ID)s
-- AND LOCAL_POSITION_ID <> 0
) -- SELECT * FROM known_wps;
, wps_set_to_return AS (
SELECT DISTINCT
    *
FROM all_points
WHERE rowno = 1
AND dist( UX_VL, UY_VL, UZ_VL, %(POS_X)s, %(POS_Y)s, %(POS_Z)s ) <= %(RADIUS)s
AND SESSION_TOKEN_ID <> %(SESSION_TOKEN_ID)s
AND LOCAL_POSITION_ID NOT IN ( SELECT * FROM known_wps )
) -- SELECT * FROM wps_set_to_return;
, nearest_point AS (
SELECT
    LOCAL_POSITION_ID,
    UX_VL, UY_VL, UZ_VL
FROM wps_set_to_return
ORDER BY dist( UX_VL, UY_VL, UZ_VL, %(POS_X)s, %(POS_Y)s, %(POS_Z)s ) ASC
LIMIT 1
)
, selected_paths AS ( 
SELECT DISTINCT
    WAYPOINT_1_STAGING_FK,
    wp1s.LOCAL_POSITION_ID AS LOCAL_POSITION_1_ID,
    dist( 
        wp1s.UX_VL, wp1s.UY_VL, wp1s.UZ_VL, 
        %(POS_X)s, %(POS_Y)s, %(POS_Z)s )::NUMERIC AS WP1_DIST,
    WAYPOINT_2_STAGING_FK,
    wp2s.LOCAL_POSITION_ID AS LOCAL_POSITION_2_ID,
    dist( 
        wp2s.UX_VL, wp2s.UY_VL, wp2s.UZ_VL, 
        %(POS_X)s, %(POS_Y)s, %(POS_Z)s )::NUMERIC AS WP2_DIST,
    COALESCE(pth.PATH_DISTANCE, dist( 
        wp1s.UX_VL, wp1s.UY_VL, wp1s.UZ_VL, 
        wp2s.UX_VL, wp2s.UY_VL, wp2s.UZ_VL )) AS PATH_DISTANCE,
    pth.CREATED_TS
FROM sar.F_HL2_STAGING_PATHS
	AS pth
LEFT JOIN all_points
	AS wp1s
	ON ( wp1s.F_HL2_QUALITY_WAYPOINTS_PK = pth.WAYPOINT_1_STAGING_FK )
LEFT JOIN all_points
	AS wp2s
	ON ( wp2s.F_HL2_QUALITY_WAYPOINTS_PK = pth.WAYPOINT_2_STAGING_FK )
WHERE 1=1
AND wp1s.LOCAL_POSITION_ID IN (SELECT DISTINCT LOCAL_POSITION_ID FROM wps_set_to_return)
AND wp2s.LOCAL_POSITION_ID IN (SELECT DISTINCT LOCAL_POSITION_ID FROM wps_set_to_return)
) 
SELECT
    pth.WAYPOINT_1_STAGING_FK,
    pth.LOCAL_POSITION_1_ID,
    pth.WAYPOINT_2_STAGING_FK,
    pth.LOCAL_POSITION_2_ID,
    mypos.LOCAL_POSITION_ID AS USER_NEAREST_POSITION,
    min_of(pth.WP1_DIST, pth.WP2_DIST) AS MIN_DIST,
    PATH_DISTANCE,
    pth.CREATED_TS
FROM selected_paths
    AS pth
LEFT JOIN nearest_point 
    AS mypos
    ON (1=1)
ORDER BY MIN_DIST
;
"""




api_transaction_hl2_download_sql_exec_log = """
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





class api_transaction_hl2_download(api_transaction_base):
    ''' HL2 Integration - Download Transaction

    '''

    def __init__(self, env: environment, request) -> None:
        ''' Create the transaction
        
        '''
        super().__init__(env)
        
        # request comes from the API
        self.request:api_hl2_download_request = request
        # response is built during check phase
        self.response:api_hl2_download_response = None

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
        self.device_is_calibrating = False
        self.device_is_calibrating_verified = False
        
        # in case based_on="", the inherited real is assigned by the class
        #    depending on the inheritable sessions found for that refpoint
        self.inherited_session_fake = None # from request or generated, None if the session doesn't inherit
        self.inherited_session_real = None # from security check upon request or found by the transaction, None if the session doesn't inherit
        self.inheritable_session_staging = None # assigned in __get_staging_infos(), None if the session doesn't inherit
        self.inherited_origin_pk = None # None if the session is the real first session
        self.staging_session_exists = False # assigned in __get_staging_infos(), it says if there are rows with this session token as owner
        self.max_id = 0 # assigned by __exec_success(), it is zero at least

        # daa from database
        self.wps_raw = None
        self.pth_raw = None
    


    def check( self ) -> None:
        ''' transaction check phase
        
        '''

        # check "declared" inherited session
        if self.request.based_on == "":
            self.device_is_calibrating = True # supposed
            self.inherited_session_fake = None
            self.inherited_session_real = None
            self.inheritable_session_staging = None

            found_fake_token, _, _, _ = self.security_handle.has_fake_token(
                self.request.user_id,
                self.request.device_id,
                self.request.session_token )
            if found_fake_token:
                self.__build_response(
                    res_status=status.HTTP_401_UNAUTHORIZED,
                    res_status_description="unauthorized",
                    log_detail='user declared to not have a token, but the user has one token currently active',
                    unsecure_request=True
                )
                return
            else:
                self.device_is_calibrating_verified = True

        else:
            self.device_is_calibrating = False
            self.inherited_session_fake = self.request.based_on

            token_found, is_not_inherited_token, self.inherited_session_real = self.security_handle.try_get_real_token_from_fake(
                self.request.user_id,
                self.request.device_id,
                self.request.session_token,
                self.inherited_session_fake )
            
            if not token_found:
                self.__build_response(
                    res_status=status.HTTP_401_UNAUTHORIZED,
                    res_status_description="unauthorized",
                    log_detail='cannot find real token from rovided fake token',
                    unsecure_request=True
                )
                return
            elif not is_not_inherited_token:
                self.inheritable_session_staging = self.inherited_session_real
            else:
                self.inheritable_session_staging = None
        
        # session exists in staging? if not, try to find a inheritable session
        self.__get_staging_infos()

        # create session if needed
        if self.device_is_calibrating_verified:
            self.__create_staging_session()
        
        self.__build_response(
            res_status=status.HTTP_202_ACCEPTED,
            res_status_description="success",
            log_detail='Download data from HoloLens2 device OK'
        )



    def execute( self ):
        ''' transaction execution phase
        
        '''
        if not self.__check_done:
            raise Exception("Missing CHECK step")
        
        '''
        try:
            if self.__log_error:
                self.__exec_fail()
            else:
                self.__exec_success()
        except Exception as e:
            self.log.err("Execution error during EXEC phase! {e}", src="transaction_hl2_download:execute")
            self.db.get_cursor().execute("ROLLBACK TRANSACTION;")
        '''
        if self.__log_error:
            self.__exec_fail()
        else:
            self.__exec_success()
    


    def __exec_success( self ):
        global api_transaction_hl2_download_sql_exec_get_waypoints
        global api_transaction_hl2_download_sql_exec_get_paths
        global api_transaction_hl2_download_sql_exec_log
        # global api_transaction_hl2_download_sql_exec_recall_waypoints

        cur = self.db.get_cursor()
        # cur.execute("BEGIN TRANSACTION;")

        req_dict = {
            'U_REFERENCE_POSITION_ID' : self.request.ref_id,
            'SESSION_TOKEN_INHERITED_ID' : ( self.inherited_session_real or '' ),
            'SESSION_TOKEN_ID' : self.request.session_token,
            'POS_X' : ( 0.0 if self.device_is_calibrating_verified else self.request.center[0] ),
            'POS_Y' : ( 0.0 if self.device_is_calibrating_verified else self.request.center[1] ),
            'POS_Z' : ( 0.0 if self.device_is_calibrating_verified else self.request.center[2] ),
            'RADIUS' : self.request.radius,
            'DEVICE_ID' : self.request.device_id
        }
        # self.log.debug_detail(f"req_dict:{req_dict}", src="transaction Download")

        self.response.ref_id = self.request.ref_id
        if self.device_is_calibrating_verified:
            self.response.based_on = self.inherited_session_fake or ""
        else:
            self.response.based_on = ""

        # extract paths
        self.log.debug_detail(f"download paths query:\n{api_transaction_hl2_download_sql_exec_get_paths % req_dict}", src="transaction Download")
        paths_found, self.pth_raw, _, _ = self.__extract_from_db( 
            api_transaction_hl2_download_sql_exec_get_paths,
            req_dict )
        if not paths_found:
            self.pth_raw = list()

        if paths_found: 

            # find connected sets inside the results
            excluded_waypoints = self.__paths_analysis(self.pth_raw)
            self.log.debug_detail(f"fund waypoints to exclude:\n\t{excluded_waypoints}",  src="transaction Download")
            req_dict['JSON_EXCLUDE_WAYPOINTS'] = json.dumps([ {'local_id':id} for id in excluded_waypoints ])

            # extract waypoints
            self.log.debug_detail(f"download waypoints query:\n{api_transaction_hl2_download_sql_exec_get_waypoints % req_dict}", src="transaction Download")
            found, self.wps_raw, _, _ = self.__extract_from_db( 
                api_transaction_hl2_download_sql_exec_get_waypoints,
                req_dict )
            
            # get max of local IDs
            _, data, _, _ = self.__extract_from_db( 
                api_transaction_hl2_download_sql_exec_get_max_id,
                {
                    'U_REFERENCE_POSITION_ID' : req_dict['U_REFERENCE_POSITION_ID'],
                    'SESSION_TOKEN_INHERITED_ID' : req_dict['SESSION_TOKEN_INHERITED_ID']
                }
            )
            self.response.max_idx = data[0]['MAX_LOCAL_POSITION_ID']

            # build the response
            for wp in self.wps_raw:
                self.response.waypoints.append(data_hl2_waypoint(
                    pos_id = wp['LOCAL_POSITION_ID'],
                    area_id = 0,
                    v = ( wp['UX_VL'], wp['UY_VL'], wp['UZ_VL'] ),
                    wp_timestamp = wp['WAYPOINT_CREATED_TS']
                ))
            for pth in self.pth_raw:
                self.response.paths.append(data_hl2_path(
                    wp1 = pth['LOCAL_POSITION_1_ID'],
                    wp2 = pth['LOCAL_POSITION_2_ID'],
                    dist = pth['PATH_DISTANCE'],
                    pt_timestamp = pth['CREATED_TS']
                ))

        # logging
        cur.execute(
            api_transaction_hl2_download_sql_exec_log,
            {
                'LOG_TYPE_DS' : 'hl2 download',
                'LOG_TYPE_ACCESS_FL' : False,
                'LOG_SUCCESS_FL' : True,
                'LOG_WARNING_FL' : False,
                'LOG_SECURITY_FAULT_FL' : False,
                'LOG_DETAILS_DS' : self.__log_detail_ds,
                'LOG_DATA' : self.dict_to_field(dict(self.request)),
            }
        )

        # cur.execute("COMMIT TRANSACTION;")
    


    def __exec_fail( self ):
        global api_transaction_hl2_download_sql_exec_log
        
        cur = self.db.get_cursor()
        # cur.execute("BEGIN TRANSACTION;")

        cur.execute(
            api_transaction_hl2_download_sql_exec_log,
            {
                'LOG_TYPE_DS' : 'hl2 download',
                'LOG_TYPE_ACCESS_FL' : False,
                'LOG_SUCCESS_FL' : False,
                'LOG_WARNING_FL' : False,
                'LOG_SECURITY_FAULT_FL' :  self.__log_unsecure_request,
                'LOG_DETAILS_DS' : self.__log_detail_ds,
                'LOG_DATA' : self.dict_to_field(dict(self.request)),
            }
        )

        # cur.execute("COMMIT TRANSACTION;")
    


    def __paths_analysis(self, paths:list) -> set:
        self.log.debug_detail(f"START PATHS ANALYSIS WITH paths:\n\t{paths}", src="__paths_analysis")
        current_pos = paths[0]['USER_NEAREST_POSITION']
        self.log.debug_detail(f"current pos is ID:{current_pos}", src="__paths_analysis")
        wp_set = set()
        wp_paths = set()

        self.log.debug_detail(f"DISCOVERING PATHS AND WAYPOINTS", src="__paths_analysis")
        for row in paths:
            if row['LOCAL_POSITION_1_ID'] not in wp_set:
                id = row['LOCAL_POSITION_1_ID']
                self.log.debug_detail(f"(wp1) found ID:{id}", src="__paths_analysis")
                wp_set.add(id)
            if row['LOCAL_POSITION_2_ID'] not in wp_set:
                id = row['LOCAL_POSITION_2_ID']
                self.log.debug_detail(f"(wp2) found ID:{id}", src="__paths_analysis")
                wp_set.add(id)
            if row['LOCAL_POSITION_1_ID'] in wp_set and row['LOCAL_POSITION_2_ID'] in wp_set:
                tup12 = ( row['LOCAL_POSITION_1_ID'], row['LOCAL_POSITION_2_ID'] )
                tup21 = ( tup12[1], tup12[0] )
                self.log.debug_detail(f"(wp2) found PATH:{tup12}/{tup21}", src="__paths_analysis")
                wp_paths.add( tup12 )
                wp_paths.add( tup21 )
        
        self.log.debug_detail(f"ITERATING OVER PATHS", src="__paths_analysis")
        wp_set, _ = self.__iterate_over_paths(current_pos, wp_set, wp_paths, iteration=1)
        self.log.debug_detail(f"END PATHS ANALYSIS", src="__paths_analysis")
        return wp_set
        
    def __iterate_over_paths(self, current_pos:int, wp_set:set, wp_paths:set, iteration) -> (set, set):
        self.log.debug_detail(f"BEGIN ITERATION {iteration} WITH\n\tcurrent_pos:{current_pos}\n\twp_set:{wp_set}\n\twp_paths:{wp_paths}", src="__iterate_over_paths")
        found_wp_set = set()
        for wp in wp_set:
            tup = ( current_pos, wp )
            if tup in wp_paths:
                found_wp_set.add(wp)
                wp_paths.remove(tup)
                wp_paths.remove((tup[1], tup[0]))
        if len(found_wp_set) == 0:
            self.log.debug_detail(f"END ITERATION {iteration} (no paths found) WITH\n\tcurrent_pos:{current_pos}\n\twp_set:{wp_set}\n\twp_paths:{wp_paths}", src="__iterate_over_paths")
            return wp_set, wp_paths

        found_wp_set.add(current_pos)
        for wp in found_wp_set:
            try:
                wp_set.remove(wp)
            except Exception as e:
                self.log.debug_detail(f"current pos ID:{wp} is not in set", src="__iterate_over_paths")
        for wp in found_wp_set:
            wp_set, wp_paths = self.__iterate_over_paths(wp, wp_set, wp_paths, iteration+1)
        
        self.log.debug_detail(f"END ITERATION {iteration} WITH\n\tcurrent_pos:{current_pos}\n\twp_set:{wp_set}\n\twp_paths:{wp_paths}", src="__iterate_over_paths")
        return wp_set, wp_paths

        
            



    def __build_response( self, 
        res_status:int, 
        res_status_description:str, 
        log_detail:str, 
        unsecure_request:bool=False 
    ) -> api_hl2_download_response:
        self.__log_detail_ds = log_detail
        self.response = api_hl2_download_response(
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



    def __get_staging_infos(self) -> None:
        ''' Get preliminary informations from the staging table
        
        1. does my session exist in staging?
        2. (calibration only) does it exists a session I can inherit? 
        3. (not calibrating) collect all the IDs already given to the device
        4. (in any case) get the PK of the origin of the inherited origin position
        '''

        # does my session exist in staging?
        _, data, _, _ = self.__extract_from_db( # does my session exist in staging?
            """
            SELECT 
            (COUNT(*) > 0)::BOOLEAN AS EXISTS_FL
            FROM sar.F_HL2_STAGING_WAYPOINTS
            WHERE SESSION_TOKEN_ID = %(SESSION_TOKEN_ID)s;
            """,
            {
                'SESSION_TOKEN_ID' : self.request.session_token
            }
        )
        self.staging_session_exists = data[0]['EXISTS_FL']


        # (calibration only) does it exists a session I can inherit? 
        if self.device_is_calibrating_verified:
            found, data, _, _ = self.__extract_from_db( # if self.device_is_calibrating_verified:
                """
                SELECT DISTINCT 
                    SESSION_TOKEN_ID AS INHERITABLE_SESSION_TOKEN_ID
                FROM sar.F_HL2_STAGING_WAYPOINTS
                WHERE 1=1
                AND SESSION_TOKEN_INHERITED_ID IS NULL
                AND U_REFERENCE_POSITION_ID = %(U_REFERENCE_POSITION_ID)s
                LIMIT 1; -- take the FIRST available
                """,
                {
                    'U_REFERENCE_POSITION_ID' : self.request.ref_id
                }
            )

            if not found:
                self.inheritable_session_staging = None
            else:
                self.inheritable_session_staging = data[0]['INHERITABLE_SESSION_TOKEN_ID']
        

        # (in any case) get the PK of the origin of the inherited origin position
        if self.inheritable_session_staging is not None:
            _, data, _, _ = self.__extract_from_db( # if self.inheritable_session_staging is not None:
                """
                SELECT 
                    F_HL2_QUALITY_WAYPOINTS_PK AS ALIGNMENT_ALIGNED_WITH_WAYPOINT_FK
                FROM sar.F_HL2_STAGING_WAYPOINTS 
                WHERE 1=1
                AND SESSION_TOKEN_ID = %(INHERITABLE_SESSION_TOKEN_ID)s
                AND LOCAL_POSITION_ID = 0
                LIMIT 1;
                """,
                {
                    'INHERITABLE_SESSION_TOKEN_ID' : self.inheritable_session_staging
                }
            )
            self.inherited_origin_pk = data[0]['ALIGNMENT_ALIGNED_WITH_WAYPOINT_FK']
        else:
            self.inherited_origin_pk = None



    def __create_staging_session(self):
        ''' the function inserts the origin into the staging waypoints table

        - SESSION_TOKEN_INHERITED_ID is null if there are no candidates for inheritance
        - ALIGNMENT_ALIGNED_WITH_WAYPOINT_FK is the origin of inherited session, or simply NULL
        - ALIGNMENT_QUALITY_VL is always 100% in this case
        - ALIGNMENT_TYPE_FL is true if the session is not inherited
        '''
        cur = self.db.get_cursor()

        # create fake session token if required
        if self.device_is_calibrating_verified:
            self.inherited_session_real = self.inheritable_session_staging
            self.inherited_session_fake = self.security_handle.create_fake_session_token(
                self.request.user_id,
                self.request.device_id,
                self.request.session_token,
                self.inheritable_session_staging
            )
        else:
            self.inherited_session_real = None
            self.inherited_session_fake = None

        
        # create staging origin
        sql_insert = """
        INSERT INTO sar.F_HL2_STAGING_WAYPOINTS (
            DEVICE_ID,
            SESSION_TOKEN_ID, 
            SESSION_TOKEN_INHERITED_ID,
            U_REFERENCE_POSITION_ID, U_SOURCE_FROM_SERVER_FL,
            UX_VL, UY_VL, UZ_VL,
            LOCAL_POSITION_ID,
            REQUEST_POSITION_ID,
            ALIGNMENT_ALIGNED_WITH_WAYPOINT_FK,
            ALIGNMENT_QUALITY_VL,
            ALIGNMENT_TYPE_FL,
            LOCAL_AREA_INDEX_ID
            -- AREA_RADIUS_VL ???
        ) VALUES (
            %(DEVICE_ID)s,
            %(SESSION_TOKEN_ID)s,
            %(SESSION_TOKEN_INHERITED_ID)s,
            %(U_REFERENCE_POSITION_ID)s, TRUE,
            0.00, 0.00, 0.00, 
            0,
            0,
            %(ALIGNMENT_ALIGNED_WITH_WAYPOINT_FK)s,
            100.0,
            %(ALIGNMENT_TYPE_FL)s,
            0
        );
        """ 
        sql_params = {
            'DEVICE_ID' : self.request.device_id,
            'SESSION_TOKEN_ID' : self.request.session_token,
            'SESSION_TOKEN_INHERITED_ID' : self.inherited_session_real, # can be None
            'U_REFERENCE_POSITION_ID' : self.request.ref_id, 
            'ALIGNMENT_ALIGNED_WITH_WAYPOINT_FK' : self.inherited_origin_pk, # can be None
            'ALIGNMENT_TYPE_FL' : ( self.inherited_origin_pk is not None )
        }
        # self.log.debug_detail(f"sql params: {sql_params}", src="download:__create_staging_session")
        # self.log.debug_detail(f"sql code: {sql_insert % sql_params}", src="download:__create_staging_session")
        cur.execute(
            sql_insert, sql_params
        )




    



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
