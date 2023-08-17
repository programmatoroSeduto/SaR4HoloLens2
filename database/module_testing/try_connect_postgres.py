
import psycopg2
import sys

sql_test = "SELECT 1 AS uno;"

db_connection_args = {
    "host" : "127.0.0.1",
    "port" : "5432",
    "dbname" : "postgres",
    "user" : "postgres",
    "password" : "postgres"
}
conn = None
cur = None
try:
    conn = psycopg2.connect( **db_connection_args )
    cur = conn.cursor()
    print("connection OK")

    print(  )
    cur.execute( sql_test )
    print("Run SQL OK with response", cur.fetchone())

    cur.close()
    conn.close()
    print("Connection closed OK")

except psycopg2.OperationalError:
    print("ERROR: can't connect to the Postgres database!")
    sys.exit(1)
except Exception as e:
    print(e)
    sys.exit(2)