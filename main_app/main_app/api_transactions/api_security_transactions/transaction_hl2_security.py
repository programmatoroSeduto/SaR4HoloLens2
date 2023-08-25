
import psycopg2
from fastapi import status
from main_app.environment import environment
from main_app.api_logging.logging import log
from main_app.interfaces import db_interface
from ..transaction_base import api_transaction_base
from main_app.api_models import api_hl2_base_request, api_hl2_base_response
import json

api_transaction_hl2_security_sql_check= """
SELECT 

user_data.USER_ID,
user_data.AUTH_HOLD_DEVICE_FL,
user_data.USER_AUTH_UPLOAD_FL,
CASE
    WHEN user_session.SESSION_ID IS NULL THEN false
    ELSE true
END AS SESSION_ID_FOUND_FL,
CASE
    WHEN user_device_lookup.USER_ID IS NULL THEN false
    ELSE true
END AS LOOKUP_DEVICE_FOUND_FL,
COALESCE(device_data.DEVICE_CAP_UPLOAD_FL, false) AS DEVICE_CAP_UPLOAD_FL,
COALESCE(device_data.CAP_LOCATION_FL, false) AS CAP_LOCATION_FL,
COALESCE(device_data.DEVICE_AUTH_UPLOAD_FL, false) AS DEVICE_AUTH_UPLOAD_FL,
CASE 
    WHEN device_session.SESSION_ID IS NULL THEN false
    ELSE true
END AS DEVICE_HAS_SESSION_OPENED_FL

FROM ( -- check user existence and auths
SELECT 

USER_ID, 
AUTH_HOLD_DEVICE_FL,
AUTH_UPDATE_DEVICE_FL AS USER_AUTH_UPLOAD_FL

FROM sar.D_USER
WHERE NOT(DELETED_FL)
AND USER_ID = %(user_id)s
) AS user_data

LEFT JOIN ( -- check session token
SELECT 

USER_ID, 
USER_SESSION_TOKEN_ID AS SESSION_ID

FROM sar.F_USER_ACTIVITY
WHERE 1=1
AND USER_SESSION_TOKEN_ID = %(session_token)s
) AS user_session
ON( user_data.USER_ID = user_session.USER_ID )

LEFT JOIN ( -- check device user association
SELECT 

USER_ID,
DEVICE_ID

FROM sar.L_DEVICE_USER
WHERE 1=1
AND NOT(DELETED_FL)
AND DEVICE_ID = %(device_id)s
) AS user_device_lookup
ON ( user_data.USER_ID = user_device_lookup.USER_ID )

LEFT JOIN ( -- check device data and auth
SELECT

DEVICE_ID,
CAP_EXCHANGE_SEND_FL AS DEVICE_CAP_UPLOAD_FL,
CASE
    WHEN CAP_LOCATION_RELATIVE_FL IS NULL OR CAP_LOCATION_GEO_FL IS NULL THEN false
    WHEN CAP_LOCATION_RELATIVE_FL OR CAP_LOCATION_GEO_FL THEN true
    ELSE false
END AS CAP_LOCATION_FL,
AUTH_UPDATE_DEVICE_FL AS DEVICE_AUTH_UPLOAD_FL

FROM sar.D_DEVICE
WHERE NOT (DELETED_FL)
AND DEVICE_TYPE_DS = 'Microsoft HoloLens2'
) AS device_data
ON ( user_device_lookup.DEVICE_ID = device_data.DEVICE_ID )

LEFT JOIN (
SELECT 

DEVICE_ID, 
USER_SESSION_TOKEN_ID AS SESSION_ID

FROM sar.F_DEVICE_ACTIVITY
WHERE 1=1
AND DEVICE_ID = %(device_id)s
) AS device_session
ON ( 
    user_device_lookup.DEVICE_ID = device_data.DEVICE_ID
    AND
    user_session.SESSION_ID = device_session.SESSION_ID
    )
;
"""




api_transaction_hl2_security_sql_exec_log = """
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





class api_transaction_hl2_security(api_transaction_base):
    ''' Security transaction base for HL2 integration

    This class contains the base securiy check for using the
    HL2 integration methods such as DOWNLOAD and UPLOAD. For these
    API calls, the check to perform is the same, hence it makes sense
    to enclose it into another independent class. 
    '''

    def __init__(self, env: environment, request) -> None:
        ''' Create the transaction
        
        '''
        super().__init__(env)
        
        # request comes from the API
        self.request:api_hl2_base_request = request
        # response is built during check phase
        self.response:api_hl2_base_response = None

        # CHECK response from the daabase
        self.__res:list = None
        self.__res_count:int = -1
        self.__res_schema:list = None

        # logging
        self.__log_detail_ds:str = ""
        self.__log_error:bool = False
        self.__log_unsecure_request:bool = False

        # transaction custom data
        self.success:bool = False
    

    def check( self ) -> None:
        ''' transaction check phase
        
        '''
        global api_transaction_hl2_security_sql_check

        cur = self.db.get_cursor()
        cur.execute( 
            api_transaction_hl2_security_sql_check,
            {
                'user_id' : self.request.user_id,
                'device_id' : self.request.device_id,
                'session_token' : self.request.session_token,
            }
        )
        self.__res = cur.fetchall()
        self.__res_schema = [ str(col.name).upper() for col in cur.description ]
        self.__res_count = cur.rowcount

        if self.__res_count == 0:
            self.__build_response(
                res_status=status.HTTP_404_NOT_FOUND,
                res_status_description="wrong access coordinates",
                log_detail='user not found'
            )
            return
        
        record = self.to_dict(self.__res_schema, self.__res[0])
        self.log.debug_detail(record, src="...")
        
        if not record['AUTH_HOLD_DEVICE_FL']:
            self.__build_response(
                res_status=status.HTTP_404_NOT_FOUND,
                res_status_description="wrong access coordinates",
                log_detail='user not authorized to hold devices'
            )
        elif not record['USER_AUTH_UPLOAD_FL']:
            self.__build_response(
                res_status=status.HTTP_401_UNAUTHORIZED,
                res_status_description="unauthorized",
                log_detail='user not allowed to upload'
            )
        elif not record['SESSION_ID_FOUND_FL']:
            self.__build_response(
                res_status=status.HTTP_404_NOT_FOUND,
                res_status_description="wrong access coordinates",
                log_detail='session ID not found or not correct'
            )
        elif not record['LOOKUP_DEVICE_FOUND_FL']:
            self.__build_response(
                res_status=status.HTTP_401_UNAUTHORIZED,
                res_status_description="unauthorized",
                log_detail='device seems not assigned to user'
            )
        elif not record['DEVICE_CAP_UPLOAD_FL']:
            self.__build_response(
                res_status=status.HTTP_404_NOT_FOUND,
                res_status_description="wrong access coordinates",
                log_detail='device has not capability to upload',
                unsecure_request=True
            )
        elif not record['CAP_LOCATION_FL']:
            self.__build_response(
                res_status=status.HTTP_404_NOT_FOUND,
                res_status_description="wrong access coordinates",
                log_detail='device has not capability to locate itself',
                unsecure_request=True
            )
        elif not record['DEVICE_AUTH_UPLOAD_FL']:
            self.__build_response(
                res_status=status.HTTP_401_UNAUTHORIZED,
                res_status_description="unauthorized",
                log_detail='device is not authorized to upload data'
            )
        elif not record['DEVICE_HAS_SESSION_OPENED_FL']:
            self.__build_response(
                res_status=status.HTTP_401_UNAUTHORIZED,
                res_status_description="unauthorized",
                log_detail='device seems to not have a session opened'
            )
        else:
            self.__build_response(
                res_status=status.HTTP_200_OK,
                res_status_description="success",
                log_detail='security check success'
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
        global api_transaction_hl2_security_sql_exec_log
        
        cur = self.db.get_cursor()
        cur.execute("BEGIN TRANSACTION;")

        cur.execute(
            api_transaction_hl2_security_sql_exec_log,
            {
                'LOG_TYPE_DS' : 'hl2 request',
                'LOG_TYPE_ACCESS_FL' : False,
                'LOG_SUCCESS_FL' : True,
                'LOG_WARNING_FL' : False,
                'LOG_SECURITY_FAULT_FL' : False,
                'LOG_DETAILS_DS' : self.__log_detail_ds,
                'LOG_DATA' : self.dict_to_field(dict(self.request)),
            }
        )

        cur.execute("COMMIT TRANSACTION;")
        self.success = True
    

    def __exec_fail( self ):
        global api_transaction_hl2_security_sql_exec_log
        
        cur = self.db.get_cursor()
        cur.execute("BEGIN TRANSACTION;")

        cur.execute(
            api_transaction_hl2_security_sql_exec_log,
            {
                'LOG_TYPE_DS' : 'hl2 request',
                'LOG_TYPE_ACCESS_FL' : True,
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
    ) -> api_hl2_base_response:
        self.__log_detail_ds = log_detail
        self.response = api_hl2_base_response(
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