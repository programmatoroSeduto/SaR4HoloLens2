
from enum import Enum
import os
from datetime import datetime
from fastapi import FastAPI as fastapi_handle
from interfaces import db_interface
from api_logging.logging import log as log_handle
import utils

class environment:
    ''' Environment shared reference
    
    This class contains a series of references to be shared to each part
    of the API application. For instance, it contains the database reference, 
    or the reference to the logging system. 
    '''
    
    def __init__(self, api:fastapi_handle, db:db_interface, log:log_handle):
        ''' shared class constructor
        
        just put the references inside. This class is meant to be shared across the 
        API application. 
        '''
        self.db = db
        self.log = log
        self.api = api