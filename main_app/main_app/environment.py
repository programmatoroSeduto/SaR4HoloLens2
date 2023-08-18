
from enum import Enum
import os
from datetime import datetime
from fastapi import FastAPI as fastapi_handle
from interfaces import db_interface
from api_logging.logging import log as log_handle
import utils

class environment:
    
    def __init__(self, api:fastapi_handle, db:db_interface, log:log_handle):
        self.db = db
        self.log = log
        self.api = api