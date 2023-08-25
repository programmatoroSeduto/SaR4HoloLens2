
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
WITH all_points AS (
SELECT 
ROW_NUMBER() OVER ( PARTITION BY LOCAL_POSITION_ID ORDER BY F_HL2_QUALITY_WAYPOINTS_PK DESC ) AS rowno,
*
FROM sar.F_HL2_STAGING_WAYPOINTS
WHERE 1=1
AND NOT(ALIGNMENT_TYPE_FL)
AND U_REFERENCE_POSITION_ID = %(U_REFERENCE_POSITION_ID)s
AND ( 
	SESSION_TOKEN_INHERITED_ID IS NULL 
	OR 
	SESSION_TOKEN_INHERITED_ID = %(SESSION_TOKEN_INHERITED_ID)s
	)
)
SELECT DISTINCT
    F_HL2_QUALITY_WAYPOINTS_PK ,
    LOCAL_POSITION_ID,
    UX_VL, UY_VL, UZ_VL,
    COALESCE(
        WAYPOINT_CREATED_TS,
        CREATED_TS
    ) AS WAYPOINT_CREATED_TS
FROM all_points
WHERE rowno = 1
AND dist( UX_VL, UY_VL, UZ_VL, %(POS_X)s, %(POS_Y)s, %(POS_Z)s ) < %(RADIUS)s
AND SESSION_TOKEN_ID <> %(SESSION_TOKEN_ID)s;
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
WITH 
source_wps AS (
SELECT 
ROW_NUMBER() OVER ( PARTITION BY LOCAL_POSITION_ID ORDER BY F_HL2_QUALITY_WAYPOINTS_PK DESC ) AS rowno,
*
FROM sar.F_HL2_STAGING_WAYPOINTS
WHERE 1=1
AND NOT(ALIGNMENT_TYPE_FL)
AND U_REFERENCE_POSITION_ID = %(U_REFERENCE_POSITION_ID)s
AND ( 
	SESSION_TOKEN_INHERITED_ID IS NULL 
	OR 
	SESSION_TOKEN_INHERITED_ID = %(SESSION_TOKEN_INHERITED_ID)s
	)
) -- SELECT * FROM source_wps;
, all_points AS ( -- tutti i punti della sessione, ereditati o generati diretti
SELECT DISTINCT
*
FROM source_wps
WHERE rowno = 1
AND dist( UX_VL, UY_VL, UZ_VL, %(POS_X)s, %(POS_Y)s, %(POS_Z)s ) < %(RADIUS)s
) -- SELECT * FROM all_points;
, unknown_points AS ( -- di questi punti, vai a selezionare quelli effettivamente nuovi da inviare nella distanza
SELECT DISTINCT
*
FROM all_points
WHERE SESSION_TOKEN_ID <> %(SESSION_TOKEN_ID)s
) -- SELECT * FROM all_points;
, selected_paths AS ( -- tutti gli archi che hanno almeno un punto nuovo tra i due estremi
SELECT 
*
FROM sar.F_HL2_STAGING_PATHS
WHERE 1=0
OR WAYPOINT_1_STAGING_FK IN ( SELECT DISTINCT F_HL2_QUALITY_WAYPOINTS_PK FROM unknown_points )
OR WAYPOINT_2_STAGING_FK IN ( SELECT DISTINCT F_HL2_QUALITY_WAYPOINTS_PK FROM unknown_points )
) 
SELECT
    pth.WAYPOINT_1_STAGING_FK,
    wp1s.LOCAL_POSITION_ID AS LOCAL_POSITION_1_ID,
    pth.WAYPOINT_2_STAGING_FK,
    wp2s.LOCAL_POSITION_ID AS LOCAL_POSITION_2_ID,
    COALESCE(pth.PATH_DISTANCE ,dist( 
        wp1s.UX_VL, wp1s.UY_VL, wp1s.UZ_VL, 
        wp2s.UX_VL, wp2s.UY_VL, wp2s.UZ_VL )) AS PATH_DISTANCE,
    pth.CREATED_TS
FROM selected_paths 
	AS pth
LEFT JOIN all_points
	AS wp1s
	ON ( wp1s.F_HL2_QUALITY_WAYPOINTS_PK = pth.WAYPOINT_1_STAGING_FK )
LEFT JOIN all_points
	AS wp2s
	ON ( wp2s.F_HL2_QUALITY_WAYPOINTS_PK = pth.WAYPOINT_2_STAGING_FK )
WHERE 1=1 -- to avoid dangling paths
AND wp1s.LOCAL_POSITION_ID IS NOT NULL
AND wp2s.LOCAL_POSITION_ID IS NOT NULL
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

            if self.security_handle.has_fake_token(
                self.request.user_id,
                self.request.device_id,
                self.request.session_token ):
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

            self.inherited_session_real = self.security_handle.try_get_real_token_from_fake(
                self.request.user_id,
                self.request.device_id,
                self.request.session_token,
                self.inherited_session_fake )
            
            if self.inherited_session_real is None:
                self.__build_response(
                    res_status=status.HTTP_401_UNAUTHORIZED,
                    res_status_description="unauthorized",
                    log_detail='cannot find real token from rovided fake token',
                    unsecure_request=True
                )
                return
            else:
                self.inheritable_session_staging = self.request.based_on
        
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
        
        try:
            if self.__log_error:
                self.__exec_fail()
            else:
                self.__exec_success()
        except Exception as e:
            self.log.err("Execution error during EXEC phase! {e}", src="aaaaaa")
            self.db.execute("ROLLBACK TRANSACTION;")
    


    def __exec_success( self ):
        global api_transaction_hl2_download_sql_exec_get_waypoints
        global api_transaction_hl2_download_sql_exec_get_paths
        global api_transaction_hl2_download_sql_exec_log

        cur = self.db.get_cursor()
        cur.execute("BEGIN TRANSACTION;")

        req_dict = {
            'U_REFERENCE_POSITION_ID' : self.request.ref_id,
            'SESSION_TOKEN_INHERITED_ID' : ( self.inherited_session_real or '' ),
            'SESSION_TOKEN_ID' : self.request.session_token,
            'POS_X' : ( 0.0 if self.self.device_is_calibrating_verified else self.request.center[0] ),
            'POS_Y' : ( 0.0 if self.self.device_is_calibrating_verified else self.request.center[1] ),
            'POS_Z' : ( 0.0 if self.self.device_is_calibrating_verified else self.request.center[2] ),
            'RADIUS' : self.request.radius
        }

        self.response.ref_id = self.request.ref_id
        self.response.based_on = self.inherited_session_fake or ""

        # get max of local IDs
        _, data, _, _ = self.__extract_from_db( 
            api_transaction_hl2_download_sql_exec_get_max_id,
            {
                'U_REFERENCE_POSITION_ID' : req_dict['U_REFERENCE_POSITION_ID'],
                'SESSION_TOKEN_INHERITED_ID' : req_dict['SESSION_TOKEN_INHERITED_ID']
            }
        )
        self.response.max_idx = data[0]['MAX_LOCAL_POSITION_ID']

        # extract waypoints
        found, self.wps_raw, _, _ = self.__extract_from_db( 
            api_transaction_hl2_download_sql_exec_get_waypoints,
            req_dict )

        if found: 
            # extract paths
            _, self.pth_raw, _, _ = self.__extract_from_db( 
                api_transaction_hl2_download_sql_exec_get_paths,
                req_dict )

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
                'LOG_TYPE_DS' : 'hololens2 download',
                'LOG_TYPE_ACCESS_FL' : False,
                'LOG_SUCCESS_FL' : True,
                'LOG_WARNING_FL' : False,
                'LOG_SECURITY_FAULT_FL' : False,
                'LOG_DETAILS_DS' : self.__log_detail_ds,
                'LOG_DATA' : self.dict_to_field(dict(self.request)),
            }
        )

        cur.execute("COMMIT TRANSACTION;")
    


    def __exec_fail( self ):
        global api_transaction_hl2_download_sql_exec_log
        
        cur = self.db.get_cursor()
        cur.execute("BEGIN TRANSACTION;")

        cur.execute(
            api_transaction_hl2_download_sql_exec_log,
            {
                'LOG_TYPE_DS' : 'hololens2 download',
                'LOG_TYPE_ACCESS_FL' : False,
                'LOG_SUCCESS_FL' : False,
                'LOG_WARNING_FL' : False,
                'LOG_SECURITY_FAULT_FL' :  self.__log_unsecure_request,
                'LOG_DETAILS_DS' : self.__log_detail_ds,
                'LOG_DATA' : self.dict_to_field(dict(self.request)),
            }
        )

        cur.execute("COMMIT TRANSACTION;")



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
        _, data, _, _ = self.__extract_from_db(
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
            found, data, _, _ = self.__extract_from_db(
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
            _, data, _, _ = self.__extract_from_db(
                """
                SELECT 
                    F_HL2_QUALITY_WAYPOINTS_PK AS ALIGNMENT_ALIGNED_WITH_WAYPOINT_FK
                FROM sar.F_HL2_STAGING_WAYPOINTS 
                WHERE 1=1
                AND SESSION_TOKEN_ID = %(INHERITABLE_SESSION_TOKEN_ID)s; 
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
        if self.device_is_calibrating_verified and self.inheritable_session_staging is not None:
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
        cur.execute(
            """
            INSERT INTO sar.F_HL2_STAGING_WAYPOINTS (
                DEVICE_ID,
                SESSION_TOKEN_ID, 
                SESSION_TOKEN_INHERITED_ID,
                U_REFERENCE_POSITION_ID, U_SOURCE_FROM_SERVER_FL,
                UX_VL, UY_VL, UZ_VL,
                LOCAL_POSITION_ID,
                ALIGNMENT_ALIGNED_WITH_WAYPOINT_FK,
                ALIGNMENT_QUALITY_VL,
                ALIGNMENT_TYPE_FL
                -- LOCAL_AREA_INDEX_ID ??
                -- AREA_RADIUS_VL ???
            ) VALUES (
                %(DEVICE_ID)s,
                %(SESSION_TOKEN_ID)s,
                %(SESSION_TOKEN_INHERITED_ID)s,
                %(U_REFERENCE_POSITION_ID)s, TRUE,
                0.00, 0.00, 0.00, 
                0,
                %(ALIGNMENT_ALIGNED_WITH_WAYPOINT_FK)s,
                100.0,
                %(ALIGNMENT_TYPE_FL)s
            );
            """,
            {
                'DEVICE_ID' : self.request.device_id,
                'SESSION_TOKEN_ID' : self.request.session_token,
                'SESSION_TOKEN_INHERITED_ID' : self.inherited_session_fake, # can be None
                'U_REFERENCE_POSITION_ID' : self.request.ref_id, 
                'ALIGNMENT_ALIGNED_WITH_WAYPOINT_FK' : self.inherited_origin_pk, # can be None
                'ALIGNMENT_TYPE_FL' : ( self.inherited_origin_pk is not None )
            }
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
