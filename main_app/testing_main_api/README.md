# SaR API -- Testing Framework

*A small framework for automated testing on the API*.

## Test Template

**File Name:** *test_(test number).py*

Template V1 for testing:

```py
import os
import sys
import json
from fastapi import status
from testing_hl2_framework import *

interactive = True
test_no_idx = -1
test_steps = []

def wait(msg:str, default_return:str = "", close_command = ":c") -> str:
    if interactive:
        msg_wait = msg + (f"(enter '{close_command}' to close)" if close_command is not None else "")
        res = input(msg_wait) or default_return
        if close_command is not None and res == close_command:
            test_end(True, "Test closed by user")
        else:
            return res
    else:
        return default_return

def test_header():
    global test_idx, test_title, test_description, test_expected_results
    print("===================== * ========================")
    print(f"TEST NUMBER {test_idx} : '{test_title}'\n")
    print(f"->\tDescription:{test_description}\n")
    print(f"->\tExpeced Results:{test_expected_results}\n")
    print("===================== * ========================")

def test_step(msg:str, expected:str = ""):
    global test_no_idx, test_steps
    test_no_idx = test_no_idx + 1
    msg_step = f"[{test_no_idx+1}]\t" + msg + ( ("\n\t\t" + expected) if expected != "" else "" )
    test_steps.append(msg_step)
    print(f"===================== [{test_no_idx+1}] ========================")
    print(msg_step)

def test_step_add_data(msg:str):
    global test_no_idx, test_steps
    test_steps[test_no_idx] += f"\n\t\tRESULTS:\n\t\t{msg}"

def test_end(success:bool, msg:str):
    global test_idx, test_steps
    print("===================== * ========================")
    print(f"TEST NUMBER {test_idx} {'SUCCESS' if success else 'FAILED'}")
    print("->\t" + msg)
    print("Steps performed:\n" + ( "\n".join(test_steps) if len(test_steps) > 0 else "\t...empty"))
    print("===================== * ========================")
    sys.exit(0 if success else 1)




# test header

test_idx = 1
test_title = "...test title..."

test_description = """

"""

test_expected_results = """
...your targets...
"""

test_header()



# test data

# ...
wait("press a key to start the test ... ")
# ...



# test execution

'''
test_step(
    msg = "... step description ...",
    expected = "")

# ...content of the test...
success = True 
# ...content of the test...

# test_step_add_data(f"aaa: {bbb}")
# test_step_add_data(f"aaa: {len(bbb)}")
if not success:
    test_end(False, "cannot acquire resource for USER1")
else:
    wait("OK! Next ...")
'''

# ...



# end of test
test_end(True, "Test succeeded. Closing...")


```

### user + device Login

```py
test_step(
    msg = "USER logs in and acquires one device",
    expected = "the user successfully logs in")
wait("Press a key to start the test ... ")
success, session_token = api_access(
    user_id,
    approver_id,
    access_key,
    device_id
)
if not success:
    test_end(False, "cannot acquire resource for USER1")
else:
    user1_data['session_token'] = session_token
    wait("OK! Next ...")
```

### User + Device logout

```py
test_step(
    msg = "USER logout",
    expected = "logout executed successfully")
wait("Press a key to start the test ... ")
success = api_release(
    user_id,
    session_token
)
if not success:
    test_end(False, "cannot release session for USER1")
else:
    user1_data['session_token'] = None
    user1_data['base_on'] = ""
    wait("OK! Next ...")
```

### Download

```py
test_step(
    msg = "USER 2 first download request",
    expected = "User2 receives 2 positions and two paths")
wait("Press a key to start the test ... ")
success, based_on, max_idx, waypoints, paths = api_download(
    user2_data["user_id"],
    user2_data["device_id"],
    user2_data['ref_id'],
    user2_data['session_token'],
    based_on,
    [0.0, 0.0, 0.0],
    4.1
)
test_step_add_data(f"{(success, based_on, max_idx, waypoints, paths)}")
if not success:
    test_end(False, "cannot perform first download!")
elif based_on == "":
    test_end(False, "fake token is empty!")
else:
    user2_data["base_on"] = based_on
    test_step_add_data(f"max_idx: {max_idx}")
    test_step_add_data(f"len waypoints: {len(waypoints)}")
    test_step_add_data(f"len paths: {len(paths)}")
    wait("OK! Next ...")
```

### Upload

```py
test_step(
    msg = "USER 1 uploads new data",
    expected = "data are structured as a square of max radius 8 from the first point. Expected success and maxIdx greater than zero with no alignment")
wait("Press a key to start the test ... ")
success, max_id, wp_alignment = api_upload(
    user1_data['user_id'],
    user1_data['device_id'],
    user1_data['ref_id'],
    user1_data['session_token'],
    user1_data['base_on'],
    [
        {"pos_id":1,"area_id":0,"v":[0,0,2],"wp_timestamp":"2023/08/29 13:20:57"},
        {"pos_id":2,"area_id":0,"v":[0,0,4],"wp_timestamp":"2023/08/29 13:21:20"},
        {"pos_id":3,"area_id":0,"v":[0,0,6],"wp_timestamp":"2023/08/29 13:21:27"}
    ],
    [
		{"wp1":1,"wp2":0,"dist":2,"pt_timestamp":"2023/08/29 13:21:20"},
		{"wp1":2,"wp2":1,"dist":2,"pt_timestamp":"2023/08/29 13:21:20"},
		{"wp1":3,"wp2":2,"dist":2,"pt_timestamp":"2023/08/29 13:21:27"}
    ]
)
test_step_add_data(f"{(success, max_id, wp_alignment)}")
if not success:
    test_end(False, "cannot upload data from USER1")
else:
    wait("OK! Next ...")
```

### USER 1

```py
user1_data = {
    "user_id" : "SARHL2_ID8849249249_USER",
    "approver_id" : "SARHL2_ID8849249249_USER",
    "access_key" : "anoth3rBr3akabl3P0sswArd",
    "device_id" : "SARHL2_ID8651165355_DEVC",
    "ref_id" : "SARHL2_ID1234567890_REFP",
    "session_token" : None,
    "base_on" : ""
}
print(f"user1 DATA:\n{dict2json(user1_data)}")
```

### USER 2

```py
user2_data = {
    "user_id" : "SARHL2_ID2894646521_USER",
    "approver_id" : "SARHL2_ID2894646521_USER",
    "access_key" : "s3vHngLh_F3s",
    "device_id" : "SARHL2_ID7864861468_DEVC",
    "ref_id" : "SARHL2_ID1234567890_REFP",
    "session_token" : None,
    "base_on" : ""
}
print(f"user2 DATA:\n{dict2json(user2_data)}")
```

---