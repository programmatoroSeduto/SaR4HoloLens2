
from fastapi import FastAPI
from fastapi import (
    status
)

app = FastAPI(
    debug=False, 
    description="main backend interface for SAR project"
)

@app.get("/test")
async def test():
    return { "test" : "true" }

@app.get("/test/{something}")
async def test_something(something: int):
    return { "test" : "true" }