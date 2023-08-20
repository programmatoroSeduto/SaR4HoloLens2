
/* ======================================================

# TRANSACTION (logout) : DEVICE logout

API: api/device/logout
    - REQUEST:
        - user_id
        - device_id
        - session_token
    - RESPONSE.
        - status : OK|KO

## Naming Conventions and Formats

devices follow this naming convention:

- SARHL2_ID0000000000_DEVC
- prefix: SARHL2_
- ID followed by a 10-digit identifier
- postfix: _DEV

## Requests examples

...

## Responses examples

...

====================================================== */










/* ====================================================== 

## CHECK -- TRANSACTION QUERY

- user_id
- device_id
- session_token

====================================================== */

SELECT 

-- l'utente esiste
CASE
    WHEN user_data.USER_ID IS NOT NULL THEN true
    ELSE false
END AS USER_EXISTS_FL

-- il device esiste
,CASE
    WHEN device_data.DEVICE_ID IS NOT NULL THEN true
    ELSE false
END AS DEVICE_EXISTS_FL

-- l'utente può avere un device
, CASE
    WHEN user_data.AUTH_HOLD_DEVICE_FL IS NULL THEN null
    ELSE user_data.AUTH_HOLD_DEVICE_FL
END AS USER_CAN_HOLD_DEVICE_FL

-- il device può essere preso da un utente?
, device_data.DEVICE_IS_HOLDABLE_FL AS DEVICE_IS_HOLDABLE_FL

-- il token è correto e l'utente è online
,CASE
    WHEN user_session.USER_ID IS NOT NULL THEN true
    ELSE false
END AS USER_IS_LOGGED_ID_FL

-- l'utente sta usando il device
, CASE 
    WHEN device_session.DEVICE_ID IS NULL THEN false
    ELSE TRUE
END AS DEVICE_IS_ALREADY_HOLD_FL

-- in caso, è l'utente stesso che lo sta utilizzando?
, CASE 
    WHEN device_session.DEVICE_ID IS NULL THEN null
    WHEN device_session.USER_SESSION_TOKEN_ID <> user_session.USER_SESSION_TOKEN_ID THEN true
    ELSE false
END AS DEVICE_IS_HOLD_BY_DIFFERENT_USER

FROM ( -- device data, capabilities and authorizations
SELECT

DEVICE_ID,
DEVICE_IS_HOLDABLE_FL

FROM sar.D_DEVICE
WHERE NOT(DELETED_FL)
AND DEVICE_ID = %(device_id)s -- REQUEST (device_id)
) AS device_data

LEFT JOIN ( -- user data and authorizations
SELECT

USER_ID,
AUTH_HOLD_DEVICE_FL

FROM sar.D_USER
WHERE NOT(DELETED_FL)
AND USER_ID = %(user_id)s -- REQUEST (user_id)
) AS user_data
ON ( 1=1 )

LEFT JOIN ( -- device status
SELECT

DEVICE_ID,
USER_SESSION_TOKEN_ID

FROM sar.F_DEVICE_ACTIVITY
WHERE 1=1
AND DEVICE_OFF_AT_TS IS NULL
AND DEVICE_ID = %(device_id)s -- REQUEST (device_id)
) AS device_session
ON ( 1=1 )
;










/* ====================================================== 

## CHECK -- OPERATIVE PROCEDURE

check query structure (in order of use):

USER_EXISTS_FL
DEVICE_EXISTS_FL
USER_IS_LOGGED_ID_FL
USER_CAN_HOLD_DEVICE_FL
DEVICE_IS_HOLDABLE_FL
DEVICE_IS_ALREADY_HOLD_FL
DEVICE_IS_HOLD_BY_DIFFERENT_USER

check pseudocode:

```
IF not USER_EXISTS_FL
    -> RETURN : 404 incorrect user, device or token
    -> LOG : unknown user id
END

IF not DEVICE_EXISTS_FL
    -> RETURN : 404 incorrect user, device or token
    -> LOG : unknown device id
END

IF not USER_IS_LOGGED_ID_FL
    -> RETURN : 404 incorrect user, device or token
    -> LOG : user not logged in, cannot find session ID
END 

IF not USER_CAN_HOLD_DEVICE
    -> RETURN : 401 access denied
    -> LOG : user is not allowed to hold any device
END

IF not DEVICE_IS_HOLDABLE_FL
    -> RETURN : 404 incorrect user, device or token
    -> LOG : device cannot be assigned since it is not holdable
END

IF not DEVICE_IS_ALREADY_HOLD_FL
    -> RETURN : 404 incorrect user, device or token
    -> LOG : the user has not a session opened for this device
END

IF DEVICE_IS_ALREADY_HOLD_FL AND DEVICE_IS_HOLD_BY_DIFFERENT_USER
    -> RETURN : 401 access denied
    -> LOG : another user is already holding this device
END

-> RETURN : 200 success
-> LOG : device successfully released by user
```

====================================================== */










/* ====================================================== 

## SUCCESS -- TRANSACTION

====================================================== */

BEGIN;

UPDATE sar.F_DEVICE_ACTIVITY
SET 
    DEVICE_OFF_AT_TS = CURRENT_TIMESTAMP
WHERE USER_SESSION_TOKEN_ID = %(session_token)s
;

INSERT INTO sar.F_ACTIVITY_LOG (
    LOG_TYPE_DS, LOG_TYPE_ACCESS_FL, LOG_SUCCESS_FL, LOG_WARNING_FL, LOG_ERROR_FL, LOG_SECURITY_FAULT_FL
    LOG_SOURCE_ID,
    LOG_DETAILS_DS,
    LOG_DATA
)
VALUES (
    'device logout', true, true, false, false, false,
    'api',
    'device {} successfully logged out',
    %(LOG_DATA)s
)

COMMIT;










/* ====================================================== 

## ERROR -- TRANSACTION

====================================================== */

BEGIN;

INSERT INTO sar.F_ACTIVITY_LOG (
    LOG_TYPE_DS, LOG_TYPE_ACCESS_FL, LOG_SUCCESS_FL, LOG_WARNING_FL, LOG_ERROR_FL, LOG_SECURITY_FAULT_FL,
    LOG_SOURCE_ID,
    LOG_DETAILS_DS,
    LOG_DATA
)
VALUES (
    'device login', true, false, %(LOG_WARNING_FL)s, false, %(LOG_SECURITY_FAULT_FL)s,
    'api',
    %(LOG_DETAILS_DS)s,
    %(LOG_DATA)s
);

COMMIT;










/* ====================================================== 

## ENDING NOTES

...

====================================================== */