
import sys
import os
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
import config
import exceptions
import interfaces
import utils
import api_models

print("===== SAR4HL2 SERVER =====")

print("trying to conenct to the database ...", end = " ")
db = interfaces.db_interface()
try:
    db.connect( config.db_access_data )
    print("OK")
except ConnectionError as ce:
    print(f"ERROR: connection error\nReason:\n\t{ce.description}")
    sys.exit(1)

print("creating FastAPI instance app ...", end = " ")
api = FastAPI(
    debug=False, 
    description="main backend interface for SAR project"
)
print("OK")

@api.get(
    "/",
    tags = [ config.api_tags.root ],
    response_model=api_models.api_base_response,
    status_code = status.HTTP_200_OK
)
async def api_root(
    request_body: Annotated[api_models.api_base_request, Body()] = api_models.api_base_request()
) -> api_models.api_base_response:
    global db
    print("CALL: api_root")
    print(dict(db.sql( "SELECT * FROM information_schema.columns" )))
    return api_models.api_base_response(
        timestamp_received=request_body.timestamp,
        status=status.HTTP_200_OK, 
        status_detail="service is online"
        )

print("Application is running now...")