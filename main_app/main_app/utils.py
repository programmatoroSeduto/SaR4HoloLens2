
from datetime import datetime
from main_app.api_models import api_base_request, api_base_response
import os

def config_from_env(varenv:str = "", default_val:str = None) -> any:
    if varenv == "":
        print(f"[{datetime.now()}, CONFIG, -1] WARNING: option 'varenv' cannot be empty")
        return None
    
    varenv_val = default_val
    try:
        varenv_val = os.environ[varenv]
    except KeyError as ke:
        print(f"[{datetime.now()}, CONFIG, -1] WARNING: configuration issue; can't find environment variable {varenv}")
    
    return varenv_val

def set_response_timestamp(req:api_base_request, res:api_base_response):
    res.timestamp_received = req.timestamp
    res.timestamp_sent = datetime.now()
    return res