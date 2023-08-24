
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
        global db_access_data
        self.debug_mode = ( config_from_env('APP_DEBUG_MODE', "true") == "true" )
        self.log_layer_default = int(config_from_env( 'APP_LOG_LAYER', '0' ))
        self.log_file_path = config_from_env('APP_LOG_PATH', "/app/logs/main_app")
        self.log_file_name = config_from_env('APP_LOG_NAME', "main_app")
        self.start_delay = int(config_from_env('APP_STARTUP_DELAY', "10"))
        
        db_access_data['host'] = config_from_env('DB_HOST', "UNKNOWN")
        db_access_data['port'] = config_from_env('DB_PORTNO', "UNKNOWN")
        db_access_data['dbname'] = config_from_env('DB_NAME', "UNKNOWN")
        db_access_data['user'] = config_from_env('DB_USER', "UNKNOWN")
        db_access_data['password'] = config_from_env('DB_PASSWORD', "UNKNOWN")