
/* ======================================================

# Datamodel Installation Script

!!! ATTENTION !!!

This script creates database in DROP&CREATE; If you just
need to check the status of the database, please use the
'check_datamodel.sql' script. 

====================================================== */

DROP SCHEMA IF EXISTS sar CASCADE;

CREATE SCHEMA sar;
COMMENT ON SCHEMA sar IS 'Database for Search&Rescue Applications.';










/* ====================================================== 

## USERS

====================================================== */

DROP TABLE IF EXISTS sar.D_USER;
CREATE TABLE sar.D_USER (

    USER_ID CHAR(24) NOT NULL
    , USER_IS_EXTERNAL_FL BOOLEAN NOT NULL
        DEFAULT false
    , USER_NAME_DS VARCHAR(100) NOT NULL
    , USER_PHONE_NUMBER_1_DS VARCHAR(30) 
        DEFAULT NULL
    , USER_PHONE_NUMBER_2_DS VARCHAR(30) 
        DEFAULT NULL
    , USER_ADDRESS_DS VARCHAR(100)
        DEFAULT NULL
    , USER_HEIGHT_VL FLOAT(2) NOT NULL

    -- admins have different checks 
    , USER_ADMIN_FL BOOLEAN NOT NULL
        DEFAULT false

    -- hold device rights
    , AUTH_HOLD_DEVICE_FL BOOLEAN NOT NULL
        DEFAULT false

    -- read rights
    , AUTH_ACCESS_USER_FL BOOLEAN NOT NULL
        DEFAULT false
    , AUTH_ACCESS_DEVICE_FL BOOLEAN NOT NULL
        DEFAULT false
    
    -- write rights
    , AUTH_UPDATE_USER_FL BOOLEAN NOT NULL
        DEFAULT false
    , AUTH_UPDATE_DEVICE_FL BOOLEAN NOT NULL
        DEFAULT false
    
    -- user metadata
    , CREATED_TS TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP
    , DELETED_FL BOOLEAN NOT NULL
        DEFAULT false
    
    , PRIMARY KEY ( USER_ID )
);

DROP TABLE IF EXISTS sar.D_USER_ACCESS_CODE;
CREATE TABLE sar.D_USER_ACCESS_CODE (

    USER_ID CHAR(24) NOT NULL
    , USER_ACCESS_CODE_ID TEXT NOT NULL

    -- metadata
    , CREATED_DT TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP
    , DELETED_FL BOOLEAN NOT NULL
        DEFAULT false

    , PRIMARY KEY ( USER_ID )
);

DROP TABLE IF EXISTS sar.F_USER_ACTIVITY;
CREATE TABLE sar.F_USER_ACTIVITY (
    
    USER_ID CHAR(24) NOT NULL
    , USER_APPROVER_ID CHAR(24) NOT NULL
    
    -- operation timestamps
    , USER_START_AT_TS TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP
    , USER_END_AT_TS TIMESTAMP 

    -- session data
    , USER_SESSION_TOKEN_ID TEXT NOT NULL

    -- metadata
    , CREATED_DT TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP

    , PRIMARY KEY ( USER_ID, USER_START_AT_TS )
);










/* ====================================================== 

## Activity Log

====================================================== */

DROP SEQUENCE IF EXISTS sar.F_ACTIVITY_LOG_SEQUENCE;
CREATE SEQUENCE sar.F_ACTIVITY_LOG_SEQUENCE
    AS BIGINT
    INCREMENT BY 1
    START WITH 1
;

DROP TABLE IF EXISTS sar.F_ACTIVITY_LOG;
CREATE TABLE sar.F_ACTIVITY_LOG (

    F_ACTIVITY_LOG_PK BIGINT NOT NULL
        DEFAULT nextval('sar.F_ACTIVITY_LOG_SEQUENCE')
    
    , LOG_TYPE_DS VARCHAR(120) NOT NULL
        DEFAULT 'success'    
    , LOG_TYPE_ACCESS_FL BOOLEAN
        DEFAULT false
    , LOG_DETAILS_DS VARCHAR(255) NOT NULL
        DEFAULT ''
    , LOG_SOURCE_ID VARCHAR(24)
        DEFAULT null
        
    , LOG_SUCCESS_FL BOOLEAN
        DEFAULT true
    , LOG_WARNING_FL BOOLEAN
        DEFAULT false
    , LOG_ERROR_FL BOOLEAN
        DEFAULT false
    , LOG_SECURITY_FAULT_FL BOOLEAN
        DEFAULT false

    -- metadata
    , CREATED_DT TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP

    , PRIMARY KEY ( F_ACTIVITY_LOG_PK )
);










/* ====================================================== 

## Devices

====================================================== */

DROP TABLE IF EXISTS sar.D_DEVICE;
CREATE TABLE sar.D_DEVICE (

    DEVICE_ID CHAR(24) NOT NULL
    
    -- device characteristics
    , DEVICE_DS VARCHAR(500)
        DEFAULT ''
    , DEVICE_LOCAL_DS VARCHAR(500)
        DEFAULT ''
    , DEVICE_TYPE_DS VARCHAR(255) NOT NULL

    -- if a user can hold the device
    , DEVICE_IS_HOLDABLE_FL BOOLEAN NOT NULL
        DEFAULT false

    -- device capabilities
    , CAP_LOCATION_GEO_FL BOOLEAN NOT NULL
        DEFAULT false
    , CAP_LOCATION_RELATIVE_FL BOOLEAN NOT NULL
        DEFAULT false
    , CAP_EXCHANGE_SEND_FL BOOLEAN NOT NULL
        DEFAULT false
    , CAP_EXCHANGE_RECEIVE_FL BOOLEAN NOT NULL
        DEFAULT false
    , CAP_USAGE_AUTONOMOUS BOOLEAN NOT NULL
        DEFAULT false
    , CAP_USAGE_WEARABLE BOOLEAN NOT NULL
        DEFAULT false

    -- device authorizations
    , AUTH_ACCESS_USER_FL BOOLEAN NOT NULL
        DEFAULT false
    , AUTH_ACCESS_DEVICE_FL BOOLEAN NOT NULL
        DEFAULT false
    , AUTH_UPDATE_USER_FL BOOLEAN NOT NULL
        DEFAULT false
    , AUTH_UPDATE_DEVICE_FL BOOLEAN NOT NULL
        DEFAULT false

    -- metadata
    , CREATED_DT TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP
    , DELETED_FL BOOLEAN NOT NULL
        DEFAULT false

    , PRIMARY KEY ( DEVICE_ID )
);

DROP SEQUENCE IF EXISTS sar.L_DEVICE_USER_SEQUENCE;
CREATE SEQUENCE sar.L_DEVICE_USER_SEQUENCE
    AS BIGINT
    INCREMENT BY 1
    START WITH 1
;

DROP TABLE IF EXISTS sar.L_DEVICE_USER;
CREATE TABLE sar.L_DEVICE_USER (

    L_DEVICE_USER_PK BIGINT NOT NULL
        DEFAULT nextval('sar.L_DEVICE_USER_SEQUENCE')

    , DEVICE_ID CHAR(24) NOT NULL
    , USER_ID CHAR(24) NOT NULL

    -- metadata
    , CREATED_DT TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP
    , DELETED_FL BOOLEAN NOT NULL
        DEFAULT false
    
    , PRIMARY KEY ( L_DEVICE_USER_PK )
);

DROP TABLE IF EXISTS sar.F_DEVICE_ACTIVITY;
CREATE TABLE sar.F_DEVICE_ACTIVITY (

    DEVICE_ID CHAR(24) NOT NULL
    , USER_SESSION_TOKEN_ID TEXT NOT NULL
    
    -- operation timestamps
    , DEVICE_ON_AT_TS TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP
    , DEVICE_OFF_AT_TS TIMESTAMP 

    -- metadata
    , CREATED_DT TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP

    , PRIMARY KEY ( DEVICE_ID, USER_SESSION_TOKEN_ID )
);










/* ======================================================

## Hololens2 Support

====================================================== */

DROP TABLE IF EXISTS sar.D_HL2_USER_DEVICE_SETTINGS;
CREATE TABLE sar.D_HL2_USER_DEVICE_SETTINGS (

    DEVICE_ID CHAR(24) NOT NULL
    , USER_ID CHAR(24) NOT NULL

    -- settings

    , PRIMARY KEY ( DEVICE_ID, USER_ID )
);

DROP TABLE IF EXISTS sar.F_HL2_STAGING_WAYPOINTS;
CREATE TABLE sar.F_HL2_STAGING_WAYPOINTS (

    DEVICE_ID CHAR(24) NOT NULL
    , USER_SESSION_TOKEN_ID TEXT NOT NULL

    -- area center
    , REFERENCE_POSITION_ID TEXT NOT NULL
    , U_LEFT_HANDED_REFERENCE_FL BOOLEAN NOT NULL
        DEFAULT true
    , U_X FLOAT(15) NOT NULL
        DEFAULT 0.00
    , U_Y FLOAT(15) NOT NULL
        DEFAULT 0.00
    , U_Z FLOAT(15) NOT NULL
        DEFAULT 0.00
    , U_SOURCE_FROM_SERVER_FL BOOLEAN NOT NULL
        DEFAULT false
    
    -- measures
    , LOCAL_POSITION_ID INT NOT NULL
    , LOCAL_AREA_INDEX_ID INT NOT NULL
    , AREA_RADIUS_VL FLOAT(4) NOT NULL
        DEFAULT 0.5
    
    -- metadata
    , CREATED_TS TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP
);

DROP SEQUENCE IF EXISTS sar.F_HL2_QUALITY_WAYPOINTS_SEQUENCE;
CREATE SEQUENCE sar.F_HL2_QUALITY_WAYPOINTS_SEQUENCE
    AS BIGINT
    INCREMENT BY 1
    START WITH 1
;

DROP TABLE IF EXISTS sar.F_HL2_QUALITY_WAYPOINTS;
/*
    U_ : relative position (wrt REFERENCE_POSITION_ID)
    G_ : geo positions
*/
CREATE TABLE sar.F_HL2_QUALITY_WAYPOINTS (

    F_HL2_QUALITY_WAYPOINTS_PK BIGINT NOT NULL
        DEFAULT nextval('sar.F_HL2_QUALITY_WAYPOINTS_SEQUENCE')

    -- SOURCE : the first user/device which found the position
    , SOURCE_USER_ID CHAR(24) NOT NULL
    , SOURCE_DEVICE_ID CHAR(24) NOT NULL
    , SOURCE_REFERENCE_POSITION_ID TEXT NOT NULL

    -- relative
    , U_LEFT_HANDED_REFERENCE_FL BOOLEAN NOT NULL
        DEFAULT true
    , U_X DECIMAL(15) NOT NULL
    , U_Y FLOAT(15) NOT NULL
    , U_Z FLOAT(15) NOT NULL

    -- global cartesian
    , G_LEFT_HANDED_REFERENCE_FL BOOLEAN NOT NULL
        DEFAULT true
    , G_X FLOAT(15) NOT NULL
    , G_Y FLOAT(15) NOT NULL
    , G_Z FLOAT(15) NOT NULL

    -- global polar
    , G_LAT FLOAT(15) NOT NULL
    , G_LON FLOAT(15) NOT NULL
    , G_ALT FLOAT(15) NOT NULL
    
    -- metadata
    , CREATED_DT TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP
    , DELETED_FL BOOLEAN NOT NULL
        DEFAULT false
    
    , PRIMARY KEY ( F_HL2_QUALITY_WAYPOINTS_PK )
);

DROP TABLE IF EXISTS sar.F_HL2_STAGING_PATHS;
CREATE TABLE sar.F_HL2_STAGING_PATHS (
    
    DEVICE_ID CHAR(24) NOT NULL
    , USER_SESSION_TOKEN_ID TEXT NOT NULL

    , LOCAL_WAYPOINT_1_ID INT NOT NULL
    , LOCAL_WAYPOINT_2_ID INT NOT NULL
    , SOURCE_FROM_SERVER_FL BOOLEAN NOT NULL
        DEFAULT false
    
    -- metadata
    , CREATED_TS TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP
);

DROP SEQUENCE IF EXISTS sar.F_HL2_QUALITY_PATHS_SEQUENCE;
CREATE SEQUENCE sar.F_HL2_QUALITY_PATHS_SEQUENCE
    AS BIGINT
    INCREMENT BY 1
    START WITH 1
;

DROP TABLE IF EXISTS sar.F_HL2_QUALITY_PATHS;
CREATE TABLE sar.F_HL2_QUALITY_PATHS (

    F_HL2_QUALITY_WAYPOINTS_PK BIGINT NOT NULL
        DEFAULT nextval('sar.F_HL2_QUALITY_PATHS_SEQUENCE')

    -- SOURCE : the first user/device which found the position
    , SOURCE_USER_ID CHAR(24) NOT NULL
    , SOURCE_DEVICE_ID CHAR(24) NOT NULL

    -- metadata
    , CREATED_DT TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP
    , DELETED_FL BOOLEAN NOT NULL
        DEFAULT false
    
    , PRIMARY KEY ( F_HL2_QUALITY_WAYPOINTS_PK )
);

DROP TABLE IF EXISTS sar.D_HL2_REFERENCE_POSITIONS;
CREATE TABLE sar.D_HL2_REFERENCE_POSITIONS (

    REFERENCE_POSITION_ID CHAR(24) NOT NULL

    -- global cartesian (if available)
    , G_LEFT_HANDED_REFERENCE_FL BOOLEAN NOT NULL
        DEFAULT true
    , G_X FLOAT(15)
        DEFAULT null
    , G_Y FLOAT(15)
        DEFAULT null
    , G_Z FLOAT(15)
        DEFAULT null

    -- global polar (if available)
    , G_LAT FLOAT(15)
        DEFAULT null
    , G_LON FLOAT(15)
        DEFAULT null
    , G_ALT FLOAT(15)
        DEFAULT null
    
    -- metadata
    , CREATED_TS TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP
    , DELETED_FL BOOLEAN NOT NULL 
        DEFAULT false

    , PRIMARY KEY ( REFERENCE_POSITION_ID )
);

DROP TABLE IF EXISTS sar.L_HL2_REFERENCE_TRANSFORMATIONS;
/*
    from Unity point of view:

    [direct transform]
    P_to = O_ + Q_ * ( P_from - (0,0,0) )

    [inverse transform]
    P_from = (Q_)^-1 * ( P_to - (0,0,0) ) - (Q_)^-1 * O_
*/
CREATE TABLE sar.L_HL2_REFERENCE_TRANSFORMATIONS (

    REFERENCE_FROM_ID CHAR(24) NOT NULL
    , REFERENCE_TO_ID CHAR(24) NOT NULL
    , REFERENCE_LABEL_DS VARCHAR(500)
        DEFAULT ''

    -- dislocation (in cartesian coordinates)
    , O_LEFT_HANDED_REFERENCE BOOLEAN NOT NULL
        DEFAULT true
    , O_X FLOAT(15) NOT NULL
        DEFAULT 0.0
    , O_Y FLOAT(15) NOT NULL
        DEFAULT 0.0
    , O_Z FLOAT(15) NOT NULL
        DEFAULT 0.0
    
    -- rotation (in quaternions)
    , Q_X FLOAT(15) NOT NULL
        DEFAULT 1.0
    , Q_Y FLOAT(15) NOT NULL
        DEFAULT 0.0
    , Q_Z FLOAT(15) NOT NULL
        DEFAULT 0.0
    , Q_K FLOAT(15) NOT NULL
        DEFAULT 0.0
    
    -- metadata
    , CREATED_TS TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP

    , PRIMARY KEY ( REFERENCE_FROM_ID, REFERENCE_TO_ID )
);

DROP TABLE IF EXISTS sar.F_HL2_SERVICE_STATUS;
CREATE TABLE sar.F_HL2_SERVICE_STATUS (

    DEVICE_ID CHAR(24) NOT NULL
    , USER_SESSION_TOKEN_ID TEXT NOT NULL
    
    -- device status
    , DEVICE_STATUS_DS VARCHAR(100) 
        DEFAULT NULL
    , DEVICE_STATUS_TS TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP

    -- metadata
    , CREATED_DT TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP
    
    , PRIMARY KEY ( DEVICE_ID, USER_SESSION_TOKEN_ID, DEVICE_STATUS_TS )
);










/* ======================================================

## IoT data integration

====================================================== */

DROP TABLE IF EXISTS sar.F_IOT_MEASUREMENTS;
CREATE TABLE sar.F_IOT_MEASUREMENTS (

    DEVICE_ID CHAR(24) NOT NULL
    , USER_SESSION_TOKEN_ID TEXT NOT NULL

    -- geolocation
    , GEO_LAT_VL FLOAT(15)
        DEFAULT null
    , GEO_LON_VL FLOAT(15)
        DEFAULT null
    , GEO_UNCERTAINTY_VL FLOAT(15)
        DEFAULT null
    , GEO_SOURCE_DS VARCHAR(12)
        DEFAULT null

    -- device parameters
    , DEVICE_VOLTAGE_VL FLOAT(15)
        DEFAULT null
    
    -- environmental measurements
    , AIR_QUALITY_VL FLOAT(15)
        DEFAULT null
    , HUMIDITY_VL FLOAT(15)
        DEFAULT null
    , AIR_PRESSURE_VL FLOAT(15)
        DEFAULT null
    , TEMPERATURE_VL FLOAT(15)
        DEFAULT null

    -- metadata
    , CREATED_DT TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP
);










/* ======================================================

## Release Notes - v1.0

Key Entities Definition:

- Devices
    - DEVICE_ID CHAR(24) NOT NULL
- Users
    - USER_ID CHAR(24) NOT NULL
    - USER_ACCESS_CODE_ID TEXT NOT NULL
        - hashed
    - USER_SESSION_TOKEN_ID TEXT NOT NULL
        - hashed

Metadata:

- CREATED_DT TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
- DELETED_FL BOOLEAN NOT NULL DEFAULT false

```sql
-- metadata
, CREATED_DT TIMESTAMP NOT NULL
    DEFAULT CURRENT_TIMESTAMP
, DELETED_FL BOOLEAN NOT NULL
    DEFAULT false
```

====================================================== */