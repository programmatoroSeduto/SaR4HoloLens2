
import requests
import json
from fastapi import status
import sys

main_app_address = "http://127.0.0.1:5000/api"
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
print("hl2 trying to download positions from server (expected: empty) ... END")

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