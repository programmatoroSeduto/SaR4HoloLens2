
from utils import config_from_env

db_access_data = {
    "host" : "database",
    "port" : "5432",
    "dbname" : "postgres",
    "user" : "postgres",
    "password" : "postgres"
}

class config:

    def __init__(self):
        global project_name
        self.debug_mode = ( config_from_env('APP_DEBUG_MODE', "true") == "true" )
        self.log_layer_default = int(config_from_env( 'APP_LOG_LAYER', '0' ))
        self.log_file_path = config_from_env('APP_LOG_PATH', "/app/logs/main_app")
        self.log_file_name = config_from_env('APP_LOG_NAME', "main_app")