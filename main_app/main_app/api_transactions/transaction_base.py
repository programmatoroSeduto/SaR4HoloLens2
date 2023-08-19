
import psycopg2
from main_app.environment import environment
from main_app.api_logging.logging import log
from main_app.interfaces import db_interface

class api_transaction_base:
    ''' base class for DB transactions
    
    A transaction represents a operation from the API to the database, which is 
    enclosed in one class. 
    This kind of class is istanced when the API has to perform a transaction with 
    the database. Here's the general lifecycle of a Transaction class:

    1. INIT -- the class is instanced passing it the environment
    2. TRANSACTION CHECKS -- the class performs the check part of the transaction
    3. TRANSACTION EXECUTION -- the class modifies the database status according with
        the result of the CHECK phase. 
    
    Each transaction is define in two parts at least:

    1. the CHECK phase
        the class checks if there are the conditions to perform that operation. 
    2. the EXECUTION phase
        the class performs operations and logging, according to the results 
        obtained in the CHECK phase
    
    Since transactions share the same general structure, a base class is provided
    to put togethere all the common functionalities. 
    '''
    
    def __init__( self, env:environment ) -> None:
        ''' Init base transaction class
        
        Fields included:
        - log : the logging utility (shared reference)
        - db : the database connection (shared reference)

        '''
        self.db:db_interface = env.db
        self.log:log = env.log
        self.__check_done:bool = False
    
    def check( self ):
        ''' transaction check phase
        
        '''
        raise NotImplementedError()
    
    def execute( self ):
        ''' transaction execution phase
        
        '''
        raise NotImplementedError()

    def to_dict( self, fields:list[str], values:list[str] ):
        return dict(zip(fields, values))