
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