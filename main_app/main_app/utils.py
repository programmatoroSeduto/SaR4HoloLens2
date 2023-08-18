
from datetime import datetime
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