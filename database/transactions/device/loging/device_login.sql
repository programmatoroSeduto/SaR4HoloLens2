
/* ======================================================

# TRANSACTION (login) : DEVICE login

API: api/device/login
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

CASE
    WHEN user_data.USER_ID IS NOT NULL THEN true
    ELSE false
END AS USER_EXISTS_FL

,CASE
    WHEN device_data.DEVICE_ID IS NOT NULL THEN true
    ELSE false
END AS DEVICE_EXISTS_FL

,CASE
    WHEN user_session.USER_ID IS NOT NULL THEN true
    ELSE false
END AS USER_IS_LOGGED_ID_FL

-- l'utente è autorizzato ad avere un device? 
, CASE
    WHEN user_data.AUTH_HOLD_DEVICE_FL IS NULL THEN null
    ELSE user_data.AUTH_HOLD_DEVICE_FL
END AS USER_CAN_HOLD_DEVICE_FL

-- l'utente è autirzzato ad avere quel device?
, CASE 
    WHEN user_data.USER_ID IS NULL
    WHEN device_assignment.USER_ID IS NULL THEN false
    ELSE true
END AS USER_CAN_HOLD_GIVEN_DEVICE_FL

-- il device può essere preso da un utente?
, device_data.DEVICE_IS_HOLDABLE_FL AS DEVICE_IS_HOLDABLE_FL

-- qualche altro utente sta già utilizzando il device in questione?
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

-- se il device può scrivere, l'utente è autorizzato alla scrittura?
, device_data.DEV_CAP_WRITE AS DEVICE_CAN_WRITE_FL
, CASE 
    WHEN device_data.DEV_CAP_WRITE AND device_data.DEV_AUTH_WRITE AND user_data.USER_AUTH_WRITE
        THEN true
    ELSE false
END AS USER_DEVICE_AUTH_WRITE_FL

-- se il device può leggere, l'utente è autorizzato alla lettura?
, device_data.DEV_CAP_READ AS DEVICE_CAN_READ_FL
, CASE 
    WHEN device_data.DEV_CAP_READ AND device_data.DEV_AUTH_READ AND user_data.USER_AUTH_READ
        THEN true
    ELSE false
END AS USER_DEVICE_AUTH_READ_FL

FROM ( -- device data, capabilities and authorizations
SELECT

DEVICE_ID,
DEVICE_IS_HOLDABLE_FL,
CAP_EXCHANGE_RECEIVE_FL AS DEV_CAP_READ,
AUTH_ACCESS_DEVICE_FL AS DEV_AUTH_READ,
CAP_EXCHANGE_SEND_FL AS DEV_CAP_WRITE,
AUTH_UPDATE_DEVICE_FL AS DEV_AUTH_WRITE

FROM sar.D_DEVICE
WHERE NOT(DELETED_FL)
AND DEVICE_ID = %(device_id)s -- REQUEST (device_id)
) AS device_data

LEFT JOIN ( -- user data and authorizations
SELECT

USER_ID,
AUTH_HOLD_DEVICE_FL,
AUTH_ACCESS_DEVICE_FL AS USER_AUTH_READ,
AUTH_UPDATE_DEVICE_FL AS USER_AUTH_WRITE

FROM sar.D_USER
WHERE NOT(DELETED_FL)
AND USER_ID = %(user_id)s -- REQUEST (user_id)
) AS user_data
ON ( 1=1 )

LEFT JOIN ( -- user device assignment check 
SELECT

DEVICE_ID, USER_ID

FROM sar.L_DEVICE_USER
WHERE NOT(DELETED_FL)
) AS device_assignment
ON (
    device_data.DEVICE_ID = device_assignment.DEVICE_ID
    AND
    user_data.USER_ID = device_assignment.USER_ID
)

LEFT JOIN ( -- user opened session check
SELECT

USER_ID,
USER_USER_SESSION_TOKEN_ID

FROM sar.F_USER_ACTIVITY
WHERE 1=1
AND USER_END_AT_TS IS NULL
AND USER_ID = %(user_id)s -- REQUEST (user_id)
AND USER_SESSION_TOKEN_ID = %(session_token)s -- REQUEST (session_token)
) AS user_session
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
USER_CAN_HOLD_GIVEN_DEVICE_FL
DEVICE_CAN_WRITE_FL
USER_DEVICE_AUTH_WRITE_FL
DEVICE_CAN_READ_FL
USER_DEVICE_AUTH_READ_FL
DEVICE_IS_ALREADY_HOLD_FL
DEVICE_IS_HOLD_BY_DIFFERENT_USER

Checkings are structured following this procedure:

1. check access coordinates (user, device, token)
2. the check of the token is embedded into the login check
3. authorization: is the device holdable and the user able to hold a device?
4. authorization: is the user/device to perform the uathorizations according to its classification?
5. activity: is the device already hold?

from the general to the particular, trying to find a tradeoff for good 
performances. 

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

IF not USER_CAN_HOLD_GIVEN_DEVICE_FL
    -> RETURN : 401 access denied
    -> LOG : user is not allowed to hold the requested devce
END

IF DEVICE_CAN_WRITE_FL AND not USER_DEVICE_AUTH_WRITE_FL
    -> RETURN : 401 access denied
    -> LOG : selected device requires to write, but either the device or the user is not allowed to write data
END

IF DEVICE_CAN_READ_FL AND not USER_DEVICE_AUTH_READ_FL
    -> RETURN : 401 access denied
    -> LOG : selected device requires to read, but either the device or the user is not allowed to read data
END

IF DEVICE_IS_ALREADY_HOLD_FL
    IF DEVICE_IS_HOLD_BY_DIFFERENT_USER
        -> RETURN : 401 device busy
        -> LOG : another user is already holding this device
    ELSE
        -> RETURN : 400 device busy
        -> LOG : user is already holding device! Asking the device twice
    END
END

-> RETURN : 200 success
-> LOG : device successfully acquired from user

```

NNotice that, in this implementation, the check of the token is
implicit into the check 'USER_IS_LOGGED_ID_FL'

====================================================== */










/* ====================================================== 

## SUCCESS -- TRANSACTION

====================================================== */

BEGIN;

INSERT INTO sar.F_DEVICE_ACTIVITY (
    DEVICE_ID, USER_SESSION_TOKEN_ID
) VALUES (
    %(device_id)s, %(session_token)s
);

INSERT INTO sar.F_ACTIVITY_LOG (
    LOG_TYPE_DS, LOG_TYPE_ACCESS_FL, LOG_SUCCESS_FL, LOG_WARNING_FL, LOG_ERROR_FL, LOG_SECURITY_FAULT_FL,
    LOG_SOURCE_ID,
    LOG_DETAILS_DS,
    LOG_DATA
)
VALUES (
    'device login', true, true, false, false, false,
    'api',
    'device successfully acquired from user',
    %(LOG_DATA)s
);

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