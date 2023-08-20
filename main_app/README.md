# Main App

## HOW TO implement a DB transaction

all the transactions are collected under the folder `api_transactions`. Here you will find:

- `__init__.py` : package placeholder (empty)
- `transaction_base.py` : it contains the class `api_transaction_base` used as base class for the transactions due to ther similar structure. 
- some other files containing classes derived from `api_transaction_base`

### What the hell is a transaction???

A transaction represents a operation from the API to the database, which is enclosed in one class. It is the conceptual model to think a operation on the database, which can be complex. 

The transaction class is istanced when the API has to perform a transaction with the database. Here's the general lifecycle of a Transaction class:

1. INIT -- the class is instanced passing it the environment
2. TRANSACTION CHECKS -- the class performs the check part of the transaction
3. TRANSACTION EXECUTION -- the class modifies the database status according with
    the result of the CHECK phase. 

Each transaction is define in two parts at least:

1. the CHECK phase
    
    the class checks if there are the conditions to perform that operation. 

2. the EXECUTION phase
    
    the class performs operations and logging, according to the results obtained in the CHECK phase

Since transactions share the same general structure, a base class is provided
to put togethere all the common functionalities. 

### Transaction Code Structure

under the `api_transactions` folder, all the file names follow this simple convention:

- `transaction_<resource>_<operation>` to say that the transaction offers a functionality invoked by `/api/<topic>/<operation>`

Each file contains exactly one class, and other data, transaction queries included. As you can see, each file is divided into 2 parts at least:

1. queries
   
    queries are designed inside the database package, documented in standard format "as execise", and then implemented with exactly that logic. 

2. the transaction class implementation

For the query section, each query variable has a naming convention:

- `api_<file name>_sql_exec_<characteristic>`

To implement the class, you can use a pattern similar to this one:

```py

import psycopg2
from fastapi import status
from main_app.environment import environment
from main_app.api_logging.logging import log
from main_app.interfaces import db_interface
from .transaction_base import api_transaction_base
import json

api_<...>_sql_check= """

"""




api_<...>_sql_exec_<...> = """

"""





class api_transaction_<topic>_<operation>(api_transaction_base):
    ''' A app transaction. 

    Description of the transaction. 
    '''

    def __init__(self, env: environment, request) -> None:
        ''' Creae the transaction
        
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
        self.__log_detail_ds = ""
        self.__log_error:bool = False
        self.__log_unsecure_request = False

        # transaction custom data
        pass
    

    def check( self ): # inherited
        ''' transaction check phase
        
        '''
        global api_<...>_sql_check

        cur = self.db.get_cursor()
        cur.execute( 
            api_<...>_sql_check,
            {
                '...' : self.request.<...>,
            }
        )
        self.__res = cur.fetchall()
        self.__res_schema = [ str(col.name).upper() for col in cur.description ]
        self.__res_count = cur.rowcount

        if self.__res_count == 0:
            return self.__build_response(
                res_status=status.HTTP_404_NOT_FOUND,
                res_status_description="...",
                log_detail='...'
            )
        
        # ... checkings ... 

        return self.__build_response(
            res_status=status.HTTP_200_OK,
            res_status_description="success",
            log_detail=''
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
        global api_<...>_sql_exec_<...>
        
        cur = self.db.get_cursor()
        cur.execute("BEGIN TRANSACTION;")

        # ... transaction : success ...

        cur.execute("COMMIT TRANSACTION;")
    

    def __exec_fail( self ):
        global api_<...>_sql_exec_<...>
        
        cur = self.db.get_cursor()
        cur.execute("BEGIN TRANSACTION;")

        # ... transaction : failure ...

        cur.execute("COMMIT TRANSACTION;")


    def __build_response( self, 
        res_status:int, 
        res_status_description:str, 
        log_detail:str, 
        unsecure_request:bool=False 
    ) -> <...response...>:
        self.__log_detail_ds = log_detail
        self.response = <...response...>(
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


```

Here's a general approach you can use to implement the code of a transaction:

1. define and parametrize queries with the syntax required by the connector
2. review parameters inside `__init__()`
3. associate reqest types to the function parameters

    please remember that the mmodule `main_app.api_models` contains all the Pydantic models you need to make the API work

4. implement the `check()` function, calling `__build_response()` each time there are sufficient elements to formulate a response (mostly, a failure in this function, except that the end)
5. remember that calling the `__build_response()` sets the check phase as done
6. implement the function `__exec_success()`
7. implement the function `__exec_fail()`
8. everythin's almost done: write the API callback

### Importing and using a transacion

inside the `main.py` file, you can import a transaction by importing its main class, for instance:

```py
from api_transactions.transaction_user_login import api_transaction_user_login
```

Since the 90% of the functionality is enclosed inside the class, you have just to

1. build the class passing the environment to it
2. call the `check()` phase
3. call the `execute()` method
4. and, in the end, close the API call

### Transaction Implementation Guidelines

- **DON'T REUSE TRANSACTION CLASSES!** There's the risk to do a operation with a dirty environment, which is dangerous: it lets to unpredictable situations and bugs very difficult to understand. 
- *check phase as single query.* To not overload the DBMS, it is suggested to try to implement all the checkings in only one query instead of using one query for each check. Of course, you can divide, but first try to create one query
- I strongly suggest to make explicit type checking on the function parameters, since it is more easy to write code with autocompletion... by the way

---