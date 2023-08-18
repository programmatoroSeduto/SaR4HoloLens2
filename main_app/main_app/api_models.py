
from pydantic import (
    BaseModel,
    Field,
)
from datetime import datetime
from typing import (
    Union,
)

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