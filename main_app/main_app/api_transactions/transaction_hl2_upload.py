
import psycopg2
from fastapi import status
from main_app.environment import environment
from main_app.api_logging.logging import log
from main_app.interfaces import db_interface
from .transaction_base import api_transaction_base
import json
from main_app.api_models import api_hl2_upload_request, api_hl2_upload_response, data_hl2_waypoint, data_hl2_path
from api_transactions.api_security_transactions.ud_security_support import ud_security_support




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

    def __init__(self, env: environment, request) -> None:
        ''' Create the transaction
        
        '''
        super().__init__(env)
        
        # request comes from the API
        self.request = request
        # response is built during check phase
        self.response = None

        # CHECK response from the daabase
        self.__res:list = None
        self.__res_count:int = -1
        self.__res_schema:list = None

        # logging
        self.__log_detail_ds:str = ""
        self.__log_error:bool = False
        self.__log_unsecure_request:bool = False

        # transaction custom data
        pass




    



    def check( self ) -> None:
        ''' transaction check phase
        
        '''
        pass

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
        cur = self.db.get_cursor()
        cur.execute("BEGIN TRANSACTION;")

        # ... transaction : success ...

        cur.execute("COMMIT TRANSACTION;")




    



    def __exec_fail( self ):
        global api_transaction_hl2_upload_sql_exec_log
        
        cur = self.db.get_cursor()
        cur.execute("BEGIN TRANSACTION;")

        cur.execute(
            api_transaction_hl2_upload_sql_exec_log,
            {
                'LOG_TYPE_DS' : 'hololens2 integration',
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