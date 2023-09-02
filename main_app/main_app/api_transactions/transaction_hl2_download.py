
import psycopg2
from fastapi import status
from main_app.environment import environment
from main_app.api_logging.logging import log
from main_app.interfaces import db_interface
from .transaction_base import api_transaction_base
from main_app.api_models import api_hl2_download_request, api_hl2_download_response, data_hl2_waypoint, data_hl2_path
import json
from api_transactions.api_security_transactions.ud_security_support import ud_security_support




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
        self.inherited_token:str = None
        self.fake_token = None
        self.wp_found = False
        self.wp_data = []
        self.wp_count = 0
        self.extraction_args = dict()
        self.pt_found = False
        self.pt_data = []
        self.pt_count = 0
        self.current_position_id = -1
        self.known_waypoints = set()
        # ...
    


    def check( self ) -> None:
        ''' transaction check phase
        
        '''

        self.log.debug("TODO: implement checks", src="download:check")
        self.log.debug("TODO: check minimum distance from inferred current position must be less than, otherwise return a particular statu code requiring a upload", src="download:check")
        
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

        self.log.debug("Performing transaction", src="download:__exec_success")
        need_fake_token = False

        self.log.debug("is this user already registered in staging?", src="download:__exec_success")
        _, data, _, _ = self.__extract_from_db(
            '''
            SELECT session_in_staging_fl(%(session_token)s::TEXT, %(ref_id)s::TEXT)::BOOLEAN AS RES;
            ''',
            {
                'session_token' : self.request.session_token,
                'ref_id' : self.request.ref_id
            }
        )
        self.log.debug(f"from db: {data[0]}", src="download:__exec_success")
        if data[0]['RES']:
            self.log.debug("already registered! Trying to get real token from fake...", src="download:__exec_success")
            fake_exists, fake_is_original, real_token = self.security_handle.try_get_real_token_from_fake(
                self.request.user_id,
                self.request.device_id,
                self.request.session_token,
                self.request.based_on
            )

            if fake_exists:
                self.log.debug(f"fake token found", src="download:__exec_success")
            else:
                self.log.debug(f"fake token does not exist!", src="download:__exec_success")
                self.response.status = status.HTTP_404_NOT_FOUND
                self.response.status_detail = 'invalid credentials'
                return

            if fake_is_original:
                self.log.debug(f"this session is a original session", src="download:__exec_success")
                self.inherited_token = None
            else:
                self.log.debug(f"this session is a inherited session", src="download:__exec_success")
                self.inherited_token = real_token

        else:
            need_fake_token = True
            self.log.debug("a new user .. what kind of new user?", src="download:__exec_success")
            _, data, _, _ = self.__extract_from_db(
                '''
                SELECT inheritable_session_id(%(ref_id)s::TEXT) AS RES;
                ''',
                {
                    'ref_id' : self.request.ref_id
                }
            )
            if data[0]['RES'] is not None:
                self.log.debug("found a session the user can inherit", src="download:__exec_success")
                self.inherited_token = data[0]['RES']
                self.log.debug(f"got fake token: {self.fake_token}", src="download:__exec_success")
                _, _, _, _ = self.__extract_from_db(
                    '''
                    SELECT register_staging_session_child(%(device_id)s, %(session_token)s, %(based_on)s, %(ref_id)s) AS RES;
                    ''',
                    {
                        'device_id' : self.request.device_id,
                        'session_token' : self.request.session_token,
                        'based_on' : self.inherited_token,
                        'ref_id' : self.request.ref_id
                    }
                )
                self.log.debug("OK created father user", src="download:__exec_success")
            else:
                self.log.debug("the user is completely new", src="download:__exec_success")
                _, _, _, _ = self.__extract_from_db(
                    '''
                    SELECT register_staging_session_father(%(device_id)s::TEXT, %(session_token)s::TEXT, %(ref_id)s::TEXT) AS RES;
                    ''',
                    {
                        'device_id' : self.request.device_id,
                        'session_token' : self.request.session_token,
                        'ref_id' : self.request.ref_id
                    }
                )
                self.log.debug("OK created father user", src="download:__exec_success")
                
        if need_fake_token:
            self.log.debug("creating fake token for user...", src="download:__exec_success")
            if self.fake_token is not None:
                self.log.debug("with fake token", src="download:__exec_success")
            else:
                self.log.debug("with NO fake token (father session)", src="download:__exec_success")
            faket = self.security_handle.create_fake_session_token(
                self.request.user_id,
                self.request.device_id,
                self.request.session_token,
                self.inherited_token # can be None
            )
            self.log.debug(f"creating fake token for user... OK; returned {faket}", src="download:__exec_success")
            self.log.debug(f"response so far: {self.response.based_on}", src="download:__exec_success")
            self.response.based_on = faket
            self.response.ref_id = self.request.ref_id
            self.response.max_idx = 0
        else:
            self.response.based_on = ""
            self.response.ref_id = self.request.ref_id
        
        self.log.debug(f"getting max of session ...", src="download:__exec_success")
        max_id_found, data, _, _ = self.__extract_from_db(
            """
            SELECT MAX(LOCAL_POSITION_ID) AS MAX_ID FROM get_session_generic_waypoints(
                %(REF_POS_ID)s,
                %(SESSION_ID)s
            ) AS tab;
            """,
            {
                'REF_POS_ID' : self.request.ref_id,
                'SESSION_ID' : ( self.inherited_token or self.request.session_token )
            }
        )
        if max_id_found:
            self.response.max_idx = data[0]['MAX_ID']
            self.log.debug(f"getting max of session ... OK max id {self.response.max_idx}", src="download:__exec_success")
        else:
            self.log.debug(f"getting max of session ... WARNING max id not found", src="download:__exec_success")

        # CALLS ARGUMENTS
        self.extraction_args = {
            'REF_POS_ID' : self.request.ref_id,
            'SESSION_INHERITED_ID' : (self.inherited_token or None),
            'SESSION_ID' : self.request.session_token,
            'UX' : self.request.center[0],
            'UY' : self.request.center[1],
            'UZ' : self.request.center[2],
            'RADIUS' : self.request.radius
        }
        self.log.debug_detail(f"extracion args: {self.extraction_args}", src="download:__exec_success")
        
        # extract waypoints
        self.log.debug(f"getting waypoints from server...", src="download:__exec_success")
        self.wp_found, self.wp_data, _, self.wp_count = self.__extract_from_db(
            '''
            SELECT 
                wp.F_HL2_QUALITY_WAYPOINTS_PK,
                wp.LOCAL_POSITION_ID,
                wp.UX_VL, wp.UY_VL, wp.UZ_VL, 
                COALESCE(wp.WAYPOINT_CREATED_TS, wp.CREATED_TS) AS CREATED_TS
            FROM get_unknown_waypoints_in_radius(
                %(REF_POS_ID)s::CHAR(24),
                %(SESSION_INHERITED_ID)s::TEXT, -- inherited
                %(SESSION_ID)s::TEXT, -- user
                %(UX)s::FLOAT, %(UY)s::FLOAT, %(UZ)s::FLOAT, %(RADIUS)s::FLOAT
            ) AS wp_base
            LEFT JOIN sar.F_HL2_STAGING_WAYPOINTS
                AS wp
                ON (wp_base.F_HL2_QUALITY_WAYPOINTS_PK = wp.F_HL2_QUALITY_WAYPOINTS_PK)
            ;
            ''',
            self.extraction_args
        )
        self.log.debug(f"getting waypoints from server... OK: found {self.wp_count} waypoints", src="download:__exec_success")
        if self.wp_count > 0:
            self.log.debug_detail(f"CREATED_TS of type: {self.wp_data[0]['CREATED_TS']} waypoints", src="download:__exec_success")
        
        if self.wp_count == 0:
            self.log.debug(f"no new waypoints found for this user.", src="download:__exec_success")
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
            return 

        # extract known waypoints
        self.log.debug(f"getting known waypoints ... ", src="download:__exec_success")
        _, data, _, _ = self.__extract_from_db(
            '''
            SELECT DISTINCT
                LOCAL_POSITION_ID 
            FROM sar.F_HL2_STAGING_WAYPOINTS
            WHERE 1=1
            AND SESSION_TOKEN_ID = %(SESSION_TOKEN_ID)s ;
            ''',
            {
                'SESSION_TOKEN_ID' : self.extraction_args['SESSION_ID']
            }
        )
        self.log.debug(f"getting known waypoints ... OK", src="download:__exec_success")
        for wp in data:
            self.known_waypoints.add(wp['LOCAL_POSITION_ID'])
        self.log.debug_detail(f"known IDs are: {self.known_waypoints}", src="download:__exec_success")

        # extract paths
        self.log.debug(f"getting paths from server...", src="download:__exec_success")
        self.pt_found, self.pt_data, _, self.pt_count = self.__extract_from_db(
            '''
            SELECT DISTINCT
                WP1_LOCAL_POS_ID,
                WP2_LOCAL_POS_ID,
                DISTANCE_VL,
                MIN(CREATED_TS) AS CREATED_TS
            FROM get_unknown_paths_in_radius(
                %(REF_POS_ID)s::CHAR(24),
                %(SESSION_INHERITED_ID)s::TEXT, -- inherited
                %(SESSION_ID)s::TEXT, -- user
                %(UX)s::FLOAT, %(UY)s::FLOAT, %(UZ)s::FLOAT, %(RADIUS)s::FLOAT
            )
            GROUP BY 1,2,3 ;
            ''',
            {
                'REF_POS_ID' : self.request.ref_id,
                'SESSION_INHERITED_ID' : (self.inherited_token or self.request.session_token),
                'SESSION_ID' : self.request.session_token,
                'UX' : self.request.center[0],
                'UY' : self.request.center[1],
                'UZ' : self.request.center[2],
                'RADIUS' : self.request.radius
            }
        )
        self.log.debug(f"getting paths from server... OK: found {self.pt_count} paths", src="download:__exec_success")

        # get current positon ID of the user
        self.log.debug(f"getting current position ID from server...", src="download:__exec_success")
        _, data, _, _ = self.__extract_from_db(
            '''
            SELECT 
                get_current_position_local_id(
                    %(REF_POS_ID)s::CHAR(24),
                    %(SESSION_INHERITED_ID)s::TEXT, 
                    %(UX)s::FLOAT, %(UY)s::FLOAT, %(UZ)s::FLOAT
                ) AS POS_ID
            ''',
            {
                'REF_POS_ID' : self.extraction_args['REF_POS_ID'],
                'SESSION_INHERITED_ID' : self.extraction_args['SESSION_INHERITED_ID'] or self.extraction_args['SESSION_ID'],
                'UX' : self.extraction_args['UX'],
                'UY' : self.extraction_args['UY'],
                'UZ' : self.extraction_args['UZ']
            }
        )
        self.log.debug(f"getting current position ID from server... OK", src="download:__exec_success")
        self.current_position_id = data[0]['POS_ID']
        self.log.debug(f"CURRENT pos ID is: {self.current_position_id}", src="download:__exec_success")

        self.log.debug(f"performing path analysis ... ", src="download:__exec_success")
        wp_set, pt_set = self.__paths_analysis(
            self.known_waypoints, 
            self.wp_data, 
            self.pt_data
            )
        self.log.debug(f"performing path analysis ... ", src="download:__exec_success")

        self.log.debug(f"collecting waypoints to return ... ", src="download:__exec_success")
        for wp in self.wp_data:
            if wp['LOCAL_POSITION_ID'] in wp_set:
                self.log.debug(f"WP:{wp['LOCAL_POSITION_ID']} excluded; skip", src="download:__exec_success")
                continue
            self.response.waypoints.append(
                data_hl2_waypoint(
                    pos_id=wp['LOCAL_POSITION_ID'],
                    area_id=0,
                    v=[ wp['UX_VL'], wp['UY_VL'], wp['UZ_VL'] ],
                    wp_timestamp=wp['CREATED_TS']
                )
            )
        self.log.debug(f"collecting waypoints to return ... OK", src="download:__exec_success")

        self.log.debug(f"collecting paths to return ... ", src="download:__exec_success")
        for pt in self.pt_data:
            path = ( pt['WP1_LOCAL_POS_ID'], pt['WP2_LOCAL_POS_ID'] )
            if path in pt_set:
                self.log.debug(f"PATH:{path} excluded; skip", src="download:__exec_success")
                continue
            self.response.paths.append(
                data_hl2_path(
                    wp1=path[0],
                    wp2=path[1],
                    dist=pt['DISTANCE_VL'],
                    pt_timestamp=pt['CREATED_TS']
                )
            )
        self.log.debug(f"collecting paths to return ... OK", src="download:__exec_success")

        self.log.debug(f"writing results in tables ... ", src="download:__exec_success")
        _, _, _, _ = self.__extract_from_db( 
            '''
            DROP TYPE IF EXISTS json_schema_wp;
            CREATE TYPE json_schema_wp AS (
                wp INT
            );
            DROP TYPE IF EXISTS json_schema_pt;
            CREATE TYPE json_schema_pt AS (
                wp1 INT,
                wp2 INT
            );
            WITH 
            exclusion_list_wp AS (
            SELECT DISTINCT
                wp
            FROM JSON_POPULATE_RECORDSET(null::json_schema_wp, %(JSON_WP)s)
            )
            , pre_insert_wp AS (
            SELECT 
                %(DEVICE_ID)s AS DEVICE_ID,
                %(SESSION_TOKEN_ID)s AS SESSION_TOKEN_ID,
                %(SESSION_TOKEN_INHERITED_ID)s AS SESSION_TOKEN_INHERITED_ID,
                wp.LOCAL_POSITION_ID AS LOCAL_POSITION_ID,
                wp.LOCAL_POSITION_ID AS REQUEST_POSITION_ID,
                %(U_REFERENCE_POSITION_ID)s AS U_REFERENCE_POSITION_ID,
                wp.UX_VL, 
                wp.UY_VL, 
                wp.UZ_VL, 
                TRUE AS U_SOURCE_FROM_SERVER_FL,
                COALESCE(wp.WAYPOINT_CREATED_TS, wp.CREATED_TS) AS WAYPOINT_CREATED_TS,
                wp.F_HL2_QUALITY_WAYPOINTS_PK AS ALIGNMENT_ALIGNED_WITH_WAYPOINT_FK,
                TRUE AS ALIGNMENT_TYPE_FL,
                100.0 AS ALIGNMENT_QUALITY_VL,
                0.0 AS ALIGNMENT_DISTANCE_VL,
                wp.F_HL2_QUALITY_WAYPOINTS_PK AS ALIGNMENT_DISTANCE_FROM_WAYPOINT_FK
            FROM get_unknown_waypoints_in_radius(
                %(U_REFERENCE_POSITION_ID)s,
                %(SESSION_TOKEN_INHERITED_ID)s, -- inherited
                %(SESSION_TOKEN_ID)s, -- user
                %(UX)s::FLOAT, %(UY)s::FLOAT, %(UZ)s::FLOAT, %(RADIUS)s::FLOAT
            ) AS wp_base
            LEFT JOIN sar.F_HL2_STAGING_WAYPOINTS
                AS wp
                ON (wp_base.F_HL2_QUALITY_WAYPOINTS_PK = wp.F_HL2_QUALITY_WAYPOINTS_PK)
            WHERE wp.LOCAL_POSITION_ID NOT IN (SELECT wp FROM exclusion_list_wp)
            )
            , insert_wp AS (
            INSERT INTO sar.F_HL2_STAGING_WAYPOINTS (
                DEVICE_ID, SESSION_TOKEN_ID, SESSION_TOKEN_INHERITED_ID, LOCAL_POSITION_ID,
                REQUEST_POSITION_ID, U_REFERENCE_POSITION_ID, UX_VL, UY_VL, UZ_VL, U_SOURCE_FROM_SERVER_FL,
                WAYPOINT_CREATED_TS, ALIGNMENT_ALIGNED_WITH_WAYPOINT_FK, ALIGNMENT_TYPE_FL,
                ALIGNMENT_QUALITY_VL, ALIGNMENT_DISTANCE_VL, ALIGNMENT_DISTANCE_FROM_WAYPOINT_FK
            )
            SELECT * FROM pre_insert_wp
            RETURNING TRUE
            )
            , exclusion_list_pt AS (
            SELECT DISTINCT
                wp1, 
                wp2
            FROM JSON_POPULATE_RECORDSET(null::json_schema_pt, %(JSON_PT)s)
            )
            -- NOTEWORTHY : JSON FROM API ALREADY CONTAINS REDUNDANT PATHS
            -- , union_exclusion_list_pt AS (
            -- SELECT wp1, wp2 FROM exclusion_list_pt
            -- UNION
            -- SELECT wp2, wp1 FROM exclusion_list_pt
            -- )
            , pre_insert_pt AS (
            SELECT
                %(DEVICE_ID)s AS DEVICE_ID,
                %(SESSION_TOKEN_ID)s AS SESSION_TOKEN_ID,
                %(SESSION_TOKEN_INHERITED_ID)s AS SESSION_TOKEN_INHERITED_ID,
                %(U_REFERENCE_POSITION_ID)s AS U_REFERENCE_POSITION_ID,
                WAYPOINT_1_STAGING_FK,
                WAYPOINT_2_STAGING_FK
            FROM get_unknown_paths_in_radius(
                %(U_REFERENCE_POSITION_ID)s,
                %(SESSION_TOKEN_INHERITED_ID)s, -- inherited
                %(SESSION_TOKEN_ID)s, -- user
                %(UX)s::FLOAT, %(UY)s::FLOAT, %(UZ)s::FLOAT, %(RADIUS)s::FLOAT
            )
            WHERE 1=1 
            AND (WP1_LOCAL_POS_ID, WP2_LOCAL_POS_ID) NOT IN (SELECT wp1, wp2 FROM exclusion_list_pt)
            )
            , insert_pt AS (
            INSERT INTO sar.F_HL2_STAGING_PATHS (
                DEVICE_ID, SESSION_TOKEN_ID, SESSION_TOKEN_INHERITED_ID, U_REFERENCE_POSITION_ID,
                WAYPOINT_1_STAGING_FK, WAYPOINT_2_STAGING_FK
            )
            SELECT * FROM pre_insert_pt
            RETURNING TRUE
            )
            SELECT TRUE AS RES;
            DROP TYPE IF EXISTS json_schema_wp;
            DROP TYPE IF EXISTS json_schema_pt;
            ''',
            {
                'JSON_WP' : json.dumps([ {"wp" : str(x)} for x in wp_set ]),
                'JSON_PT' : json.dumps([ {"wp1" : str(x[0]), "wp2" : str(x[1])} for x in pt_set ]),
                'DEVICE_ID' : self.request.device_id,
                'SESSION_TOKEN_ID' : self.request.session_token,
                'SESSION_TOKEN_INHERITED_ID' : self.inherited_token,
                'U_REFERENCE_POSITION_ID' : self.request.ref_id,
                'UX' : self.extraction_args['UX'],
                'UY' : self.extraction_args['UY'],
                'UZ' : self.extraction_args['UZ'],
                'RADIUS' : self.request.radius
            }, fetch_res=False
        )
        self.log.debug(f"writing results in tables ... OK", src="download:__exec_success")

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

    def __paths_analysis(self, known_waypoints:set, waypoints:list, paths:list) -> (list, list):
        '''
        RETURNS:
            (list, list)
            1. waypoints exclusion list
            2. paths exclusion list
        '''

        # TODO: wha about empty structure?

        wp_set = set() # waypoints exclusion list
        pt_set = set() # found paths (list of tuples2) to be explored

        for wp in waypoints:
            wp_set.add(int(wp['LOCAL_POSITION_ID']))

        self.log.debug(f"creating the structure of the problem... ", src="download:__paths_analysis")
        for row in paths:
            pt = ( row['WP1_LOCAL_POS_ID'], row['WP2_LOCAL_POS_ID'] )
            if pt not in pt_set:
                self.log.debug(f"adding PATH:{pt[0]} <-> {pt[1]}", src="download:__paths_analysis")
                pt_set.add(pt)
                pt_set.add(( pt[1], pt[0] ))
            else:
                self.log.debug(f"skipping PATH:{pt[0]} <-> {pt[1]}", src="download:__paths_analysis")
        self.log.debug(f"creating the structure of the problem... OK", src="download:__paths_analysis")

        self.log.debug(f"performing analysis", src="download:__paths_analysis")
        for wp in known_waypoints:
            current_pos = int(wp)
            # wp_set.add(current_pos)
            wp_set, pt_set = self.__iterate_over_paths(current_pos, wp_set, pt_set)
            if len(pt_set) == 0:
                break
        
        return wp_set, pt_set


    
    
    def __iterate_over_paths(self, current_pos:int, wp_set:set, pt_set:set, iteration:int = 1) -> (set, set):
        self.log.debug_detail(f"BEGIN ITERATION {iteration} WITH\n\tcurrent_pos:{current_pos}\n\twp_set:{wp_set}\n\tpt_set:{pt_set}", src="__iterate_over_paths")

        found_wp_set = set() # set of found waypoints reachable from this waypoint
        for wp in wp_set:
            tup = ( int(current_pos), int(wp) )
            # self.log.debug_detail(f"evalaing path {tup} IN {pt_set}... ", src="__iterate_over_paths")
            if tup in pt_set:
                # self.log.debug_detail(f"{tup} ... added to set", src="__iterate_over_paths")
                found_wp_set.add(wp)
                pt_set.remove(tup)
                pt_set.remove((tup[1], tup[0]))
            else:
                self.log.debug_detail(f"{tup} ... ignored", src="__iterate_over_paths")
        if len(found_wp_set) == 0:
            self.log.debug_detail(f"END ITERATION {iteration} WITH\n\tcurrent_pos:{current_pos}\n\twp_set:{wp_set}\n\tpt_set:{pt_set}", src="__iterate_over_paths")
            return wp_set, pt_set
        found_wp_set.add(current_pos)
        
        for wp in found_wp_set:
            try:
                wp_set.remove(wp) # remove this point from exclusion list
            except Exception as e:
                self.log.debug_detail(f"current pos ID:{wp} is not in set", src="__iterate_over_paths")
            wp_set, pt_set = self.__iterate_over_paths(wp, wp_set, pt_set, iteration+1)

        self.log.debug_detail(f"END ITERATION {iteration} WITH\n\tcurrent_pos:{current_pos}\n\twp_set:{wp_set}\n\tpt_set:{pt_set}", src="__iterate_over_paths")
        return wp_set, pt_set


        
    


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




    



    def __extract_from_db(self, query, query_data:dict, print_query=True, fetch_res=True):
        ''' results as a dictionary
        
        RETURNS
            ( is res not empty?, res_data, res_schema, res_count )
        '''
        cur = self.db.get_cursor()
        if print_query:
            qdata = dict()
            for q in query_data.keys():
                qdata[q] = f"'{str(query_data[q])}'"
            self.log.debug_detail( query % qdata, src="download:__extract_from_db" )
        cur.execute(query, query_data)
        res_schema = []
        res_count = 0
        if fetch_res:
            res_data_raw = cur.fetchall()
            res_schema = [ str(col.name).upper() for col in cur.description ]
            res_count = cur.rowcount

        if res_count == 0:
            return ( False, None, list(), 0 )
        
        res_data = list()
        for row in res_data_raw:
            res_data.append(self.to_dict(res_schema, row))
        
        return ( True, res_data, res_schema, res_count )
