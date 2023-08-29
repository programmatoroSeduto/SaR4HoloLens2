
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
        # ...
    


    def check( self ) -> None:
        ''' transaction check phase
        
        '''

        self.log.debug("TODO: implement checks", src="download:check")
        
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
            self.log.debug("already registered!", src="download:__exec_success")
            self.log.debug("TODO", src="download:__exec_success")
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
                self.log.debug("TODO", src="download:__exec_success")
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
            faket = self.security_handle.create_fake_session_token(
                self.request.user_id,
                self.request.device_id,
                self.request.session_token,
                None # father session
            )
            self.log.debug(f"creating fake token for user... OK; returned {faket}", src="download:__exec_success")
            self.log.debug(f"response so far: {self.response.based_on}", src="download:__exec_success")
            self.response.based_on = faket
            self.response.ref_id = self.request.ref_id
            self.response.max_idx = 0

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




    



    def __extract_from_db(self, query, query_data:dict, print_query=True):
        ''' results as a dictionary
        
        RETURNS
            ( is res not empty?, res_data, res_schema, res_count )
        '''
        cur = self.db.get_cursor()
        if print_query:
            self.log.debug_detail( query % query_data, src="download:__extract_from_db" )
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
