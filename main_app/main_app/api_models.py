
from pydantic import (
    BaseModel,
    Field,
)
from datetime import datetime
from typing import (
    Union,
)



## ===== SUPPORT MODELS ===== ##

class table_base(BaseModel):
    status: bool = Field(
        default = True,
        description = "either the query succeeded or not"
    )
    status_detail: str = Field(
        default="",
        description="details about the status"
    )
    query: str = Field(
        default="",
        description="the query used to extract results"
    )
    table_schema: list[str] = Field(
        default=[],
        description="the schema of the able, aligned with the database results"
    )
    table_size: int = Field(
        default = 0,
        description="how many rows inside the table"
    )
    table_values:list[tuple] = Field(
        default=[],
        description="the values from the table"
    )



## ===== BASE REQUEST RESPONSE ===== ##

class api_base_request(BaseModel):
    timestamp: datetime = Field(
        default=datetime.now(),
        description="the time the request arrived to the server",
        )

class api_base_response(BaseModel):
    timestamp_received: Union[datetime, None] = Field(
        default=None,
        description="When the message has been received if known"
    )
    timestamp_sent: datetime = Field(
        default=datetime.now(),
        description="the time the server sent the response",
    )
    status: int = Field(
        description="the number representing the status of the request",
    )
    status_detail: str = Field(
        default="", 
        title="output message referred to the status code",
    )



## ===== USER LOGIN ===== ##

class api_user_login_request(api_base_request):
    user_id:str = Field(
        description="the user that is requiring to access the service",
        pattern="SARHL2_ID[0-9]{10}_USER"
    )
    approver_id:str = Field(
        description="the user which should approve the request (euqlas to user_id if admin access mode)",
        pattern="SARHL2_ID[0-9]{10}_USER"
    )
    access_key:str = Field(
        description="the access key used for accessing the resource"
    )

class api_user_login_response(api_base_response):
    session_token:str = Field(
        default="",
        description="It is a has code generated directly from the server. It must be attached to any following requests from the user."
    )