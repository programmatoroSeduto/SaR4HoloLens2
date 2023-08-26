
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



## ===== BASE DATA CLASSES ===== ##

class data_base_pack(BaseModel):
    pass



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



## ===== HL2 GENERIC ===== ##

class api_hl2_base_request(api_base_request):
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

class api_hl2_base_response(api_base_response):
    pass

class data_hl2_waypoint(data_base_pack):
    pos_id:int = Field(
        description="the local ID of the waypoint, assigned by the database"
    )
    area_id:int = Field(
        description="local area center, i.e. wrt the local zones division"
    )
    v:tuple[float, float, float] = Field(
        description="Area Center of the waypoint wrt the reference position"
    )
    wp_timestamp:Union[str, datetime] = Field(
        description="the date/time the waypoint has been recorded"
    )

class data_hl2_path(data_base_pack):
    wp1:int = Field(
        ge = 0
    )
    wp2:int = Field(
        ge = 0
    )
    dist:float = Field(
        ge=0.0
    )
    pt_timestamp:Union[str, datetime] = Field(
        description="the date/time the link has been recorded"
    )

class data_hl2_align_item(data_base_pack):
    request_position_id:int
    aligned_position_id:int



## ===== HL2 DOWNLOAD ===== ##

class api_hl2_download_request(api_hl2_base_request):
    based_on:str = ''
    ref_id:str = Field(
        pattern = refpos_id_pattern
    )
    center:tuple[float, float, float]
    radius:float = Field(
        default = 500.00 # meters
    )

class api_hl2_download_response(api_hl2_base_response):
    ref_id:str = Field(
        default = '',
        description="Identifier of the reference point used for calibration by HL2"
    )
    based_on:str = ''
    max_idx:int = -1
    waypoints:list[data_hl2_waypoint] = Field(
        default=list(),
        description="set of waypoints measured by the HoloLens2 system"
    )
    paths:list[data_hl2_path] = Field(
        default=list(),
        description="list of links locally measured between waypoints"
    )


## ===== HL2 UPLOAD ===== ##

class api_hl2_upload_request(api_hl2_base_request):
    based_on:str
    ref_id:str = Field(
        description="Identifier of the reference point used for calibration by HL2",
        pattern=refpos_id_pattern
    )
    waypoints:list[data_hl2_waypoint] = Field(
        description="set of waypoints measured by the HoloLens2 system"
    )
    paths:list[data_hl2_path] = Field(
        description="list of links locally measured between waypoints"
    )

class api_hl2_upload_response(api_hl2_base_response):
    max_id:int = -1
    wp_alignment:list[data_hl2_align_item] = Field(
        default = list()
    )



## ===== HL2 SETTINGS FROM SERVER ===== ##

class api_hl2_settings_request(api_hl2_base_request):
    config_profile_id:int = -1

class api_hl2_settings_response(api_hl2_base_response):
    found_profile:bool = True
    config_profile_id:int = 0
    user_height:float = 1.85
    base_height:float = 0.8
    base_distance:float = 0.5
    distance_tollerance:float = 0.1
    use_cluster:bool = True
    cluster_size:int = 25
    use_max_indices:bool = True
    max_indices:int = 10
    log_layer:int = 1
    ref_id:str = ''