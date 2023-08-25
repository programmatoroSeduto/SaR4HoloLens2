
import psycopg2
from main_app.environment import environment
from main_app.api_logging.logging import log
from main_app.interfaces import db_interface
import json

class ud_security_support:
    '''A suporto for HL2 security for U.D. Protocol.
    
    Please give a look at the workbook 'du_security_transactions.sql'
    inside the DB area to understand in detail hao this class works. 
    '''



    def __init__( self, env:environment ) -> None:
        ''' Init ud_security_support class
        
        Fields included:
        - log : the logging utility (shared reference)
        - db : the database connection (shared reference)

        '''
        self.db:db_interface = env.db
        self.log:log = env.log



    def try_get_real_token_from_fake(self, user_id, device_id, owner_token, fake_token) -> str:
        ''' try to get the real token from a fake token
        
        the function tries to get a real session token from fake token. 
        If the function fails, it returns None, and the meaning of this
        return value depends on the situation the check is runned out. 

        TODO
        - cache system : to avoid useless requests to the database
        '''

        found, data, _, _ = self.__extract_from_db(
            """
            SELECT 
                USER_SESSION_TOKEN_ID 
            FROM sar.F_SESSION_ALIAS
            WHERE 1=1
            AND USER_ID  = %(user_id)s
            AND DEVICE_ID  = %(device_id)s
            AND OWNER_SESSION_TOKEN_ID = %(owner_token)s
            AND FAKE_SESSION_TOKEN_ID = %(fake_token)s;
            """,
            {
                'user_id' : user_id,
                'device_id' : device_id,
                'owner_token' : owner_token,
                'fake_token' : fake_token
            }
        )

        if not found:
            return None
        else:
            return data[0]['USER_SESSION_TOKEN_ID']



    def get_fake_token_infos(self, fake_token) -> (bool, str, str, str, str, str):
        ''' Get all the informations related to the fake token if it exists
        
        Return in not exising case:
            (False, None, None, None, None, None)
        Return when the token is found (first available):
            (True, 'user_id', 'device_id', 'owner_session_token', 'user_session_token', 'salt')
        '''
        return (False, None, None, None, None, None)



    def has_fake_token(self, user_id, device_id, owner_token) -> (bool, str, str, str):
        '''Check if the user has or not a fake token associated to its login
        
        Return in not exising case:
            (False, None, None, None)
        Return when the token is found (first available):
            (True, 'user_session_token', 'fake_session_token', 'salt')
        Names:
            found, user_session_token, fake_session_token, salt
        '''

        found, data, _, _ = self.__extract_from_db(
            """
            SELECT 
                USER_SESSION_TOKEN_ID, FAKE_SESSION_TOKEN_ID, SALT_ID 
            FROM sar.F_SESSION_ALIAS
            WHERE 1=1
            AND USER_ID  = %(user_id)s
            AND DEVICE_ID  = %(device_id)s
            AND OWNER_SESSION_TOKEN_ID = %(owner_token)s
            LIMIT 1; -- a USER SHOULD have ONLY one fake TOKEN opened FOR the device...
            """,
            {
                'user_id' : user_id,
                'device_id' : device_id,
                'owner_token' : owner_token
            }
        )

        if not found:
            return (False, None, None, None)
        else:
            return (True, data[0]['USER_SESSION_TOKEN_ID'], data[0]['FAKE_SESSION_TOKEN_ID'], data[0]['SALT_ID'])


    def create_fake_session_token(self, user_id, device_id, owner_token, user_token) -> bool:
        cur = self.db.get_cursor()
        _, data, _, _ = self.__extract_from_db(
            """
            INSERT INTO sar.F_SESSION_ALIAS 
            SELECT 
                %(user_id)s AS USER_ID ,
                %(device_id)s AS DEVICE_ID ,
                %(owner_token)s AS OWNER_SESSION_TOKEN_ID ,
                data_tab.session_to_hide AS USER_SESSION_TOKEN_ID ,
                data_tab.salt_code AS SALT_ID ,
                MD5(
                    CONCAT( data_tab.salt_code, data_tab.session_to_hide, data_tab.salt_code )
                ) AS FAKE_SESSION_TOKEN_ID
            FROM (
                SELECT 
                    MD5(FLOOR(RANDOM()*100000000)::TEXT) AS salt_code,
                    %(user_token)s AS session_to_hide
            ) AS data_tab 
            RETURNING
                FAKE_SESSION_TOKEN_ID;
            """,
            {
                'user_id' : user_id,
                'device_id' : device_id,
                'user_token' : user_token,
                'owner_token' : owner_token
            }
        )
        return data[0]['FAKE_SESSION_TOKEN_ID']



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