
import psycopg2
from fastapi import status
from main_app.environment import environment
from main_app.api_logging.logging import log
from main_app.interfaces import db_interface
from .transaction_base import api_transaction_base
from main_app.api_models import api_hl2_download_request, api_hl2_download_response
import json




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
        self.device_is_calibrating = False
        self.device_is_calibrating_verified = False
        self.inherited_session = None
    

    def check( self ) -> None:
        ''' transaction check phase
        
        '''

        if self.request.based_on == "":
            self.device_is_calibrating = True
        else:
            self.device_is_calibrating = False
            self.inherited_session = self.request.based_on

        pass


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
        
        cur = self.db.get_cursor()
        cur.execute("BEGIN TRANSACTION;")

        # ... transaction : success ...

        cur.execute("COMMIT TRANSACTION;")
    

    def __exec_fail( self ):
        global api_transaction_hl2_download_sql_exec_log
        
        cur = self.db.get_cursor()
        cur.execute("BEGIN TRANSACTION;")

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