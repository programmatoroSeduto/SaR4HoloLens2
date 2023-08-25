
import requests
import json
from fastapi import status
import sys
import random
import time
from datetime import datetime
import math

random.seed(time.time())
newpoint = [
    random.uniform(-1, 5),
    0,
    random.uniform(-1, 5)
]

# main_app_address = "http://127.0.0.1:5000/api"
main_app_address = "http://127.0.0.1/sar/api"
user_session_token = {
    'SARHL2_ID8849249249_USER' : '',
    'SARHL2_ID4243264423_USER' : ''
}
err = False
fake_session_token = ''
print(f"MAIN APP TESTING -- address: '{main_app_address}'")




## ===== SERVER STATUS ===== ## 

print("sending status request ...")
res = None
try:
    res = requests.get(
        url=main_app_address
    )   
except ConnectionRefusedError as cre:
    print("CONNECTION ERROR --", cre, "\nclosing")
    sys.exit(1)
except requests.exceptions.ConnectionError as reqcre:
    print("CONNECTION ERROR --", reqcre, "\nclosing")
    sys.exit(1)

if res.status_code == status.HTTP_418_IM_A_TEAPOT:
    print("Server OK")
else:
    err = True
    print("ERROR: service not available!")
    print("returned code:", res.status_code)
    print("response:")
    try:
        print(res.json())
    except:
        print(res.text)

if err:
    print("ERROR: closing")
    sys.exit(1)
print("sending status request ... END")




## ===== USER LOGIN FOR ADMIN USER ===== ## 

print("user login ADMIN ... ")
payload = {
    'user_id' : 'SARHL2_ID8849249249_USER',
    'approver_id' : 'SARHL2_ID8849249249_USER',
    'access_key' : 'anoth3rBr3akabl3P0sswArd'
}
res = None
try:
    res = requests.post(
        url=f"{main_app_address}/user/login",
        data=json.dumps(payload)
    )   
except ConnectionRefusedError as cre:
    print("CONNECTION ERROR --", cre, "\nclosing")
    sys.exit(1)
except requests.exceptions.ConnectionError as reqcre:
    print("CONNECTION ERROR --", reqcre, "\nclosing")
    sys.exit(1)

if res.status_code == status.HTTP_200_OK:
    print("Server OK")
else:
    err = True
    print("ERROR: can't login")
    print("returned code:", res.status_code)
print("response:")
res_jsonobj = None
res_raw = None
try:
    print("(JSON response)", res.json())
    res_jsonobj = res.json()
    user_session_token['SARHL2_ID8849249249_USER'] = res_jsonobj['session_token']
except:
    print("(RAW response)", res.text)
    res_raw = res.text

if err:
    print("ERROR: closing")
    sys.exit(1)
print("user login ADMIN ... END")




## ===== USER LOGIN FOR NON ADMIN USER ===== ## 

print("user login NO ADMIN ... ")
payload = {
    'user_id' : 'SARHL2_ID4243264423_USER',
    'approver_id' : 'SARHL2_ID8849249249_USER',
    'access_key' : 'casseruola96'
}
res = None
try:
    res = requests.post(
        url=f"{main_app_address}/user/login",
        data=json.dumps(payload)
    )   
except ConnectionRefusedError as cre:
    print("CONNECTION ERROR --", cre, "\nclosing")
    sys.exit(1)
except requests.exceptions.ConnectionError as reqcre:
    print("CONNECTION ERROR --", reqcre, "\nclosing")
    sys.exit(1)

if res.status_code == status.HTTP_200_OK:
    print("Server OK")
else:
    err = True
    print("ERROR: can't login")
    print("returned code:", res.status_code)
print("response:")
res_jsonobj = None
res_raw = None
try:
    print("(JSON response)", res.json())
    res_jsonobj = res.json()
    user_session_token['SARHL2_ID4243264423_USER'] = res_jsonobj['session_token']
except:
    print("(RAW response)", res.text)
    res_raw = res.text

if err:
    print("ERROR: closing")
    sys.exit(1)
print("user login NO ADMIN ... END")




## ===== ACQUIRE DEVICE ===== ## 

print("ACQUIRE DEVICE HOLOLENS2 ... ")
payload = {
    'user_id' : 'SARHL2_ID4243264423_USER',
    'device_id' : 'SARHL2_ID8651165355_DEVC',
    'session_token' : user_session_token['SARHL2_ID4243264423_USER']
}
res = None
try:
    res = requests.post(
        url=f"{main_app_address}/device/login",
        data=json.dumps(payload)
    )   
except ConnectionRefusedError as cre:
    print("CONNECTION ERROR --", cre, "\nclosing")
    sys.exit(1)
except requests.exceptions.ConnectionError as reqcre:
    print("CONNECTION ERROR --", reqcre, "\nclosing")
    sys.exit(1)

if res.status_code == status.HTTP_200_OK:
    print("Server OK")
else:
    err = True
    print("ERROR: can't acquire device ")
    print("returned code:", res.status_code)
print("response:")
res_jsonobj = None
res_raw = None
try:
    print("(JSON response)", res.json())
except:
    print("(RAW response)", res.text)
    res_raw = res.text

if err:
    print("ERROR: closing")
    sys.exit(1)
print("ACQUIRE DEVICE HOLOLENS2 ... END")




## ===== DOWNLOAD REQUEST ===== ## 

print("hl2 trying to download positions from server (expected: empty) ... ")
payload = {
    'user_id' : 'SARHL2_ID4243264423_USER',
    'device_id' : 'SARHL2_ID8651165355_DEVC',
    'session_token' : user_session_token['SARHL2_ID4243264423_USER'],
    'based_on' : '', # calibration
    'ref_id' : 'SARHL2_ID1234567890_REFP',
    'center' : [ 0, 0, 0 ],
    'radius' : 50
}
res = None
try:
    res = requests.post(
        url=f"{main_app_address}/hl2/download",
        data=json.dumps(payload)
    )   
except ConnectionRefusedError as cre:
    print("CONNECTION ERROR --", cre, "\nclosing")
    sys.exit(1)
except requests.exceptions.ConnectionError as reqcre:
    print("CONNECTION ERROR --", reqcre, "\nclosing")
    sys.exit(1)

if res.status_code == status.HTTP_200_OK:
    print("Server OK")
else:
    err = True
    print("ERROR: can't download ")
    print("returned code:", res.status_code)
print("response:")
res_jsonobj = None
res_raw = None
try:
    print("(JSON response)", res.json())
    res_jsonobj = res.json()
    fake_session_token = res_jsonobj['based_on']
except:
    print("(RAW response)", res.text)
    res_raw = res.text

if err:
    print("ERROR: closing")
    sys.exit(1)
print("hl2 trying to download positions from server (expected: empty) ... END")

input()




## ===== UPLOAD REQUEST ===== ## 

print("hl2 time to upload! ... ")
payload = {
    'user_id' : 'SARHL2_ID4243264423_USER',
    'device_id' : 'SARHL2_ID8651165355_DEVC',
    'session_token' : user_session_token['SARHL2_ID4243264423_USER'],
    'ref_id' : 'SARHL2_ID1234567890_REFP',
    'based_on' : fake_session_token,
    'waypoints' : [
        {
            'pos_id' : 1,
            'area_id' : 0,
            'v' : newpoint,
            'wp_timestamp' : datetime.now().strftime("%Y/%m/%d %H:%M:%S")
        }
    ],
    'paths' : [
        {
            'wp1' : 0,
            'wp2' : 1,
            'dist' : math.sqrt(newpoint[0]*newpoint[0] + newpoint[2]*newpoint[2]),
            'pt_timestamp' : datetime.now().strftime("%Y/%m/%d %H:%M:%S")
        }
    ]
}

res = None
print(json.dumps(payload, indent=4))
try:
    res = requests.post(
        url=f"{main_app_address}/hl2/upload",
        data=json.dumps(payload)
    )   
except ConnectionRefusedError as cre:
    print("CONNECTION ERROR --", cre, "\nclosing")
    sys.exit(1)
except requests.exceptions.ConnectionError as reqcre:
    print("CONNECTION ERROR --", reqcre, "\nclosing")
    sys.exit(1)

if res.status_code == status.HTTP_200_OK:
    print("Server OK")
else:
    err = True
    print("ERROR: can't upload! ")
    print("returned code:", res.status_code)
print("response:")
res_jsonobj = None
res_raw = None
try:
    print("(JSON response)", res.json())
    res_jsonobj = res.json()
except:
    print("(RAW response)", res.text)
    res_raw = res.text

print("hl2 time to upload! ... END")

input()




## ===== DOWNLOAD REQUEST ===== ## 

print("hl2 trying to download positions from server ... ")
payload = {
    'user_id' : 'SARHL2_ID4243264423_USER',
    'device_id' : 'SARHL2_ID8651165355_DEVC',
    'session_token' : user_session_token['SARHL2_ID4243264423_USER'],
    'based_on' : fake_session_token,
    'ref_id' : 'SARHL2_ID1234567890_REFP',
    'center' : [ 0, 0, 0 ],
    'radius' : 2
}
res = None
try:
    res = requests.post(
        url=f"{main_app_address}/hl2/download",
        data=json.dumps(payload)
    )   
except ConnectionRefusedError as cre:
    print("CONNECTION ERROR --", cre, "\nclosing")
    sys.exit(1)
except requests.exceptions.ConnectionError as reqcre:
    print("CONNECTION ERROR --", reqcre, "\nclosing")
    sys.exit(1)

if res.status_code == status.HTTP_200_OK:
    print("Server OK")
else:
    err = True
    print("ERROR: can't download ")
    print("returned code:", res.status_code)
print("response:")
res_jsonobj = None
res_raw = None
try:
    print("(JSON response)", res.json())
    res_jsonobj = res.json()
    fake_session_token = res_jsonobj['based_on']
except:
    print("(RAW response)", res.text)
    res_raw = res.text

if err:
    print("ERROR: closing")
    sys.exit(1)
print("hl2 trying to download positions from server  ... END")

input()



## ===== USER LOGOUT FOR NON ADMIN USER ===== ## 

print("user logout NO ADMIN ... ")
print("session token: ")
payload = {
    'user_id' : 'SARHL2_ID4243264423_USER',
    'session_token' : user_session_token['SARHL2_ID4243264423_USER']
}
res = None

try:
    res = requests.post(
        url=f"{main_app_address}/user/logout",
        data=json.dumps(payload)
    )   
except ConnectionRefusedError as cre:
    print("CONNECTION ERROR --", cre, "\nclosing")
    sys.exit(1)
except requests.exceptions.ConnectionError as reqcre:
    print("CONNECTION ERROR --", reqcre, "\nclosing")
    sys.exit(1)

if res.status_code == status.HTTP_200_OK:
    print("Server OK")
else:
    err = True
    print("ERROR: can't logout")
    print("returned code:", res.status_code)
print("response:")
res_jsonobj = None
res_raw = None
try:
    print("(JSON response)", res.json())
    res_jsonobj = res.json()
except:
    print("(RAW response)", res.text)
    res_raw = res.text

if err:
    print("ERROR: closing")
    sys.exit(1)
print("user logout NO ADMIN ... END ")




## ===== USER LOGOUT FOR ADMIN USER ===== ## 

print("user logout ADMIN ... ")
print("session token: ")
payload = {
    'user_id' : 'SARHL2_ID8849249249_USER',
    'session_token' : user_session_token['SARHL2_ID8849249249_USER']
}
res = None

try:
    res = requests.post(
        url=f"{main_app_address}/user/logout",
        data=json.dumps(payload)
    )   
except ConnectionRefusedError as cre:
    print("CONNECTION ERROR --", cre, "\nclosing")
    sys.exit(1)
except requests.exceptions.ConnectionError as reqcre:
    print("CONNECTION ERROR --", reqcre, "\nclosing")
    sys.exit(1)

if res.status_code == status.HTTP_200_OK:
    print("Server OK")
else:
    err = True
    print("ERROR: can't logout")
    print("returned code:", res.status_code)
print("response:")
res_jsonobj = None
res_raw = None
try:
    print("(JSON response)", res.json())
    res_jsonobj = res.json()
except:
    print("(RAW response)", res.text)
    res_raw = res.text

if err:
    print("ERROR: closing")
    sys.exit(1)
print("user logout ADMIN ... END ")