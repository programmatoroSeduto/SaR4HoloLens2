
import psycopg2
from fastapi import status
from main_app.environment import environment
from main_app.api_logging.logging import log
from main_app.interfaces import db_interface
from .transaction_base import api_transaction_base
from api_models import api_hl2_settings_request, api_hl2_settings_response
import json




api_transaction_hl2_settings_sql_check_profile_info = """
SELECT 
	USER_HEIGHT_VL ,
	BASE_HEIGHT_VL ,
	BASE_DISTANCE_VL ,
	DISTANCE_TOLLERANCE_VL ,
	USER_CLUSTER_FL ,
	CLUSTER_SIZE_VL ,
	USE_MAX_INDICES_FL ,
	MAX_INDICES_VL ,
	LOG_LAYER_VL ,
	REFERENCE_POSITION_ID 
FROM sar.D_HL2_USER_DEVICE_SETTINGS
WHERE 1=1
AND DEVICE_ID = %(DEVICE_ID)s
AND USER_ID = %(USER_ID)s
AND CONFIGURATION_PROFILE_ID = %(CONFIGURATION_PROFILE_ID)s;
"""




api_transaction_hl2_settings_sql_exec_log = """
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





class api_transaction_hl2_settings(api_transaction_base):
    ''' A app transaction. 

    Description of the transaction. 
    '''

    def __init__(self, env: environment, request:api_hl2_settings_request) -> None:
        ''' Create the transaction
        
        '''
        super().__init__(env)
        
        # request comes from the API
        self.request:api_hl2_settings_request = request
        # response is built during check phase
        self.response:api_hl2_settings_response = None

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
        
        Just check if the profile exists; if not, don't create a default profile!
        Just return not found. 
        '''
        global api_transaction_hl2_settings_sql_check_profile_info

        cur = self.db.get_cursor()
        cur.execute( 
            api_transaction_hl2_settings_sql_check_profile_info,
            {
                'USER_ID' : self.request.user_id,
                'DEVICE_ID' : self.request.device_id,
                'CONFIGURATION_PROFILE_ID' : ( 0 if self.request.config_profile_id < 0 else self.request.config_profile_id )
            }
        )
        self.__res = cur.fetchall()
        self.__res_schema = [ str(col.name).upper() for col in cur.description ]
        self.__res_count = cur.rowcount

        if self.__res_count == 0:
            self.__build_response(
                res_status=status.HTTP_404_NOT_FOUND,
                res_status_description="not found",
                log_detail='cannot find requested profile'
            )
            self.response.found_profile = False
            return
        
        self.__res = self.to_dict(self.__res_schema, self.__res[0])
        self.log.debug_detail(self.__res, src="transaction hl2 settings")
        
        self.__build_response(
            res_status=status.HTTP_200_OK,
            res_status_description="success",
            log_detail='found requested profile'
        )
        return

    def execute( self ):
        ''' transaction execution phase
        
        '''
        if self.__log_error:
            self.__exec_fail()
        else:
            self.__exec_success()
    

    def __exec_success( self ):
        
        cur = self.db.get_cursor()
        cur.execute("BEGIN TRANSACTION;")

        self.response.found_profile = True
        self.response.config_profile_id = ( 0 if self.request.config_profile_id < 0 else self.request.config_profile_id )
        self.response.base_height = self.__res['USER_HEIGHT_VL']
        self.response.base_distance = self.__res['BASE_DISTANCE_VL']
        self.response.distance_tollerance = self.__res['DISTANCE_TOLLERANCE_VL']
        self.response.use_cluster = self.__res['USER_CLUSTER_FL']
        self.response.cluster_size = self.__res['CLUSTER_SIZE_VL']
        self.response.use_max_indices = self.__res['USE_MAX_INDICES_FL']
        self.response.max_indices = self.__res['MAX_INDICES_VL']
        self.response.log_layer = self.__res['LOG_LAYER_VL']
        self.response.ref_id = self.__res['REFERENCE_POSITION_ID'] or ''

        cur.execute(
            api_transaction_hl2_settings_sql_exec_log,
            {
                'LOG_TYPE_DS' : 'hl2 settings',
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
        global api_transaction_hl2_settings_sql_exec_log
        
        cur = self.db.get_cursor()
        cur.execute("BEGIN TRANSACTION;")

        cur.execute(
            api_transaction_hl2_settings_sql_exec_log,
            {
                'LOG_TYPE_DS' : 'hl2 settings',
                'LOG_TYPE_ACCESS_FL' : False,
                'LOG_SUCCESS_FL' : False,
                'LOG_WARNING_FL' : False,
                'LOG_SECURITY_FAULT_FL' : False,
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
    ) -> api_hl2_settings_response:
        self.__log_detail_ds = log_detail
        self.response = api_hl2_settings_response(
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