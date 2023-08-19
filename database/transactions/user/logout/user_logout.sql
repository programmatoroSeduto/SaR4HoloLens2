
/* ======================================================

# TRANSACTION (login) : USER activity ends

API: api/<resource>/<operation>
    - REQUEST:
        - user id
        - access token
    - RESPONSE:
        - status : OK|KO
        - status details : success

## Naming Conventions and Formats

- user : SARHL2_ID00000000_USER

## Requests examples

...

## Responses examples

...

====================================================== */










/* ====================================================== 

## CHECK -- TRANSACTION QUERY

- user exists
- session exists
- user is admin and no other user is approved by their

====================================================== */

SELECT 

-- user exists
user_data.USER_ID AS USER_ID,
-- user is admin
user_data.USER_ADMIN_FL AS USER_ADMIN_FL,
-- user has approved users
CASE 
    WHEN user_data.USER_ADMIN_FL THEN online_users_count.COUNT_APPROVED_USERS_VL 
    ELSE NULL
AS COUNT_APPROVED_USERS_VL,
-- session is opened
CASE
    WHEN user_status.USER_SESSION_TOKEN_ID IS NOT NULL THEN true
    ELSE false
END AS USER_HAS_SESSION_OPENED_FL,
-- session ID is correct (check directly from API service)
user_status.USER_SESSION_TOKEN_ID,

-- (useful for other steps) the user can hold some device?
COALESCE(AUTH_HOLD_DEVICE_FL, false) AS AUTH_HOLD_DEVICE_FL

FROM ( -- user
SELECT 

USER_ID,
USER_ADMIN_FL,
USER_IS_EXTERNAL_FL,
AUTH_HOLD_DEVICE_FL

FROM sar.D_USER 
WHERE 1=1
AND USER_ID='SARHL2_ID1124200849_USER' -- REQUEST (user_id)
AND NOT(DELETED_FL)
) AS user_data

LEFT JOIN ( -- check user status
SELECT

USER_ID,
USER_APPROVER_ID,
USER_START_AT_TS,
USER_SESSION_TOKEN_ID

FROM sar.F_USER_ACTIVITY
WHERE 1=1
AND USER_END_AT_TS IS NULL
) AS user_status
ON ( user_data.USER_ID = user_status.USER_ID )

LEFT JOIN ( -- count users approved by this admin
SELECT 

COUNT(*) AS COUNT_APPROVED_USERS_VL

FROM sar.F_USER_ACTIVITY
WHERE 1=1
AND USER_END_AT_TS IS NULL
AND USER_APPROVER_ID = 'SARHL2_ID1124200849_USER' -- REQUEST (user_id)
AND USER_ID <> USER_APPROVER_ID -- exclude the admin himself
) AS online_users_count
ON ( 1=1 )
;










/* ====================================================== 

## CHECK -- OPERATIVE PROCEDURE

```
IF NOT user exists
    -> RETURN : invalid user or token
    -> LOG : wrong user in logout request
END IF

IF NOT session exists
    -> RETURN invalid user or token
    -> LOG : missing session
END IF

IF NOT token correct
    -> RETURN invalid user or token
    -> LOG : missing session
END IF

IF ( user is admin ) AND ( online users approved by this admin )
    -> RETURN can't log out
    -> LOG : there are tot logged users!
END IF
```

====================================================== */










/* ====================================================== 

## SUCCESS -- TRANSACTION

- (if the user can hold any device) close device sessions 
    attached to the user session
- close the user session

====================================================== */

BEGIN;

-- close device sessions
UPDATE TABLE sar.F_DEVICE_ACTIVITY
SET 
    DEVICE_OFF_AT_TS = CURRENT_TIMESTAMP
WHERE USER_SESSION_TOKEN_ID = '...' -- REQUEST (access token)
RETURNING *
;

-- update log only if the reviously executed query returns something
--    one for each row updated
INSERT INTO sar.F_ACTIVITY_LOG (
    LOG_TYPE_DS, LOG_TYPE_ACCESS_FL, LOG_SUCCESS_FL, LOG_WARNING_FL, LOG_ERROR_FL, LOG_SECURITY_FAULT_FL
    LOG_SOURCE_ID,
    LOG_DETAILS_DS,
    LOG_DATA
)
VALUES (
    'success', true, true, false, false, false,
    'api',
    'device {} successfully logged out',
    '...the JSON packet request...'
)

UPDATE TABLE sar.F_USER_ACTIVITY
SET
    USER_END_AT_TS = CURRENT_TIMESTAMP
WHERE USER_SESSION_TOKEN_ID = '...' -- REQUEST (access token)
RETURNING *
;

-- user logged out
INSERT INTO sar.F_ACTIVITY_LOG (
    LOG_TYPE_DS, LOG_TYPE_ACCESS_FL, LOG_SUCCESS_FL, LOG_WARNING_FL, LOG_ERROR_FL, LOG_SECURITY_FAULT_FL
    LOG_SOURCE_ID,
    LOG_DETAILS_DS,
    LOG_DATA
)
VALUES (
    'success', true, true, false, false, false,
    'api',
    'user {} successfully logged out',
    '...the JSON packet request...'
)

COMMIT;










/* ====================================================== 

## ERROR -- TRANSACTION

====================================================== */

BEGIN;

INSERT INTO sar.F_ACTIVITY_LOG (
    LOG_TYPE_DS, LOG_TYPE_ACCESS_FL, LOG_SUCCESS_FL, LOG_WARNING_FL, LOG_ERROR_FL, LOG_SECURITY_FAULT_FL
    LOG_SOURCE_ID,
    LOG_DETAILS_DS,
    LOG_DATA
)
VALUES (
    'logout error', true, false, false, false, false|true,
    'api',
    '...error message...',
    '...the JSON packet request...'
)

COMMIT;










/* ====================================================== 

## ENDING NOTES

releasing a resource requires generally less controls than accessing
a server resource. One risk that is to minitage is the possibility to
launch a logout on a operator account: to do this, the systems checks
for users' actvities. 

====================================================== */