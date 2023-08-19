
/* ======================================================

# TRANSACTION (login) : USER activity begins

API: api/user/login
    - REQUEST:
        - user id
        - approver id
        - access token
    - RESPONSE:
        - status : OK
        - status details : success
        - session_token : ...hash...



## User Naming Convention

- user : SARHL2_ID00000000_USER



## Requests examples

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


## User Login

...

====================================================== */










/* ====================================================== 

## CHECK -- TRANSACTION QUERY

====================================================== */

SELECT -- query is empty if the user doesn't exist

-- user esiste?
user_data.USER_ID as USER_ID,
-- l'utente è esterno? (non può accederecome admin!!!)
user_data.USER_IS_EXTERNAL_FL AS USER_IS_EXTERNAL_FL,
-- lo user è un admin?
user_data.USER_ADMIN_FL AS USER_IS_ADMIN_FL,
-- is user currently active?
CASE 
    WHEN user_status.USER_ID IS NOT NULL THEN true 
    ELSE false
END AS USER_STATUS_IS_ACTIVE_FL,
-- da quando è attivo l'user?
user_status.USER_START_AT_TS AS USER_STATUS_START_AT_DT,
-- chi ha approvato l'user?
user_status.USER_APPROVER_ID AS USER_STATUS_APPROVED_BY_ID,

-- l'approvatore esiste?
user_admin_data.USER_ADMIN_ID AS ADMIN_ID,
-- l'admin è esterno? (non può essere che si usi come admi un esterno)
user_admin_data.USER_IS_EXTERNAL_FL AS ADMIN_EXTERNAL_FL,
CASE
    WHEN user_admin_data.USER_ADMIN_ID IS NOT NULL THEN true
    ELSE false
END AS ADMIN_FOUND_FL,
-- l'approvatore è admin? 
user_admin_data.USER_ADMIN_FL AS ADMIN_IS_ADMIN_FL,
-- l'approvatore possiede diritti di accesso almeno in lettura sull'utente?
user_admin_data.AUTH_ACCESS_USER_FL AS ADMIN_CAN_ACCESS_USER_FL,
-- is the admin currently active if there's a admin for this request?
CASE
    WHEN user_status_admin.USER_ADMIN_ID IS NOT NULL THEN true
    ELSE false
END AS ADMIN_STATUS_IS_ACTIVE_FL,

-- is the user's access key correct?
CASE 
    WHEN user_access.USER_ID IS NOT NULL THEN true
    ELSE false
END AS USER_STATUS_PASS_CHECK_FL

FROM ( -- base user
SELECT 

USER_ID,
USER_ADMIN_FL,
USER_IS_EXTERNAL_FL

FROM sar.D_USER 
WHERE 1=1
AND USER_ID='SARHL2_ID1124200849_USER' -- REQUEST (user_id)
AND NOT(DELETED_FL)
) AS user_data

LEFT JOIN ( -- admin user
SELECT 

USER_ID AS USER_ADMIN_ID,
USER_ADMIN_FL,
AUTH_ACCESS_USER_FL,
USER_IS_EXTERNAL_FL

FROM sar.D_USER
WHERE NOT(DELETED_FL)
AND USER_ID = 'SARHL2_ID1124200849_USER' -- REQUEST (approver_id)
) as user_admin_data
ON ( 1=1 )

LEFT JOIN ( -- check user status
SELECT

USER_ID,
USER_APPROVER_ID,
USER_START_AT_TS

FROM sar.F_USER_ACTIVITY
WHERE 1=1
AND USER_END_AT_TS IS NULL
) AS user_status
ON ( user_data.USER_ID = user_status.USER_ID )

LEFT JOIN ( -- check admin approver status
SELECT

USER_ID AS USER_ADMIN_ID,
USER_START_AT_TS

FROM sar.F_USER_ACTIVITY
WHERE 1=1
AND USER_END_AT_TS IS NULL
AND USER_ID = USER_APPROVER_ID -- admin user
) AS user_status_admin
ON ( user_admin_data.USER_ADMIN_ID = user_status_admin.USER_ADMIN_ID )

LEFT JOIN ( -- check user pass
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










/* ====================================================== 

## CHECK -- OPERATIVE PROCEDURE

```
IF NOT user exists
    -> RETURN : incorrect user, admin or pass
    -> LOG: not found user from request
END IF

IF ( user = admin ) AND ( user is not admin )
    -> RETURN : wrong approver
    -> LOG : trying to access as admin without admin flag
END IF

IF ( user = admin ) AND ( user is external )
    -> RETURN : incorrect user, admin or pass
    -> LOG : a external user can't access as admin
END IF

IF NOT admin exists
    -> RETURN : incorrect user, admin or pass
    -> LOG: not found admin from request
END IF

IF admin is not admin 
    -> RETURN : incorrect user, admin or pass
    -> LOG: declared admin which is not a admin
END IF

IF admin is external
    -> RETURN : incorrect user, admin or pass
    -> LOG: trying to use a external account as approver for a login operation
END IF

IF password is wrong
    -> RETURN : incorrect user, admin or pass
    -> LOG: wrong password
END IF

IF admin has not access to user
    -> RETURN : incorrect user, admin or pass
    -> LOG: trying to access with a admin which is not auhorized to access users
END IF

IF user currently active
    -> RETURN : access denied
    IF user_approver != admin
        -> LOG: session active with one approver, but required the access with another approver
    ELSE
        -> LOG: trying to access a user already logged in
    END IF
END IF

IF admin currently not active
    -> RETURN : access denied
    -> LOG : supervisor not logged in
END IF
```

====================================================== */










/* ====================================================== 

## SUCCESS -- TRANSACTION

- register a new session ID
- register access into the log

====================================================== */

BEGIN; 

INSERT INTO sar.F_USER_ACTIVITY (
    USER_ID, 
    USER_APPROVER_ID,
    
    USER_SESSION_TOKEN_ID
)
VALUES (
    'SARHL2_ID1124200849_USER', -- REQUEST (user_id)
    'SARHL2_ID1124200849_USER', -- REQUEST (approver_id)

    MD5( CONCAT(
        FLOOR(RAND() * 1000000), -- salt
        'SARHL2_ID1124200849_USER', -- REQUEST (user_id)
        FLOOR(RAND() * 1000000), -- salt
        'SARHL2_ID1124200849_USER', -- REQUEST (approver_id)
        FLOOR(RAND() * 1000000), -- salt
    ) )
)
RETURNING 
    USER_ID, USER_APPROVER_ID, USER_SESSION_TOKEN_ID
;

INSERT INTO sar.F_ACTIVITY_LOG (
    LOG_TYPE_DS, LOG_TYPE_ACCESS_FL, LOG_SUCCESS_FL, LOG_WARNING_FL, LOG_ERROR_FL, LOG_SECURITY_FAULT_FL
    LOG_SOURCE_ID,
    LOG_DETAILS_DS,
    LOG_DATA
)
VALUES (
    'success', true, true, false, false, false,
    'api',
    'user (ADMIN|NOT ADMIN) {} successfully logged in (approved by {} (ADMIN))',
    '...the JSON packet request...'
)

COMMIT;










/* ====================================================== 

## ERROR -- TRANSACTION

- store error message in the log

====================================================== */

BEGIN;

INSERT INTO sar.F_ACTIVITY_LOG (
    LOG_TYPE_DS, LOG_TYPE_ACCESS_FL, LOG_SUCCESS_FL, LOG_WARNING_FL, LOG_ERROR_FL, LOG_SECURITY_FAULT_FL
    LOG_SOURCE_ID,
    LOG_DETAILS_DS,
    LOG_DATA
)
VALUES (
    'addess denied', true, false, false, false, false|true,
    'api',
    '...error message...',
    '...the JSON packet request...'
)

COMMIT;










/* ====================================================== 

## ENDING NOTES

...

====================================================== */