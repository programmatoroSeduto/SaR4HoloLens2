
import psycopg2
import exceptions

class db_interface:
    ''' communication with the database
    
    This class allows to communicate with the database 
    usig direct queries. The object is meant to be shared
    across the project. 
    '''

    def __init__(self, conn = None, logger = None) -> None:
        ''' simple constructor
        
        It prepares the class. To perform the connection, it is required
        to call Init(). 
        '''
        self.__connection = conn
        self.__init_done = False
        self.__logger = logger

    def connect(self, conn_data: dict = {}) -> bool:
        ''' connection to the database
        
        the method tries to connect to the database with the infos provided
        as py dictionary. 

        :raises: ConnectionException: wrong connection data
        '''
        if self.__init_done == True:
            return True

        try:
            self.__connection = psycopg2.connect( **conn_data )
            self.__init_done = True
        except Exception as e:
            self.__connection = None
            exc = exceptions.connection_exception()
            exc.description = str(e)
            raise exc
        
        return True

    def get_cursor(self):
        ''' Get the reference to the psycopg2 cursor class
        
        '''
        if not self.__init_done or self.__connection is None:
            return None
        else:
            return self.__connection.cursor()
        



