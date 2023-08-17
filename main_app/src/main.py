
from fastapi import FastAPI
from fastapi import (
    status
)

app = FastAPI(
    debug=False, 
    description="main backend interface for SAR project"
)

@app.get("/", status_code=status.HTTP_200_OK)
async def api_root():
    return { "success" : "true" }

