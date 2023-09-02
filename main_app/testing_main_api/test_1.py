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
test_title = "initial test"

test_description = """
A simple "batch" test: USER1 writes data, and USER2 reads that data in two steps. 
The test aims at showing that USER2 receives only the needed informations with no redundancy from another device. 
"""

test_expected_results = """
[1]     USER1 logs in and acquires one device
                the user successfully logs in
[2]     USER 1 first download request
                the user acquires no data; a based_on fake token is sent to the user. Wps ad Pts are emtpy lists. MaxIdx is zero.
                RESULTS:
                (True, '283927b6343ed893548458c3b4da874e', 0, [], [])
[3]     USER 1 uploads new data
                data are structured as a square of max radius 8 from the first point. Expected success and maxIdx greater than zero with no alignment
                RESULTS:
                (True, 12, [])
[4]     USER 1 logout
                logout executed successfully
[5]     USER 2 logs in and acquires one device
                USER 2 successfully logs in
[6]     USER 2 first download request
                User2 receives 2 positions and two paths
                RESULTS:
                (True, '0208f5d1cc7da5d714f2b00398b81061', 12, [{'pos_id': 2, 'area_id': 0, 'v': [0.0, 0.0, 4.0], 'wp_timestamp': '2023-08-29T13:21:20'}, {'pos_id': 1, 'area_id': 0, 'v': [0.0, 0.0, 2.0], 'wp_timestamp': '2023-08-29T13:20:57'}], [{'wp1': 1, 'wp2': 0, 'dist': 2.0, 'pt_timestamp': '2023-08-29T13:20:57'}, {'wp1': 2, 'wp2': 1, 'dist': 2.0, 'pt_timestamp': '2023-08-29T13:21:20'}])
[7]     USER 2 second download request
                User2 receives other positions different from the ones received before,, with new paths (each path is linked with another known point or a new one)
                RESULTS:
                (True, None, 12, [{'pos_id': 4, 'area_id': 0, 'v': [0.0, 0.0, 8.0], 'wp_timestamp': '2023-08-29T13:22:57'}, {'pos_id': 3, 'area_id': 0, 'v': [0.0, 0.0, 6.0], 'wp_timestamp': '2023-08-29T13:21:27'}], [{'wp1': 3, 'wp2': 2, 'dist': 2.0, 'pt_timestamp': '2023-08-29T13:21:27'}, {'wp1': 4, 'wp2': 3, 'dist': 2.0, 'pt_timestamp': '2023-08-29T13:22:57'}])        
[8]     USER 2 logout
                logout executed successfully
"""

test_header()



# test data

user1_data = {
    "user_id" : "SARHL2_ID8849249249_USER",
    "approver_id" : "SARHL2_ID8849249249_USER",
    "access_key" : "anoth3rBr3akabl3P0sswArd",
    "device_id" : "SARHL2_ID8651165355_DEVC",
    "ref_id" : "SARHL2_ID1234567890_REFP",
    "session_token" : None,
    "base_on" : ""
}
user2_data = {
    "user_id" : "SARHL2_ID2894646521_USER",
    "approver_id" : "SARHL2_ID2894646521_USER",
    "access_key" : "s3vHngLh_F3s",
    "device_id" : "SARHL2_ID7864861468_DEVC",
    "ref_id" : "SARHL2_ID1234567890_REFP",
    "session_token" : None,
    "base_on" : ""
}

print(f"user1 DATA:\n{dict2json(user1_data)}")
print(f"user2 DATA:\n{dict2json(user2_data)}")

wait("press a key to start the test ... ")
# ...



# test execution

test_step(
    msg = "USER1 logs in and acquires one device",
    expected = "the user successfully logs in")
wait("Press a key to start the test ... ")
success, session_token = api_access(
    user1_data['user_id'],
    user1_data["approver_id"],
    user1_data["access_key"],
    user1_data['device_id']
)
if not success:
    test_end(False, "cannot acquire resource for USER1")
else:
    user1_data['session_token'] = session_token
    wait("OK! Next ...")

# ...

test_step(
    msg = "USER 1 first download request",
    expected = "the user acquires no data; a based_on fake token is sent to the user. Wps ad Pts are emtpy lists. MaxIdx is zero. ")
wait("Press a key to start the test ... ")
success, based_on, max_idx, waypoints, paths = api_download(
    user1_data["user_id"],
    user1_data["device_id"],
    user1_data['ref_id'],
    user1_data['session_token'],
    "",
    [0.0, 0.0, 0.0],
    250.0
)
test_step_add_data(f"{(success, based_on, max_idx, waypoints, paths)}")
if not success:
    test_end(False, "cannot perform first download!")
elif based_on == "":
    test_end(False, "fake token is empty!")
else:
    user1_data["base_on"] = based_on
    wait("OK! Next ...")

# ...

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
        {"pos_id":3,"area_id":0,"v":[0,0,6],"wp_timestamp":"2023/08/29 13:21:27"},
        {"pos_id":4,"area_id":0,"v":[0,0,8],"wp_timestamp":"2023/08/29 13:22:57"},
        {"pos_id":5,"area_id":0,"v":[2,0,8],"wp_timestamp":"2023/08/29 13:22:20"},
        {"pos_id":6,"area_id":0,"v":[4,0,8],"wp_timestamp":"2023/08/29 13:23:27"},
        {"pos_id":7,"area_id":0,"v":[6,0,8],"wp_timestamp":"2023/08/29 13:24:57"},
        {"pos_id":8,"area_id":0,"v":[8,0,8],"wp_timestamp":"2023/08/29 13:25:20"},
        {"pos_id":9,"area_id":0,"v":[8,0,6],"wp_timestamp":"2023/08/29 13:26:27"},
        {"pos_id":10,"area_id":0,"v":[8,0,4],"wp_timestamp":"2023/08/29 13:27:57"},
        {"pos_id":11,"area_id":0,"v":[8,0,2],"wp_timestamp":"2023/08/29 13:28:20"},
        {"pos_id":12,"area_id":0,"v":[8,0,0],"wp_timestamp":"2023/08/29 13:29:27"}
    ],
    [
		{"wp1":1,"wp2":0,"dist":2,"pt_timestamp":"2023/08/29 13:21:20"},
		{"wp1":2,"wp2":1,"dist":2,"pt_timestamp":"2023/08/29 13:21:20"},
		{"wp1":3,"wp2":2,"dist":2,"pt_timestamp":"2023/08/29 13:21:27"},
		{"wp1":4,"wp2":3,"dist":2,"pt_timestamp":"2023/08/29 13:21:20"},
		{"wp1":5,"wp2":4,"dist":2,"pt_timestamp":"2023/08/29 13:21:20"},
		{"wp1":6,"wp2":5,"dist":2,"pt_timestamp":"2023/08/29 13:21:27"},
		{"wp1":7,"wp2":6,"dist":2,"pt_timestamp":"2023/08/29 13:21:20"},
		{"wp1":8,"wp2":7,"dist":2,"pt_timestamp":"2023/08/29 13:21:20"},
		{"wp1":9,"wp2":8,"dist":2,"pt_timestamp":"2023/08/29 13:21:27"},
		{"wp1":10,"wp2":9,"dist":2,"pt_timestamp":"2023/08/29 13:21:20"},
		{"wp1":11,"wp2":10,"dist":2,"pt_timestamp":"2023/08/29 13:21:20"},
		{"wp1":12,"wp2":11,"dist":2,"pt_timestamp":"2023/08/29 13:21:27"}
    ]
)
test_step_add_data(f"{(success, max_id, wp_alignment)}")
if not success:
    test_end(False, "cannot upload data from USER1")
else:
    wait("OK! Next ...")

# ...

test_step(
    msg = "USER 1 logout",
    expected = "logout executed successfully")
wait("Press a key to start the test ... ")
success = api_release(
    user1_data['user_id'],
    user1_data['session_token']
)
if not success:
    test_end(False, "cannot release session for USER1")
else:
    user1_data['session_token'] = None
    user1_data['base_on'] = ""
    wait("OK! Next ...")

# ...

test_step(
    msg = "USER 2 logs in and acquires one device",
    expected = "USER 2 successfully logs in")
wait("Press a key to start the test ... ")
success, session_token = api_access(
    user2_data['user_id'],
    user2_data["approver_id"],
    user2_data["access_key"],
    user2_data['device_id']
)
if not success:
    test_end(False, "cannot acquire resources for USER2")
else:
    user2_data['session_token'] = session_token
    wait("OK! Next ...")

# ...

test_step(
    msg = "USER 2 first download request",
    expected = "User2 receives 2 positions and two paths")
wait("Press a key to start the test ... ")
success, based_on, max_idx, waypoints, paths = api_download(
    user2_data["user_id"],
    user2_data["device_id"],
    user2_data['ref_id'],
    user2_data['session_token'],
    "",
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

# ...

test_step(
    msg = "USER 2 second download request",
    expected = "User2 receives other positions different from the ones received before,, with new paths (each path is linked with another known point or a new one)")
wait("Press a key to start the test ... ")
success, based_on, max_idx, waypoints, paths = api_download(
    user2_data["user_id"],
    user2_data["device_id"],
    user2_data['ref_id'],
    user2_data['session_token'],
    user2_data['base_on'],
    [0.0, 0.0, 0.0],
    8.2
)
test_step_add_data(f"{(success, based_on, max_idx, waypoints, paths)}")
if not success:
    test_end(False, "cannot perform download!")
else:
    test_step_add_data(f"max_idx: {max_idx}")
    test_step_add_data(f"len waypoints: {len(waypoints)}")
    test_step_add_data(f"len paths: {len(paths)}")
    wait("OK! Next ...")

# ...

test_step(
    msg = "USER 2 logout",
    expected = "logout executed successfully")
wait("Press a key to start the test ... ")
success = api_release(
    user2_data['user_id'],
    user2_data['session_token']
)
if not success:
    test_end(False, "cannot release session for USER1")
else:
    user2_data['session_token'] = None
    user2_data['base_on'] = ""
    wait("OK! Next ...")

# ...

# end of test
test_end(True, "Test succeeded. Closing...")

