
/* ======================================================

# TRANSACTION (HL2) : Upload Positions From Device 

API: api/hl2/{HL2_device}/upload
    - PATH
        - device_id
    - REQUEST:
        - user_id
        - session_token
        - ... JSON representation of the content of the database from HL2 ...
        - full_export : is this a full export from the position database or a export of only newly generated positions?
    - RESPONSE:
        - OK|KO
        - ... support informations for HL2 ...

## High-level import procedure

during the upload process, data are uploaded into the staging folder. 
Positions are related to the session ID, hence the data upload is simply
a table update in append. 

- full export ? 
    - (full) DELETE waypoints using the session token and the device id
    - (full) DELETE links using session ref
    - (full) DELETE renamings
    - (full) JSON object pre-parsing
    - (full) INSERT waypooints 
    - (full) INSERT links
    - (full) INSERT renamings
    - (HL2 side) mark all poitions as exported
- not full export
    - ... yet not implemented ...

## Requests examples

...

## Responses examples

...

====================================================== */










/* ====================================================== 

## CHECK -- TRANSACTION QUERY -- standard check

Checks are simpler than the ones required to acquire/release resources:

- user session is found and the association is correct
- the join with device session id is null and the device is correct

plus this:

- the device is of kind hololens2
- the user is authorized to send infos to the server
- the device has the capability to upload infos
- the device is authorized to send infos to the server

====================================================== */

SELECT 

user_data.USER_ID,
user_data.AUTH_HOLD_DEVICE_FL,
user_data.USER_AUTH_UPLOAD_FL,
CASE
    WHEN user_session.SESSION_ID IS NULL THEN false
    ELSE true
END AS SESSION_ID_FOUND_FL,
CASE
    WHEN user_device_lookup.USER_ID IS NULL THEN false
    ELSE true
END AS LOOKUP_DEVICE_FOUND_FL,
COALESCE(device_data.DEVICE_CAP_UPLOAD_FL, false) AS DEVICE_CAP_UPLOAD_FL,
COALESCE(device_data.CAP_LOCATION_FL, false) AS CAP_LOCATION_FL,
COALESCE(device_data.DEVICE_AUTH_UPLOAD_FL, false) AS DEVICE_AUTH_UPLOAD_FL,
CASE 
    WHEN device_session.SESSION_ID IS NULL THEN false
    ELSE true
END AS DEVICE_HAS_SESSION_OPENED_FL

FROM ( -- check user existence and auths
SELECT 

USER_ID, 
AUTH_HOLD_DEVICE_FL,
AUTH_UPDATE_DEVICE_FL AS USER_AUTH_UPLOAD_FL

FROM sar.D_USER
WHERE NOT(DELETED_FL)
) AS user_data

LEFT JOIN ( -- check session token
SELECT 

USER_ID, 
USER_SESSION_TOKEN_ID AS SESSION_ID

FROM sar.F_USER_ACTIVITY
WHERE 1=1
AND USER_SESSION_TOKEN_ID = %(session_token)s
) AS user_session
ON( user_data.USER_ID = user_session.USER_ID )

LEFT JOIN ( -- check device user association
SELECT 

USER_ID,
DEVICE_ID

FROM sar.L_DEVICE_USER
WHERE 1=1
AND NOT(DELETED_FL)
AND DEVICE_ID = %(device_id)s
) AS user_device_lookup
ON ( user_data.USER_ID = user_device_lookup.USER_ID )

LEFT JOIN ( -- check device data and auth
SELECT

DEVICE_ID,
CAP_EXCHANGE_SEND_FL AS DEVICE_CAP_UPLOAD_FL,
CASE
    WHEN CAP_LOCATION_RELATIVE_FL IS NULL OR CAP_LOCATION_GEO_FL IS NULL THEN false
    WHEN CAP_LOCATION_RELATIVE_FL OR CAP_LOCATION_GEO_FL THEN true
    ELSE false
END AS CAP_LOCATION_FL,
AUTH_UPDATE_DEVICE_FL AS DEVICE_AUTH_UPLOAD_FL

FROM sar.D_DEVICE
WHERE NOT (DELETED_FL)
AND DEVICE_TYPE_DS = 'Microsoft HoloLens2'
) AS device_data
ON ( user_device_lookup.DEVICE_ID = device_data.DEVICE_ID )

LEFT JOIN (
SELECT 

DEVICE_ID, 
USER_SESSION_TOKEN_ID AS SESSION_ID

FROM sar.F_DEVICE_ACTIVITY
WHERE 1=1
AND DEVICE_ID = %(device_id)s
) AS device_session
ON ( 
    user_device_lookup.DEVICE_ID = device_data.DEVICE_ID
    AND
    user_session.SESSION_ID = device_session.SESSION_ID
    )
;










/* ====================================================== 

## CHECK -- OPERATIVE PROCEDURE

Security checks:

```
IF query is empty
    -> RETURN : 404 wrong access coordinates
    -> LOG : user not found
END IF

IF not AUTH_HOLD_DEVICE_FL
    -> RETURN : 404 wrong access coordinates
    -> LOG : user not authorized to hold devices
END IF

IF not USER_AUTH_UPLOAD_FL
    -> RETURN : 401 unauthorized
    -> LOG : user not allowed to upload!
END

IF not SESSION_ID_FOUND_FL
    -> RETURN : 404 wrong access coordinates
    -> LOG : session ID not found or not correct
END IF

IF not LOOKUP_DEVICE_FOUND_FL
    -> RETURN : 401 unauthorized
    -> LOG : device seems not assigned to user
END IF

IF not DEVICE_CAP_UPLOAD_FL
    -> RETURN : 404 wrong access coordinates
    -> LOG : device has not capability to upload
    -> SET UNSECURE REQUEST !
END IF

IF not CAP_LOCATION_FL
    -> RETURN : 404 wrong access coordinates
    -> LOG : device has not capability to locate itself
    -> SET UNSECURE REQUEST !
END IF

IF not DEVICE_AUTH_UPLOAD_FL
    -> RETURN : 401 unauthorized
    -> LOG : device is not authorized to upload data
END IF 

IF NOT DEVICE_HAS_SESSION_OPENED_FL
    -> RETURN : 401 unauthorized
    -> LOG : device seems to not have a session opened
END IF
```

====================================================== */










/* ====================================================== 

## SUCCESS -- TRANSACTION

- ...

====================================================== */

BEGIN;

-- ... --

COMMIT;










/* ====================================================== 

## ERROR -- TRANSACTION

====================================================== */

BEGIN;

-- ... --

COMMIT;










/* ====================================================== 

## ENDING NOTES

...

====================================================== */