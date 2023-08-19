
import psycopg2
from fastapi import status

from main_app.main_app.environment import environment
from main_app.api_models import api_user_login_request, api_user_login_response
from .transaction_base import api_transaction_base

from main_app.api_logging.logging import log
from main_app.interfaces import db_interface



__api_transaction_user_login_sql_check = """
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
    WHEN NVL( user_data.USER_APPROVED_BY_ID, 'N/A' ) = NVL( user_admin_data.USER_ADMIN_ID, 'N/A' ) THEN true
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



class api_transaction_user_login(api_transaction_base):
    ''' implementation of the transaction login
    
    How to use this class:

    1. class init
    2. 
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
        self.__log_exec = ""
    
    def check( self ):
        ''' transaction check phase
        
        '''
        global __api_transaction_user_login_sql_check

        cur = self.db.get_cursor()
        cur.execute( 
            __api_transaction_user_login_sql_check,
            {
                'user_id' : self.request.user_id,
                'approver_id' : self.request.approver_id,
                'access_key' : self.request.access_key
            }
        )
        self.__res = cur.fetchall()
        self.__res_schema = [ str(col.name) for col in cur.description ]
        self.__res_count = cur.rowcount

        if self.__res_count == 0:
            return self.__build_response(
                res_status=status.HTTP_404_NOT_FOUND,
                res_status_description="incorrect user, admin or pass key"
            )
        record = self.__res[0]



        
    
    def execute( self ):
        ''' transaction execution phase
        
        '''
        raise NotImplementedError()


    def __build_response( self, res_status:int, res_status_description:str ) -> api_user_login_response:
        self.response = api_user_login_response(
            timestamp_received = self.request.timestamp,
            status = res_status,
            status_detail = res_status_description,
        )
        return self.response