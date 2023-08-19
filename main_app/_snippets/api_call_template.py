
@api.get(
    "/",
    tags = [ metadata.api_tags.??? ],
    response_model=api_models.???,
    status_code = status.HTTP_200_OK
)
async def api_root(
    ...
) -> api_models.???:
    global config, env
    log.info_api( "/", src="api_root" )

    return api_models.api_base_response(
        timestamp_received=request_body.timestamp,
        status=status.HTTP_200_OK, 
        status_detail=""
        )