
import psycopg2
from fastapi import status

from main_app.environment import environment
from main_app.api_models import api_user_login_request, api_user_login_response
from .transaction_base import api_transaction_base

from main_app.api_logging.logging import log
from main_app.interfaces import db_interface
import json





api_transaction_user_login_sql_check = """
SELECT -- query is empty if the user doesn't exist

-- user esiste?
user_data.USER_ID as USER_ID,
-- l'utente è esterno? (non può accederecome admin!!!)
user_data.USER_IS_EXTERNAL_FL AS USER_IS_EXTERNAL_FL,
-- lo user è un admin?
user_data.USER_ADMIN_FL AS USER_IS_ADMIN_FL,
-- is user currently active?
CASE 
    WHEN user_status.USER_ID IS NOT NULL THEN true 
    ELSE false
END AS USER_STATUS_IS_ACTIVE_FL,
-- da quando è attivo l'user?
user_status.USER_START_AT_TS AS USER_STATUS_START_AT_DT,
-- chi ha approvato l'user?
user_status.USER_APPROVER_ID AS USER_STATUS_APPROVED_BY_ID,

-- l'approvatore esiste?
user_admin_data.USER_ADMIN_ID AS ADMIN_ID,
CASE
    WHEN user_admin_data.USER_ADMIN_ID IS NOT NULL THEN true
    ELSE false
END AS ADMIN_FOUND_FL,
-- l'admin è esterno? (non può essere che si usi come admin un esterno)
user_admin_data.USER_IS_EXTERNAL_FL AS ADMIN_EXTERNAL_FL,
-- l'approvatore è admin? 
user_admin_data.USER_ADMIN_FL AS ADMIN_IS_ADMIN_FL,
-- approvatore dichiarato per l'utente coincide con l'utente nella API?
CASE
    WHEN user_data.USER_APPROVED_BY_ID IS NULL AND user_data.USER_ADMIN_FL THEN true
    WHEN COALESCE( user_data.USER_APPROVED_BY_ID, 'N/A' ) = COALESCE( user_admin_data.USER_ADMIN_ID, 'N/A' ) THEN true
    ELSE false
END AS USER_APPROVER_CORRECT_FL,
-- l'approvatore possiede diritti di accesso almeno in lettura sull'utente?
user_admin_data.AUTH_ACCESS_USER_FL AS ADMIN_CAN_ACCESS_USER_FL,
-- is the admin currently active if there's a admin for this request?
CASE
    WHEN user_status_admin.USER_ADMIN_ID IS NOT NULL THEN true
    ELSE false
END AS ADMIN_STATUS_IS_ACTIVE_FL,

-- is the user's access key correct?
CASE 
    WHEN user_access.USER_ID IS NOT NULL THEN true
    ELSE false
END AS USER_STATUS_PASS_CHECK_FL

FROM ( -- base user
SELECT 

USER_ID,
USER_ADMIN_FL,
USER_IS_EXTERNAL_FL,
USER_APPROVED_BY_ID

FROM sar.D_USER 
WHERE 1=1
AND USER_ID=%(user_id)s -- REQUEST (user_id)
AND NOT(DELETED_FL)
) AS user_data

LEFT JOIN ( -- admin user
SELECT 

USER_ID AS USER_ADMIN_ID,
USER_ADMIN_FL,
AUTH_ACCESS_USER_FL,
USER_IS_EXTERNAL_FL

FROM sar.D_USER
WHERE NOT(DELETED_FL)
AND USER_ID = %(approver_id)s -- REQUEST (approver_id)
) as user_admin_data
ON ( 1=1 )

LEFT JOIN ( -- check user status
SELECT

USER_ID,
USER_APPROVER_ID,
USER_START_AT_TS

FROM sar.F_USER_ACTIVITY
WHERE 1=1
AND USER_END_AT_TS IS NULL
) AS user_status
ON ( user_data.USER_ID = user_status.USER_ID )

LEFT JOIN ( -- check admin approver status
SELECT

USER_ID AS USER_ADMIN_ID,
USER_START_AT_TS

FROM sar.F_USER_ACTIVITY
WHERE 1=1
AND USER_END_AT_TS IS NULL
AND USER_ID = USER_APPROVER_ID -- admin user
) AS user_status_admin
ON ( user_admin_data.USER_ADMIN_ID = user_status_admin.USER_ADMIN_ID )

LEFT JOIN ( -- check user pass
SELECT 

USER_ID,
USER_ACCESS_CODE_ID

FROM sar.D_USER_ACCESS_DATA
WHERE NOT(DELETED_FL)
) AS user_access
ON ( 
    user_data.USER_ID = user_access.USER_ID
    AND
    MD5(%(access_key)s) = user_access.USER_ACCESS_CODE_ID -- REQUEST (access_key)
    ) 
;
"""





api_transaction_user_login_sql_exec_open_session = """
INSERT INTO sar.F_USER_ACTIVITY (
    USER_ID, 
    USER_APPROVER_ID,
    
    USER_SESSION_TOKEN_ID
)
VALUES (
    %(user_id)s,
    %(approver_id)s,

    MD5( CONCAT(
        FLOOR(RANDOM() * 1000000), 
        %(user_id)s,
        FLOOR(RANDOM() * 1000000), 
        %(approver_id)s, 
        FLOOR(RANDOM() * 1000000)
    ) )
);
"""





api_transaction_user_login_sql_exec_set_log = """
INSERT INTO sar.F_ACTIVITY_LOG (
    LOG_TYPE_DS, LOG_TYPE_ACCESS_FL, LOG_SUCCESS_FL, LOG_WARNING_FL, LOG_ERROR_FL, LOG_SECURITY_FAULT_FL,
    LOG_SOURCE_ID,
    LOG_DETAILS_DS,
    LOG_DATA
)
VALUES (
    %(log_type_ds)s, true, %(log_success_fl)s, %(log_security_fault_fl)s, false, %(log_security_fault_fl)s,
    'api',
    %(log_detail_ds)s,
    %(log_data)s
)
"""





class api_transaction_user_login(api_transaction_base):
    ''' implementation of the transaction login
    
    How to use this class:

    1. class init
    2. CHECK phase -- a response is formulated here from the results from the DB
        the check_done flag is set to true only when the response is created
    3. EXEC phase -- using the response created before
        this step will fail if CHECK has not beel called before
    '''

    def __init__(self, env: environment, request:api_user_login_request) -> None:
        ''' Creae the transaction
        
        '''
        super().__init__(env)
        self.request:api_user_login_request = request
        self.response:api_user_login_response = None
        self.__res:list = None
        self.__res_count:int = -1
        self.__res_schema:list = None
        self.__log_detail_ds = ""
        self.__log_error:bool = False
        self.__log_unsecure_request = False

    def check( self ):
        ''' transaction check phase
        
        '''
        global api_transaction_user_login_sql_check

        cur = self.db.get_cursor()
        cur.execute( 
            api_transaction_user_login_sql_check,
            {
                'user_id' : self.request.user_id,
                'approver_id' : self.request.approver_id,
                'access_key' : self.request.access_key
            }
        )
        self.__res = cur.fetchall()
        self.__res_schema = [ str(col.name).upper() for col in cur.description ]
        self.__res_count = cur.rowcount

        if self.__res_count == 0:
            return self.__build_response(
                res_status=status.HTTP_404_NOT_FOUND,
                res_status_description="incorrect user, admin or pass key",
                log_detail='not found user from request'
            )
        
        record = self.to_dict(self.__res_schema, self.__res[0])
        self.log.debug_detail(record, src="transaction_user_login")
        if self.request.user_id == self.request.approver_id and not record['USER_IS_ADMIN_FL']:
            return self.__build_response(
                res_status=status.HTTP_404_NOT_FOUND,
                res_status_description="incorrect user, admin or pass key",
                log_detail='trying to access as admin without admin flag'
            )
        elif self.request.user_id == self.request.approver_id and record['USER_IS_EXTERNAL_FL']:
            return self.__build_response(
                res_status=status.HTTP_404_NOT_FOUND,
                res_status_description="incorrect user, admin or pass key",
                log_detail='a external user can\'t access as admin',
                unsecure_request=True
            )
        elif not record['ADMIN_FOUND_FL']:
            return self.__build_response(
                res_status=status.HTTP_404_NOT_FOUND,
                res_status_description="incorrect user, admin or pass key",
                log_detail='not found admin from request'
            )
        elif not record['ADMIN_IS_ADMIN_FL']:
            return self.__build_response(
                res_status=status.HTTP_404_NOT_FOUND,
                res_status_description="incorrect user, admin or pass key",
                log_detail='trying to use admin code referred to non-admin user'
            )
        elif record['ADMIN_EXTERNAL_FL']:
            return self.__build_response(
                res_status=status.HTTP_404_NOT_FOUND,
                res_status_description="incorrect user, admin or pass key",
                log_detail='trying to use a external account as admin for a login operation'
            )
        elif not record['USER_STATUS_PASS_CHECK_FL']:
            return self.__build_response(
                res_status=status.HTTP_404_NOT_FOUND,
                res_status_description="incorrect user, admin or pass key",
                log_detail='wrong password'
            )
        elif not record['ADMIN_CAN_ACCESS_USER_FL']:
            return self.__build_response(
                res_status=status.HTTP_401_UNAUTHORIZED,
                res_status_description="incorrect user, admin or pass key",
                log_detail='trying to access with a admin which is not auhorized to read user data'
            )
        elif record['USER_STATUS_IS_ACTIVE_FL']:
            if record['USER_STATUS_APPROVED_BY_ID'] != self.request.approver_id:
                return self.__build_response(
                    res_status=status.HTTP_403_FORBIDDEN,
                    res_status_description="access denied",
                    log_detail='session active with one approver, but required the access with another approver',
                    unsecure_request=True
                )
            else:
                return self.__build_response(
                    res_status=status.HTTP_403_FORBIDDEN,
                    res_status_description="nothing to do",
                    log_detail='trying to access a user already logged in'
                )
        else:
            return self.__build_response(
                res_status=status.HTTP_200_OK,
                res_status_description="user successfully logged in",
                log_detail='user successfully logged in'
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
        global api_transaction_user_login_sql_exec_open_session
        global api_transaction_user_login_sql_exec_set_log
        
        cur = self.db.get_cursor()
        cur.execute("BEGIN TRANSACTION;")

        # open user session
        cur.execute(
            api_transaction_user_login_sql_exec_open_session,
            {
                'user_id' : self.request.user_id,
                'approver_id' : self.request.approver_id,
            }
        )

        # record on log
        cur.execute(
            api_transaction_user_login_sql_exec_set_log,
            {
                'log_type_ds' : 'login success',
                'log_success_fl' : 'true',
                'log_security_fault_fl' : 'false',
                'log_detail_ds' : self.__log_detail_ds,
                'log_data' : self.dict_to_field(dict(self.request)),
            }
        )

        cur.execute("COMMIT TRANSACTION;")

    def __exec_fail( self ):
        global api_transaction_user_login_sql_exec_set_log
        cur = self.db.get_cursor()        

        # record on log
        cur.execute("BEGIN TRANSACTION;")
        cur.execute(
            api_transaction_user_login_sql_exec_set_log,
            {
                'log_type_ds' : 'login fail',
                'log_success_fl' : 'false',
                'log_security_fault_fl' : self.__log_unsecure_request,
                'log_detail_ds' : self.__log_detail_ds,
                'log_data' : self.dict_to_field(dict(self.request)),
            }
        )
        cur.execute("COMMIT TRANSACTION;")

    def __build_response( self, res_status:int, res_status_description:str, log_detail:str, unsecure_request:bool=False ) -> api_user_login_response:
        self.__log_detail_ds = log_detail
        self.response = api_user_login_response(
            timestamp_received = self.request.timestamp,
            status = res_status,
            status_detail = res_status_description,
        )
        self.__log_error = ( res_status not in ( status.HTTP_200_OK, status.HTTP_202_ACCEPTED, status.HTTP_100_CONTINUE ) )
        self.__log_unsecure_request = unsecure_request

        self.__check_done = True
        return self.response



