
from pydantic import (
    BaseModel,
    Field,
)
from datetime import datetime
from typing import (
    Union,
)

user_id_pattern = "SARHL2_ID[0-9]{10}_USER"
device_id_pattern = "SARHL2_ID[0-9]{10}_DEVC"
refpos_id_pattern = "SARHL2_ID[0-9]{10}_REFP"



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
        pattern=user_id_pattern
    )
    approver_id:str = Field(
        description="the user which should approve the request (euqlas to user_id if admin access mode)",
        pattern=user_id_pattern
    )
    access_key:str = Field(
        description="the access key used for accessing the resource",
        pattern=".+"
    )

class api_user_login_response(api_base_response):
    session_token:str = Field(
        default="",
        description="It is a has code generated directly from the server. It must be attached to any following requests from the user."
    )



## ===== USER LOGOUT ===== ##

class api_user_logout_request(api_base_request):
    user_id:str = Field(
        description="the user that is requiring to access the service",
        pattern=user_id_pattern
    )
    session_token:str = Field(
        description="the session token identifying the user session",
        pattern=".+"
    )

class api_user_logout_response(api_base_response):
    logged_out_devices:list[str] = Field(
        default=list(),
        description="When a user logs out, the server also releases all the devices associated with it"
    )



## ===== DEVICE LOGIN ===== ##

class api_device_login_request(api_base_request):
    user_id:str = Field(
        description="the user that is requiring to access the service",
        pattern=user_id_pattern
    )
    device_id:str = Field(
        description="the device the user is trying to acquire",
        pattern=device_id_pattern
    )
    session_token:str = Field(
        description="the session token identifying the user session",
        pattern=".+"
    )

class api_device_login_response(api_base_response):
    pass



## ===== DEVICE LOGOUT ===== ##

class api_device_logout_request(api_base_request):
    user_id:str = Field(
        description="the user that is requiring to access the service",
        pattern=user_id_pattern
    )
    device_id:str = Field(
        description="the device the user is trying to release",
        pattern=device_id_pattern
    )
    session_token:str = Field(
        description="the session token identifying the user session",
        pattern=".+"
    )

class api_device_logout_response(api_base_response):
    pass