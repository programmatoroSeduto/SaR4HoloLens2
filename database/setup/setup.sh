#!/bin/bash

echo "===== The SAR Project : INIT SCRIPT ====="
echo "===== BEGIN"

args="-U ${POSTGRES_USER} -d ${POSTGRES_DB} -W ${POSTGRES_PASSWORD}"
null_output="> /dev/null"

echo "setting up functions and libraries ..."
psql ${args} -f ${APP_LIB_SETUP_FILE} 
echo "setting up functions and libraries ... OK"

echo "creating datamodel ..."
psql ${args} -f ${APP_SETUP_FILE}
echo "creating datamodel ... OK"

if [[ "${APP_TESTING_MODE}" == "true" ]] ; then
    echo "TESTING MODE IS ON"
    echo "creating samples ..."
    for APP_SAMPLES_FILE in ${APP_SAMPLES_PATH}/*; do 
        echo "running $APP_SAMPLES_FILE ... "
        psql ${args} -f ${APP_SAMPLES_FILE}
        echo "running $APP_SAMPLES_FILE ... returned $?"
    done
    echo "creating samples ... OK"
else
    echo "TESTING MODE IS OFF"
fi

if [[ "${APP_USE_EXPERIMENTAL_SCRIPT}" == "true" ]] ; then
    echo "loading experimental script ... "
    psql ${args} -f ${APP_SETUP_PATH}/setup_experimental.sql
    echo "loading experimental script ... OK"
fi

echo "===== END"