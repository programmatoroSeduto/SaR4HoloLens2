
import psycopg2
import exceptions
import api_models

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

    def get_cursor(self) -> psycopg2.cursor:
        ''' Get the reference to the psycopg2 cursor class
        
        '''
        if not self.__init_done or self.__connection is None:
            return None
        else:
            return self.__connection.cursor()

    def sql(self, query:str="", fetch_max_size:int =-1) -> api_models.table_base:
        ''' Perform a query on the database
        
        the method allows to extract results of a query from the database.
        Results are returned using the Pydantic model table_base. 

        Arguments:
            query: str
                the SQL code to use for extracting the results from the
                database. 
            fetch_max_size: int
                if greater than zero, the method fetches only a limited number
                of lines from the query. Default: -1, it causes the method to
                download the entire table
        '''
        if not self.__init_done or self.__connection is None:
            print("WARNING: Trying to get cursor from a not connected system")
            return None
        
        cur = self.__connection.cursor()
        try:
            cur.execute(query)
            extract = ( cur.fetchmany(size=fetch_max_size) if fetch_max_size>=0 else cur.fetchall() )
            return api_models.table_base(
                status=True,
                status_detail="",
                query=query,
                table_schema=[ str(col.name) for col in cur.description ],
                table_size=cur.rowcount,
                table_values=[ tuple([ str(val) for val in row]) for row in extract ]
            )
        except psycopg2.errors.UndefinedTable as UndefinedTable:
            return api_models.table_base(
                status=False,
                status_detail=str(UndefinedTable),
                query=query
            )
        except psycopg2.errors.SyntaxError as SyntaxError:
            return api_models.table_base(
                status=False,
                status_detail=str(SyntaxError),
                query=query
            )
        except Exception as e:
            return api_models.table_base(
                status=False,
                status_detail="UNHANDLED EXCEPTION!" + str(e),
                query=query
            )
        



