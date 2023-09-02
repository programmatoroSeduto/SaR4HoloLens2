
import sys
import os
from fastapi import status
import requests
import json



base_url = "http://127.0.0.1:5000/api"



def dict2json(data:dict) -> str:
    data_serializable = dict()
    for k in data.keys():
        data_serializable[k] = str(data[k])
    return json.dumps(data_serializable, indent=4)



def api_post_request(url:str, payload:dict = dict(), print_request:bool = True, print_response:bool = True) -> (bool, int, str, str):
    '''
    RETURNS:
    -    success?
    -    status code
    -    text response (can be NULL if request is not valid)
    -    JSON response (NULL if not parsable)
    '''

    success = False
    status_code = 0
    text_response = None
    json_response = None
    if url == "":
        print(f"api_post_request ERROR: empty URL")
        return (success, status_code, text_response, json_response)
    
    if print_request:
        print(f"POST REQUEST to: {url}\nWITH PAYLOAD:\n" + dict2json(payload))
    
    res = requests.post(
        url,
        json=payload
    )

    success = (
        res.status_code in ( status.HTTP_200_OK, status.HTTP_202_ACCEPTED )
    )
    status_code = res.status_code
    text_response = res.text
    try:
        json_response = res.json()
    except Exception as e:
        print(f"api_post_request WARNING: {e}")
        json_response = None
    
    if print_response and json_response is not None:
        print(f"POST RESPONSE (json) from: {url}\nWITH CONTENT:\n" + dict2json(json_response))
    elif print_response:
        print(f"POST RESPONSE (text) from: {url}\nWITH CONTENT:\n" + text_response)

    return (success, status_code, text_response, json_response)



def api_release(user_id:str, session_token:str) -> bool:
    '''
    RETURNS:
    -    success or not
    '''
    if user_id == "" or session_token == "":
        print(f"api_release ERROR: empty data")
        return False

    success, _, _, _ = api_post_request(
        f"{base_url}/user/logout",
        {
            'user_id' : user_id,
            'session_token' : session_token
        }
    )

    return success



def api_access(user_id:str, approver_id:str, access_key:str, device_id:str, logout_if_fail:bool = True):
    '''
    RETURNS:
    -   success, 
    -   session token
    '''
    global base_url
    session_token = None
    success = False

    if user_id == "" or approver_id == "" or access_key == "" or device_id == "":
        print(f"api_access ERROR: empty data")
        return (success, session_token) # false, None
    
    # login
    login_success, login_status_code, text_response, json_response = api_post_request(
        f"{base_url}/user/login",
        {
            'user_id' : user_id,
            'approver_id' : approver_id,
            'access_key' : access_key,
            'device_id' : device_id
        }
    )
    if not login_success:
        print(f"api_access ERROR: cannot login with status code {login_status_code}")
        if json_response is not None:
            print(f"api_access INFO: from server JSON:\n{json.dumps(json_response, indent=4)}")
        elif text_response is not None:
            print(f"api_access INFO: from server TEXT:\n{text_response}")
        
        return (success, session_token) # false, None
    else:
        session_token = json_response['session_token']

    # access device
    device_success, device_status_code, text_response, json_response = api_post_request(
        f"{base_url}/device/login",
        {
            'user_id' : user_id,
            'device_id' : device_id,
            'session_token' : session_token
        }
    )
    if not device_success:
        print(f"api_access ERROR: cannot access device, with status code {device_status_code}")
        if json_response is not None:
            print(f"api_access INFO: from server JSON:\n{json.dumps(json_response, indent=4)}")
        elif text_response is not None:
            print(f"api_access INFO: from server TEXT:\n{text_response}")
        
        if logout_if_fail:
            api_release(user_id, session_token)
        
        return (success, session_token) # false, None

    success = True
    return (success, session_token)



def vector3(x, y, z):
    return [ float(x), float(y), float(z) ]



def api_download(user_id:str, device_id:str, ref_id:str, session_token:str, arg_based_on:str, center:list, radius:float):
    '''
    RETURNS
    -   success?
    -   based_on (in case, equals to the one passed as argument of the function, can be None)
    -   max_idx (can be None)
    -   waypoints (never None, at most empty)
    -   paths (never None, at most empty)
    '''
    global base_url
    success:bool = False
    based_on:str = None
    max_idx:int = None
    waypoints:dict = list()
    paths:dict = list()

    if user_id=="" or device_id=="" or ref_id=="" or session_token=="":
        print(f"api_download ERROR: empty data\n\t{(user_id, device_id, ref_id, session_token, based_on, center, radius)}")
        return (success, based_on, max_idx, waypoints, paths)
    
    success, status_code, text_response, json_response = api_post_request(
        f"{base_url}/hl2/download",
        {
            'user_id' : user_id,
            'device_id' : device_id,
            'session_token' : session_token,
            'based_on' : arg_based_on or "",
            'ref_id' : ref_id,
            'center' : center,
            'radius' : radius
        }
    )
    if not success:
        print(f"api_access ERROR: cannot download, with status code {status_code}")
        if json_response is not None:
            print(f"api_access INFO: from server JSON:\n{json.dumps(json_response, indent=4)}")
        elif text_response is not None:
            print(f"api_access INFO: from server TEXT:\n{text_response}")
        
        return (success, based_on, max_idx, waypoints, paths)
    
    based_on = ( json_response['based_on'] if json_response['based_on'] != "" else based_on )
    max_idx = json_response['max_idx']
    waypoints = json_response['waypoints']
    paths = json_response['paths']

    return (success, based_on, max_idx, waypoints, paths)



def api_upload(user_id:str, device_id:str, ref_id:str, session_token:str, based_on:str, waypoints:list, paths:list):
    '''
    RETURNS:
    -   success?
    -   max_id (can be None)
    -   wp_alignment (never None, empty at most)
    '''
    global base_url

    success:bool = False
    max_id:int = None
    wp_alignment:list = []

    if user_id=="" or device_id=="" or ref_id=="" or session_token=="":
        print(f"api_download ERROR: empty data\n\t{(user_id, device_id, ref_id, session_token, based_on, waypoints, paths)}")
        return (success, max_id, wp_alignment)
    
    success, status_code, text_response, json_response = api_post_request(
        f"{base_url}/hl2/upload",
        {
            'user_id' : user_id,
            'device_id' : device_id,
            'session_token' : session_token,
            'based_on' : based_on,
            'ref_id' : ref_id,
            'waypoints' : waypoints,
            'paths' : paths
        }
    )
    if not success:
        print(f"api_access ERROR: cannot download, with status code {status_code}")
        if json_response is not None:
            print(f"api_access INFO: from server JSON:\n{json.dumps(json_response, indent=4)}")
        elif text_response is not None:
            print(f"api_access INFO: from server TEXT:\n{text_response}")
        
        return (success, max_id, wp_alignment)

    max_id = json_response['max_id']
    wp_alignment = json_response['wp_alignment']

    return (success, max_id, wp_alignment)
