
import sys
import os
from datetime import datetime
from fastapi import FastAPI
from fastapi import (
    status,
    Body,
)
from pydantic import (
    BaseModel,
    Field,
)
from typing import (
    Annotated
)
from config import config as config_handle
from config import db_access_data
import metadata
from environment import environment as environment_handle
import exceptions
import interfaces
import utils
import api_models
from api_logging.logging import log as log_handle
from api_logging.setup_log import setup_log
from api_logging.log_layer import log_layer
from api_logging.log_type import log_type
from api_transactions.transaction_base import api_transaction_base



config = config_handle()
log:log_handle = None
log_err_details = ""
log, log_err_details = setup_log(
    debug = False,
    layer = config.log_layer_default,
    log_file_path = config.log_file_path,
    log_file_name = config.log_file_name
)
if log is None:
    print(f"[{datetime.now()}, CONFIG] CRITICAL: error during the creation of the logger! \n\t{log_err_details}")
    sys.exit(2)

log.info("trying to connect to the database ... ", src="main")
db = interfaces.db_interface()
try:
    log.debug(f"Connecting to: \n\tDB Address: {db_access_data['host']}:{db_access_data['port']}\n\tDB name: {db_access_data['dbname']}", src="main")
    db.connect( db_access_data )
    log.info("trying to connect to the database ... OK", src="main")
except ConnectionError as ce:
    log.err(f"connection error\nReason:\n\t{ce.description}")
    sys.exit(1)

log.info("creating FastAPI instance app ...", src="main")
api = FastAPI(
    debug=False, 
    description="main backend interface for SAR project"
)
log.info("creating FastAPI instance app ... OK", src="main")

log.info("creating environment ...", src="main")
env = environment_handle(
    db = db,
    api = api,
    log = log
)
log.info("creating environment ... OK", src="main")



@api.get(
    "/",
    tags = [ metadata.api_tags.root ],
    response_model=api_models.api_base_response,
    status_code = status.HTTP_400_BAD_REQUEST
)
async def root(
    request_body: Annotated[api_models.api_base_request, Body()] = api_models.api_base_request()
) -> api_models.api_base_response:
    ''' root request
    
    This is not accessible: the serer will return error if the client tries 
    to call this path. 
    '''
    global config, env
    log.info_api( "/", src=metadata.api_tags.root )

    return api_models.api_base_response(
        timestamp_received=request_body.timestamp,
        status=status.HTTP_400_BAD_REQUEST, 
        status_detail=""
        )



@api.get(
    "/api",
    tags = [ metadata.api_tags.api_root ],
    response_model=api_models.api_base_response,
    status_code = status.HTTP_418_IM_A_TEAPOT
)
async def api_root(
    request_body: Annotated[api_models.api_base_request, Body()] = api_models.api_base_request()
) -> api_models.api_base_response:
    ''' API Status
    
    No coffee for ya I'm a goddam teapot. 
    '''
    global config, env
    log.info_api( "/api", src=metadata.api_tags.api_root )

    return api_models.api_base_response(
        timestamp_received=request_body.timestamp,
        status=status.HTTP_418_IM_A_TEAPOT, 
        status_detail=""
        )



from api_transactions.transaction_user_login import api_transaction_user_login
@api.post(
    "/api/user/login",
    tags=[ metadata.api_tags.api_user_login ],
    response_model=api_models.api_user_login_response,
)
async def api_user_login(
    request_body: Annotated[api_models.api_user_login_request, Body()]
) -> api_models.api_user_login_response:
    ''' User Login
    
    the first step to access the server: the user must log in. 
    '''
    global config, env
    log.info_api( "/api/user/login", src=metadata.api_tags.api_user_login )

    tr = api_transaction_user_login(env, request_body)
    tr.check()
    tr.execute()

    return utils.set_response_timestamp(
        request_body,
        tr.response
    )



from api_transactions.transaction_user_logout import api_transaction_user_logout
@api.post(
    "/api/user/logout",
    tags=[ metadata.api_tags.api_user_logout ],
    response_model=api_models.api_user_logout_response,
)
async def api_user_login(
    request_body: Annotated[api_models.api_user_logout_request, Body()]
) -> api_models.api_user_logout_response:
    ''' User Logout
    
    This request is used for releasing user
    '''
    global config, env
    log.info_api( "/api/user/logout", src=metadata.api_tags.api_user_logout )

    tr = api_transaction_user_logout(env, request_body)
    tr.check()
    tr.execute()

    return utils.set_response_timestamp(
        request_body,
        tr.response
    )



from api_transactions.transaction_device_login import api_transaction_device_login
@api.post(
    "/api/device/login",
    tags=[ metadata.api_tags.api_device_login ],
    response_model=api_models.api_device_login_response,
)
async def api_device_login(
    request_body: Annotated[api_models.api_device_login_request, Body()]
) -> api_models.api_device_login_response:
    ''' Device Login
    
    This API call allows a user to acquire a holdable device
    '''
    global config, env
    log.info_api( "/api/device/login", src=metadata.api_tags.api_device_login )

    tr = api_transaction_device_login(env, request_body)
    tr.check()
    tr.execute()

    return utils.set_response_timestamp(
        request_body,
        tr.response
    )



from api_transactions.transaction_device_logout import api_transaction_device_logout
@api.post(
    "/api/device/logout",
    tags=[ metadata.api_tags.api_device_logout ],
    response_model=api_models.api_device_logout_response,
)
async def api_device_login(
    request_body: Annotated[api_models.api_device_logout_request, Body()]
) -> api_models.api_device_logout_response:
    ''' Device Logout
    
    This API call allows a user to release a holding device. 
    '''
    global config, env
    log.info_api( "/api/device/logout", src=metadata.api_tags.api_device_logout )

    tr = api_transaction_device_logout(env, request_body)
    tr.check()
    tr.execute()

    return utils.set_response_timestamp(
        request_body,
        tr.response
    )






log.info("Application is running now...", src="main")