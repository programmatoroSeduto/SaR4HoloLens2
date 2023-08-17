
from enum import Enum

db_access_data = db_connection_args = {
    "host" : "database",
    "port" : "5432",
    "dbname" : "postgres",
    "user" : "postgres",
    "password" : "postgres"
}

class api_tags(Enum):
    root = "root"