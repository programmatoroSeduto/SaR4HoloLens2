
import psycopg2
from fastapi import status
from main_app.environment import environment
from main_app.api_logging.logging import log
from main_app.interfaces import db_interface
from .transaction_base import api_transaction_base
from main_app.api_models import api_user_logout_request, api_user_logout_response
import json

api_transaction_user_logout_sql_check= """
SELECT 

-- user exists
user_data.USER_ID AS USER_ID,
-- user is admin
user_data.USER_ADMIN_FL AS USER_ADMIN_FL,
-- user has approved users
CASE 
    WHEN user_data.USER_ADMIN_FL THEN online_users_count.COUNT_APPROVED_USERS_VL 
    ELSE NULL
END AS COUNT_APPROVED_USERS_VL,
-- session is opened
CASE
    WHEN user_status.USER_SESSION_TOKEN_ID IS NOT NULL THEN true
    ELSE false
END AS USER_HAS_SESSION_OPENED_FL,
-- session ID is correct (check directly from API service)
-- user_status.USER_SESSION_TOKEN_ID,
CASE 
    WHEN user_status.USER_SESSION_TOKEN_ID IS NULL THEN false
    WHEN user_status.USER_SESSION_TOKEN_ID = %(session_token)s THEN true
    ELSE false
END AS USER_SESSION_TOKEN_VALID_FL,

-- (useful for other steps) the user can hold some device?
COALESCE(AUTH_HOLD_DEVICE_FL, false) AS AUTH_HOLD_DEVICE_FL

FROM ( -- user
SELECT 

USER_ID,
USER_ADMIN_FL,
USER_IS_EXTERNAL_FL,
AUTH_HOLD_DEVICE_FL

FROM sar.D_USER 
WHERE 1=1
AND USER_ID=%(user_id)s -- REQUEST (user_id)
AND NOT(DELETED_FL)
) AS user_data

LEFT JOIN ( -- check user status
SELECT

USER_ID,
USER_APPROVER_ID,
USER_START_AT_TS,
USER_SESSION_TOKEN_ID

FROM sar.F_USER_ACTIVITY
WHERE 1=1
AND USER_END_AT_TS IS NULL
) AS user_status
ON ( user_data.USER_ID = user_status.USER_ID )

LEFT JOIN ( -- count users approved by this admin
SELECT 

COUNT(*) AS COUNT_APPROVED_USERS_VL

FROM sar.F_USER_ACTIVITY
WHERE 1=1
AND USER_END_AT_TS IS NULL
AND USER_APPROVER_ID = %(user_id)s -- REQUEST (user_id)
AND USER_ID <> USER_APPROVER_ID -- exclude the admin himself
) AS online_users_count
ON ( 1=1 )
;
"""




api_transaction_user_logout_sql_exec_close_devices_sessions = """
UPDATE sar.F_DEVICE_ACTIVITY
SET 
    DEVICE_OFF_AT_TS = CURRENT_TIMESTAMP
WHERE USER_SESSION_TOKEN_ID = %(session_token)s
RETURNING 
    DEVICE_ID
;
"""




api_transaction_user_logout_sql_exec_close_user_session = """
UPDATE sar.F_USER_ACTIVITY
SET
    USER_END_AT_TS = CURRENT_TIMESTAMP
WHERE USER_SESSION_TOKEN_ID = %(session_token)s
;
"""




api_transaction_user_logout_sql_exec_log = """
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





class api_transaction_user_logout(api_transaction_base):
    ''' User Logout Transaction implementation

    This transaction is used when the user logs out its user account. 
    '''

    def __init__(self, env: environment, request:api_user_logout_request) -> None:
        ''' Creae the transaction
        
        '''
        super().__init__(env)
        
        # request comes from the API
        self.request:api_user_logout_request = request
        # response is built during check phase
        self.response:api_user_logout_response = None

        # CHECK response from the daabase
        self.__res:list = None
        self.__res_count:int = -1
        self.__res_schema:list = None

        # logging
        self.__log_detail_ds:str = ""
        self.__log_error:bool = False
        self.__log_unsecure_request:bool = False
        
        # custom transaction data
        self.user_has_auth_devices:bool = False
        self.logged_out_devices:list[str] = list()
    

    def check( self ): 
        ''' transaction check phase
        
        '''
        global api_transaction_user_logout_sql_check

        cur = self.db.get_cursor()
        cur.execute( 
            api_transaction_user_logout_sql_check,
            {
                'user_id' : self.request.user_id,
                'session_token' : self.request.session_token
            }
        )
        self.__res = cur.fetchall()
        self.__res_schema = [ str(col.name).upper() for col in cur.description ]
        self.__res_count = cur.rowcount

        if self.__res_count == 0:
            self.__build_response(
                res_status=status.HTTP_404_NOT_FOUND,
                res_status_description="invalid user or token",
                log_detail='wrong user in logout request'
            )
            return
        
        record = self.to_dict(self.__res_schema, self.__res[0])
        self.log.debug_detail(record, src="transaction_user_logout")
        self.user_has_auth_devices = record['AUTH_HOLD_DEVICE_FL']

        if not record['USER_HAS_SESSION_OPENED_FL']:
            self.__build_response(
                res_status=status.HTTP_401_UNAUTHORIZED,
                res_status_description="invalid user or token",
                log_detail='missing session'
            )
        elif not record['USER_SESSION_TOKEN_VALID_FL']:
            self.__build_response(
                res_status=status.HTTP_401_UNAUTHORIZED,
                res_status_description="invalid user or token",
                log_detail='unvalid session token'
            )
        elif record['USER_ADMIN_FL'] and record['COUNT_APPROVED_USERS_VL'] > 0:
            self.__build_response(
                res_status=status.HTTP_401_UNAUTHORIZED,
                res_status_description="can't log out",
                log_detail='there are still {} logged user depending to this admin; can\'t accomplish request'.format(record['COUNT_APPROVED_USERS_VL'])
            )
        else:
            self.__build_response(
                res_status=status.HTTP_200_OK,
                res_status_description="success",
                log_detail='successfully logged out'
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
        global api_transaction_user_logout_sql_exec_close_devices_sessions
        global api_transaction_user_logout_sql_exec_close_user_session
        global api_transaction_user_logout_sql_exec_log
        
        cur = self.db.get_cursor()
        cur.execute("BEGIN TRANSACTION;")
        
        if self.user_has_auth_devices:
            cur.execute(
                api_transaction_user_logout_sql_exec_close_devices_sessions,
                {
                    'session_token' : self.request.session_token
                }
            )
            self.logged_out_devices = cur.fetchall()
        
        cur.execute(
            api_transaction_user_logout_sql_exec_close_user_session,
            {
                'session_token' : self.request.session_token
            }
        )

        self.request.session_token = "..."
        if self.user_has_auth_devices and len(self.logged_out_devices) > 0:
            for device in self.logged_out_devices:
                cur.execute(
                    api_transaction_user_logout_sql_exec_log,
                    {
                        'LOG_TYPE_DS' : 'device logout',
                        'LOG_TYPE_ACCESS_FL' : True,
                        'LOG_SUCCESS_FL' : True,
                        'LOG_WARNING_FL' : False,
                        'LOG_SECURITY_FAULT_FL' : False,
                        'LOG_DETAILS_DS' :"device '{}' logout success".format(device),
                        'LOG_DATA' : self.dict_to_field(dict(self.request)),
                    }
                )
            self.response.logged_out_devices = self.logged_out_devices
        
        cur.execute(
            api_transaction_user_logout_sql_exec_log,
            {
                'LOG_TYPE_DS' : 'user logout',
                'LOG_TYPE_ACCESS_FL' : True,
                'LOG_SUCCESS_FL' : True,
                'LOG_WARNING_FL' : False,
                'LOG_SECURITY_FAULT_FL' : False,
                'LOG_DETAILS_DS' :"user successfully logged out",
                'LOG_DATA' : self.dict_to_field(dict(self.request)),
            }
        )

        cur.execute("COMMIT TRANSACTION;")
    

    def __exec_fail( self ):
        global api_transaction_user_logout_sql_exec_log
        
        cur = self.db.get_cursor()
        cur.execute("BEGIN TRANSACTION;")

        cur.execute(
            api_transaction_user_logout_sql_exec_log,
            {
                'LOG_TYPE_DS' : 'user logout',
                'LOG_TYPE_ACCESS_FL' : True,
                'LOG_SUCCESS_FL' : False,
                'LOG_WARNING_FL' : False,
                'LOG_SECURITY_FAULT_FL' : self.__log_unsecure_request,
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
    ) -> api_user_logout_response:
        self.__log_detail_ds = log_detail
        self.response = api_user_logout_response(
            timestamp_received = self.request.timestamp,
            status = res_status,
            status_detail = res_status_description,
        )
        self.__log_error = ( res_status not in ( 
            status.HTTP_200_OK, 
            status.HTTP_202_ACCEPTED, 
            status.HTTP_100_CONTINUE,
            status.HTTP_418_IM_A_TEAPOT,
            ) )
        self.__log_unsecure_request = unsecure_request

        self.__check_done = True
        return self.response

