
import psycopg2
from fastapi import status
from main_app.environment import environment
from main_app.api_logging.logging import log
from main_app.interfaces import db_interface
from .transaction_base import api_transaction_base
from main_app.api_models import api_device_logout_request, api_device_logout_response
import json

api_transaction_device_logout_sql_check= """
SELECT 

-- l'utente esiste
CASE
    WHEN user_data.USER_ID IS NOT NULL THEN true
    ELSE false
END AS USER_EXISTS_FL

-- il device esiste
,CASE
    WHEN device_data.DEVICE_ID IS NOT NULL THEN true
    ELSE false
END AS DEVICE_EXISTS_FL

-- l'utente può avere un device
, CASE
    WHEN user_data.AUTH_HOLD_DEVICE_FL IS NULL THEN null
    ELSE user_data.AUTH_HOLD_DEVICE_FL
END AS USER_CAN_HOLD_DEVICE_FL

-- il device può essere preso da un utente?
, device_data.DEVICE_IS_HOLDABLE_FL AS DEVICE_IS_HOLDABLE_FL

-- il token è correto e l'utente è online
,CASE
    WHEN user_session.USER_ID IS NOT NULL THEN true
    ELSE false
END AS USER_IS_LOGGED_ID_FL

-- l'utente sta usando il device
, CASE 
    WHEN device_session.DEVICE_ID IS NULL THEN false
    ELSE TRUE
END AS DEVICE_IS_ALREADY_HOLD_FL

-- in caso, è l'utente stesso che lo sta utilizzando?
, CASE 
    WHEN device_session.DEVICE_ID IS NULL THEN null
    WHEN device_session.USER_SESSION_TOKEN_ID <> user_session.USER_SESSION_TOKEN_ID THEN true
    ELSE false
END AS DEVICE_IS_HOLD_BY_DIFFERENT_USER

FROM ( -- device data, capabilities and authorizations
SELECT

DEVICE_ID,
DEVICE_IS_HOLDABLE_FL

FROM sar.D_DEVICE
WHERE NOT(DELETED_FL)
AND DEVICE_ID = %(device_id)s -- REQUEST (device_id)
) AS device_data

LEFT JOIN ( -- user data and authorizations
SELECT

USER_ID,
AUTH_HOLD_DEVICE_FL

FROM sar.D_USER
WHERE NOT(DELETED_FL)
AND USER_ID = %(user_id)s -- REQUEST (user_id)
) AS user_data
ON ( 1=1 )

LEFT JOIN ( -- user opened session check
SELECT

USER_ID,
USER_SESSION_TOKEN_ID

FROM sar.F_USER_ACTIVITY
WHERE 1=1
AND USER_END_AT_TS IS NULL
AND USER_ID = %(user_id)s -- REQUEST (user_id)
AND USER_SESSION_TOKEN_ID = %(session_token)s -- REQUEST (session_token)
) AS user_session
ON ( 1=1 )

LEFT JOIN ( -- device status
SELECT

DEVICE_ID,
USER_SESSION_TOKEN_ID

FROM sar.F_DEVICE_ACTIVITY
WHERE 1=1
AND DEVICE_OFF_AT_TS IS NULL
AND DEVICE_ID = %(device_id)s -- REQUEST (device_id)
) AS device_session
ON ( 1=1 )
;
"""




api_transaction_device_logout_sql_exec_close_session = """
UPDATE sar.F_DEVICE_ACTIVITY
SET 
    DEVICE_OFF_AT_TS = CURRENT_TIMESTAMP
WHERE 1=1
    AND USER_SESSION_TOKEN_ID = %(session_token)s
    AND DEVICE_ID = %(device_id)s
    AND DEVICE_OFF_AT_TS IS NULL
;
"""




api_transaction_device_logout_sql_exec_log = """
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
);
"""





class api_transaction_device_logout(api_transaction_base):
    ''' Device Logout Transaction

    This transaction allows a user to release a holding device. 
    '''

    def __init__(self, env: environment, request:api_device_logout_request) -> None:
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
        global api_transaction_device_logout_sql_check

        cur = self.db.get_cursor()
        cur.execute( 
            api_transaction_device_logout_sql_check,
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
                res_status_description="incorrect user, device or token",
                log_detail='no data found from the request'
            )
            return
        
        record = self.to_dict(self.__res_schema, self.__res[0])
        self.log.debug_detail(record, src="transaction_device_logout")
        
        if not record['USER_EXISTS_FL']:
            self.__build_response(
                res_status=status.HTTP_404_NOT_FOUND,
                res_status_description="incorrect user, device or token",
                log_detail='unknown user id'
            )
            return
        elif not record['DEVICE_EXISTS_FL']:
            self.__build_response(
                res_status=status.HTTP_404_NOT_FOUND,
                res_status_description="incorrect user, device or token",
                log_detail='unknown device id'
            )
            return
        elif not record['USER_IS_LOGGED_ID_FL']:
            self.__build_response(
                res_status=status.HTTP_404_NOT_FOUND,
                res_status_description="incorrect user, device or token",
                log_detail='user not logged in, cannot find session ID'
            )
            return
        elif not record['USER_CAN_HOLD_DEVICE_FL']:
            self.__build_response(
                res_status=status.HTTP_401_UNAUTHORIZED,
                res_status_description="incorrect user, device or token",
                log_detail='device cannot be assigned since it is not holdable'
            )
            return
        elif not record['DEVICE_IS_HOLDABLE_FL']:
            self.__build_response(
                res_status=status.HTTP_404_NOT_FOUND,
                res_status_description="incorrect user, device or token",
                log_detail='device cannot be assigned since it is not holdable'
            )
            return
        elif not record['DEVICE_IS_ALREADY_HOLD_FL']:
            self.__build_response(
                res_status=status.HTTP_404_NOT_FOUND,
                res_status_description="incorrect user, device or token",
                log_detail='the user has not a session opened for this device'
            )
            return
        elif record['DEVICE_IS_ALREADY_HOLD_FL'] and record['DEVICE_IS_HOLD_BY_DIFFERENT_USER']:
            self.__build_response(
                res_status=status.HTTP_401_UNAUTHORIZED,
                res_status_description="access denied",
                log_detail='another user is already holding this device'
            )
            return
        else:
            self.__build_response(
                res_status=status.HTTP_200_OK,
                res_status_description="success",
                log_detail='device {} logout success'.format(self.request.device_id)
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
            self.log.err(f"Execution error during EXEC phase! {e}", src="transaction_device_logout")
            self.db.execute("ROLLBACK TRANSACTION;")
    

    def __exec_success( self ):
        global api_transaction_device_logout_sql_exec_close_session
        global api_transaction_device_logout_sql_exec_log
        
        cur = self.db.get_cursor()
        cur.execute("BEGIN TRANSACTION;")

        cur.execute(
            api_transaction_device_logout_sql_exec_close_session,
            {
                'device_id' : self.request.device_id,
                'session_token' : self.request.session_token,
            }
        )

        cur.execute(
            api_transaction_device_logout_sql_exec_log,
            {
                'LOG_TYPE_DS' : 'device logout',
                'LOG_TYPE_ACCESS_FL' : True,
                'LOG_SUCCESS_FL' : True,
                'LOG_WARNING_FL' : False,
                'LOG_SECURITY_FAULT_FL' : False,
                'LOG_DETAILS_DS' : self.__log_detail_ds,
                'LOG_DATA' : self.dict_to_field(dict(self.request)),
            }
        )

        cur.execute("COMMIT TRANSACTION;")
    

    def __exec_fail( self ):
        global api_transaction_device_logout_sql_exec_log
        
        cur = self.db.get_cursor()
        cur.execute("BEGIN TRANSACTION;")

        cur.execute(
            api_transaction_device_logout_sql_exec_log,
            {
                'LOG_TYPE_DS' : 'device logout',
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
    ) -> api_device_logout_response:
        self.__log_detail_ds = log_detail
        self.response = api_device_logout_response(
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

