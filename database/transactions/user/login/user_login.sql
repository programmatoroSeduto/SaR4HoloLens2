
/* ======================================================

# TRANSACTION (login) : USER activity begins

API: api/access/login
    - REQUEST:
        - user id
        - approver id
        - access token
    - RESPONSE:
        - OK or not OK ...?

if the user is not approver, the check also involves the 
actvity status of the approver user. Admins are distinguished
by common users since user_id and user_approver_id are the same. 
It enables for example to know when a malicious user tries to send
a request pretending to be a admin: if the flag admin is false, or
the admin account is already active, there's something potentially
bad thst it is happening. 

(when I write "if ... is ...", just take into account that if that 
condition is not satisfied, it will be taken a action to deal with 
the problem)

- CHECK if users codes are the same, 
    - (D_USER) if the user exists
    - (D_USER) if the user is really admin
    - (F_USER_ACTIVITY) if the user is not already active
    - (D_USER_ACCESS_CODE) if the access hash of the access code is correct
- CHECK otherwise
    - (D_USER) if the approver exists
    - (F_USER_ACTIVITY) if the approver is active
    - (D_USER) if the user exists
    - (D_USER_ACCESS_CODE) if the access hash of the user is correct
    - (F_USER_ACTIVITY) if the user is currently active
- INSERT user starts activity 
    - (F_USER_ACTIVITY) create activity
    - (F_USER_ACTIVITY) assign session token
    - (F_ACTIVITY_LOG) log success
- and send back to the user

## User Naming Convention

- user : SARHL2_ID00000000_USER

====================================================== */










/* ====================================================== 

## Admin Login

REQUEST: the following is accepted (admin user and correct password)

{
    'user_id' : 'SARHL2_ID1124200849_USER',
    'user_approver_id' : 'SARHL2_ID1124200849_USER',
    'access_key' : 'aaaaaa'
}

REQUEST: the following is potentially malicious (exteral user!)

{
    'user_id' : 'SARHL2_ID5566448822_USER',
    'user_approver_id' : 'SARHL2_ID5566448822_USER',
    'access_key' : 'bbbbbb'
}

REQUEST: the following has the access_key wrong (admin user but not correct password)

{
    'user_id' : 'SARHL2_ID1124200849_USER',
    'user_approver_id' : 'SARHL2_ID1124200849_USER',
    'access_key' : 'bbbbbb'
}

====================================================== */

-- (D_USER) the user exists
-- (D_USER) if the user is really admin
-- (F_USER_ACTIVITY) if the user is not already active
-- (D_USER_ACCESS_CODE) if the access hash of the access code is correct
SELECT

user_data.USER_ID as USER_ID,
user_data.USER_ADMIN_FL AS USER_IS_ADMIN_FL,
CASE 
    WHEN user_status.USER_ID IS NOT NULL THEN true 
    ELSE false
END AS USER_STATUS_IS_ACTIVE_FL,
user_status.USER_START_AT_TS AS USER_STATUS_START_AT_DT,
CASE 
    WHEN user_access.USER_ID IS NOT NULL THEN true
    ELSE false
END AS USER_STATUS_PASS_CHECK_FL

FROM (
SELECT 

USER_ID,
USER_ADMIN_FL

FROM sar.D_USER 
WHERE 1=1
AND USER_ID='SARHL2_ID1124200849_USER' -- REQUEST (user_id oppure user_approver_id)
AND NOT(DELETED_FL)
) AS user_data

LEFT JOIN (
SELECT

USER_ID,
USER_APPROVER_ID,
USER_START_AT_TS

FROM sar.F_USER_ACTIVITY
WHERE 1=1
AND USER_END_AT_TS IS NULL
) AS user_status
ON ( user_data.USER_ID = user_status.USER_ID )

LEFT JOIN (
SELECT 

USER_ID,
USER_ACCESS_CODE_ID 

FROM sar.D_USER_ACCESS_DATA
WHERE NOT(DELETED_FL)
) AS user_access
ON ( 
    user_data.USER_ID = user_access.USER_ID
    AND
    MD5('aaaaaa') = user_access.USER_ACCESS_CODE_ID -- REQUEST (access_key)
    ) 
;

