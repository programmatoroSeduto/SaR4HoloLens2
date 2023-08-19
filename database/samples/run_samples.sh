#!/bin/bash

psql -U ${POSTGRES_USER} -d ${POSTGRES_DB} -W ${POSTGRES_PASSWORD} -a -f /app/samples/sample_${DB_SAMPLES_FILE}.sql