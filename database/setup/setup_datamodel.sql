
/* ======================================================

# The SAR PROJECT : Data Model

## sar -- the main schema

'sar' contais the entire set of tables for managing
devices (HoloLens2, Environmental Sensors, ...) as well 
as organization (API login data and currently active
sessions, user grants, ...)

## Columns Naming Conventions

each columns follows this simple naming convention:

- <identifier>_<data type>

About suffixes: it indicates the type of the column

- _FL : flag, boolean
- _TS : timestamp
- _DT : date
- _HR : hour
- _DS : description
- _ID : identifier
- _PK : primary key
    - when present, the column naming convention is <table name>_PK
- _FK : foreign key
    - when present, the column naming convention is <table name>_FK

## Tables Naming Conventions

tables are identified by the prefix of the name:

- D_ : dimensions (describing entities, ES: list of users and their data)
- F_ : facts (records generated in time, ES: a log table)
- L_ : lookup (relations between objects, ES: user holding a device)

## ID and codes

There are some conventions to recall, depending on what
user or entity it is handled:

- user : SARHL2_ID00000000_USER
- device : SARHL2_ID00000000_DEVC
- reference position : SARHL2_ID00000000_REFP

conventions:

- each ID starts with SARHL2_
- the ID part has prefix 'ID' and has 10 digits after that
- the end of the name is the ype of entity

Each entity is stored into the 'sar' datamodel, and they
are assigned *authorizations* to them as flags. 

## Special codes

- the codes with ID0000000000 are technical codes, not to use.
- the system considers some special positions:
    - pcDevPosID = "SARHL2_ID90909091_REFP"
    - pcDevCalibPosID = "SARHL2_ID06600660_REFP"
    - deviceCalibPosID = "SARHL2_ID12700385_REFP"
    - deviceNoCalibPosID = "SARHL2_66660000_REFP"
    - prodDevicePosID = "SARHL2_ID00000000_REFP"

In particular the last one, 'SARHL2_ID00000000_REFPOS', is
used by active devices to ask their reference position. 

====================================================== */

DROP SCHEMA IF EXISTS sar CASCADE;

CREATE SCHEMA sar;
COMMENT ON SCHEMA sar IS 'Database for Search&Rescue Applications.';










/* ====================================================== 

## Users and activities

it represents the pisical person handling a device or 
partecipating the operations. 

- D_USER : user dimension
    - KEY : USER_ID
- F_USER-ACTIVITY : when the user starts participating the operations
    - KEY : USER_ID, START_AT_TS

## About Entities Authorization

to make more secute the data model, the tables D_USER, D_DEVICE 
and other tables, explicitly declare what a user can, or cannot
do. Tables declaring authorizations have some field AUTH_ which 
names follow this naming convention:

- AUTH_<auth topic>_<auth entity>_FL

auth topics are:

- HOLD : the entity can own or hold something else. Only for devices
- ACCESS : the entity can read data of another entity from the database
- UPDATE : the user can modify the data of another entity

Here are some use cases and scenarios:

- HOLD : someone else, not belonging to the organization, tries to 
    access the system pretending to be a HL2 device; the user cannot
    update data, so the "fake user" can just access data if it is
    allowed. It also could happen that the user has been deleted, 
    enabling the system to track someone not belonging to the
    organization and then sending signals to the control center. Or,
    again, the user and the device are not associated, then there's
    something wrong with that request. 

The more infos you have about how a user can and cannot do, the more
the system can react to a malicious scenario if any. 

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
    , USER_HEIGHT_VL FLOAT(2)
        DEFAULT null

    -- admins have different checks 
    , USER_ADMIN_FL BOOLEAN NOT NULL
        DEFAULT false
    , USER_APPROVED_BY_ID CHAR(24)
        DEFAULT null

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
    , ANNOTATIONS_DS VARCHAR(500) 
        DEFAULT NULL
    
    , PRIMARY KEY ( USER_ID, CREATED_TS, DELETED_FL )
);
COMMENT 
    ON COLUMN sar.D_USER.USER_APPROVED_BY_ID 
    IS 'a common user can be approved only by one admin (null for admins)';
COMMENT 
    ON COLUMN sar.D_USER.USER_HEIGHT_VL 
    IS 'User''s height in meters (nullable field)';
COMMENT 
    ON COLUMN sar.D_USER.ANNOTATIONS_DS 
    IS 'Use this field to attach some annotation to the record';

DROP TABLE IF EXISTS sar.D_USER_ACCESS_DATA;
CREATE TABLE sar.D_USER_ACCESS_DATA (

    USER_ID CHAR(24) NOT NULL
    , USER_ACCESS_CODE_ID TEXT NOT NULL

    -- metadata
    , CREATED_TS TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP
    , DELETED_FL BOOLEAN NOT NULL
        DEFAULT false
    , ANNOTATIONS_DS VARCHAR(500) 
        DEFAULT NULL

    , PRIMARY KEY ( USER_ID, DELETED_FL )
);
COMMENT 
    ON COLUMN sar.D_USER_ACCESS_DATA.USER_ACCESS_CODE_ID 
    IS 'hashed with MD5 algorithm';

DROP TABLE IF EXISTS sar.F_USER_ACTIVITY;
CREATE TABLE sar.F_USER_ACTIVITY (
    
    USER_ID CHAR(24) NOT NULL
    , USER_APPROVER_ID CHAR(24) NOT NULL
    
    -- operation timestamps
    , USER_START_AT_TS TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP
    , USER_END_AT_TS TIMESTAMP 
        DEFAULT NULL

    -- session data
    , USER_SESSION_TOKEN_ID TEXT NOT NULL

    -- metadata
    , CREATED_TS TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP

    , PRIMARY KEY ( USER_ID, USER_START_AT_TS )
);
COMMENT 
    ON COLUMN sar.F_USER_ACTIVITY.USER_END_AT_TS 
    IS 'null if the session is currently active';
COMMENT 
    ON COLUMN sar.F_USER_ACTIVITY.USER_APPROVER_ID 
    IS 'equals to USER_ID if the user is admin';










/* ======================================================

## log table

This table allows the control center to check the requrests made upon
the system. In particular, it enables he admins to check if suspicious
requests have been sent to the system. 

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
    , LOG_DATA TEXT
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
    , CREATED_TS TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP

    , PRIMARY KEY ( F_ACTIVITY_LOG_PK )
);
COMMENT 
    ON COLUMN sar.F_ACTIVITY_LOG.LOG_TYPE_DS 
    IS 'The type of the log record. types are: success, warning, error, security_fault';
COMMENT 
    ON COLUMN sar.F_ACTIVITY_LOG.LOG_SOURCE_ID 
    IS 'It identifies the component of the system which creaed the log record. sources are: api, data_integrator, data_processor, ...';
COMMENT 
    ON COLUMN sar.F_ACTIVITY_LOG.LOG_TYPE_ACCESS_FL 
    IS 'it marks the log lines from the login procedure. It indicates that the log is referred to the acquisition or release of a resource.';
COMMENT 
    ON COLUMN sar.F_ACTIVITY_LOG.LOG_SECURITY_FAULT_FL 
    IS 'It is a warning to mark suspicious attempts to acess the resource';
COMMENT 
    ON COLUMN sar.F_ACTIVITY_LOG.LOG_DATA 
    IS 'It contains a copy of the data received from a request, for instance, some JSON code.';
    
DROP TABLE IF EXISTS sar.F_SESSION_ALIAS;
CREATE TABLE sar.F_SESSION_ALIAS (

    DEVICE_ID CHAR(24) NOT NULL
    , USER_ID CHAR(24) NOT NULL
    , OWNER_SESSION_TOKEN_ID TEXT NOT NULL
    , USER_SESSION_TOKEN_ID TEXT
    , SALT_ID TEXT NOT NULL
    , FAKE_SESSION_TOKEN_ID TEXT NOT NULL

    -- metadata
    , CREATED_TS TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP
);
COMMENT 
    ON COLUMN sar.F_SESSION_ALIAS.OWNER_SESSION_TOKEN_ID 
    IS 'the user session which required the creation of this fake token';
COMMENT 
    ON COLUMN sar.F_SESSION_ALIAS.USER_SESSION_TOKEN_ID 
    IS 'this is not the owner of the session token, but the session token which is mapped to the faked token. The system could also decide to generate a completely fake token, using just the salt';
COMMENT 
    ON TABLE sar.F_SESSION_ALIAS 
    IS 'for security reasons, it is strongly discouraged to echo a session token inside a response; for this reason, the server can map a session token in another fake session token';
COMMENT 
    ON COLUMN sar.F_SESSION_ALIAS.SALT_ID 
    IS 'salt is generated as MD5 of a random number';
COMMENT 
    ON COLUMN sar.F_SESSION_ALIAS.FAKE_SESSION_TOKEN_ID 
    IS 'generated by MD5 of the concat: SALT + TOKEN + SALT';










/* ======================================================

## devices

once the user obtained the access to the database, it can be
assigned a device to their. References to the device item is
stored in the table D_DEVICES, with their type and which user
is holding the resource.

### Device IDs

devices follow this naming convention:

- SARHL2_ID0000000000_DEVC
- prefix: SARHL2_
- ID followed by a 10-digit identifier
- postfix: _DEV

### Device Capabilities

for instance, 

- HoloLens2 : 
    - LOCATION_GEO=false : in this implementation, hololens2 can't determine its position
        withuout some support
    - LOCATION_RELATIVE=true : in this implementation, the device is able
        to locate itself wrt a given position
    - EXCHANGE_SEND=true : it can send informations to the server
    - EXCHANGE_RECEIVE=true : it can interpret statements from the server
    - DATASOURCE_EXTERNAL=false : the device itself is a datasource, actively
        interacting with the system
- Thingy:91 : 
    - LOCATION_GEO=true
    - LOCATION_RELATIVE=false : it doesn't need it (it could be true as well in this case)
    - EXCHANGE_SEND=false : informations have to be obtained from a external API 
    - EXCHANGE_RECEIVE=false : the device can't communicate directly with the system
    - DATASOURCE_EXTERNAL=true : it is integrated through a custom data integrator
- a aerial drone : (it is referred to a particular implementation, just to make an example)
    - LOCATION_GEO=true : hopefully it can determine its position through a GPS
    - LOCATION_RELATIVE=true : it can be regulated to infer its position given a reference
    - EXCHANGE_SEND=true : it can send infos to the DB
    - EXCHANGE_RECEIVE=true : it can receive commands from a central unit
    - DATASOURCE_EXTERNAL=false : the device itself is a source of information

If you think this taxonomy is not helful, I suggest you to remember the situation
in which someone malicious tries to access the server with a fake device: you know
how the

here are the capabilities a device can have in this data model:

- LOCATION
    - LOCATION_GEO : the ability to geolocalize itself in pure geocoordinates
    - LOCATION_RELATIVE : the ability to determine its position given
        a reference point from the server or other entities
- EXCHANGE
    - EXCHANGE_SEND : the device has the ability to send data to the server
    - EXCHANGE_RECEIVE : the device can receive and execute statements from the server
- DATASOURCE
    - DATASOURCE_EXTERNAL : the device is integrated through a external API
- USAGE
    - USAGE_AUTONOMOUS : it is a autonomous device
    - USAGE_WEARABLE : wearable device

### Hold a device

it is sufficient to have a active user session to hold a device with a given ID. 

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
    , CREATED_TS TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP
    , DELETED_FL BOOLEAN NOT NULL
        DEFAULT false
    , ANNOTATIONS_DS VARCHAR(500) 
        DEFAULT NULL

    , PRIMARY KEY ( DEVICE_ID, CREATED_TS, DELETED_FL )
);
COMMENT 
    ON COLUMN sar.D_DEVICE.CAP_EXCHANGE_SEND_FL 
    IS 'The device has the capability to write some data to the DB (but not necessarly access to the DB)';
COMMENT 
    ON COLUMN sar.D_DEVICE.AUTH_UPDATE_DEVICE_FL 
    IS 'The device has the authorization to write some data to the DB (this includes its self status)';
COMMENT 
    ON COLUMN sar.D_DEVICE.CAP_EXCHANGE_RECEIVE_FL 
    IS 'The device has the capability to read data from the DB  (but not necessarly access to the DB)';
COMMENT 
    ON COLUMN sar.D_DEVICE.AUTH_ACCESS_DEVICE_FL 
    IS 'The device has the authorization to read data from the DB (this includes its self status)';


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
    , CREATED_TS TIMESTAMP NOT NULL
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
    , CREATED_TS TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP

    , PRIMARY KEY ( DEVICE_ID, DEVICE_ON_AT_TS, USER_SESSION_TOKEN_ID )
);










/* ======================================================

## Hololens2 Support

Here are the requirements for the datamodel for HoloLens2 in this implementation:

- device settings per user -> D_HL2_USER_DEVICE_SETTINGS
    - the device can import/export its settings, for example the height of the user
- positions export from the position database -> F_HL2_STAGING_WAYPOINTS, F_HL2_QUALITY_WAYPOINTS
    - related to one user and one device
    - related to one known reference position
- links from the position database -> F_HL2_STAGING_PATHS, F_HL2_QUALITY_PATHS
    - related to one user nd one device
- reference positions -> D_HL2_REFERENCE_POSITIONS
    - at least ID and operative protocol to determine the calibration
    - in addition, the geolocation if available
- transformations between reference positions -> L_HL2_REFERENCE_TRANSFORMATIONS
- service status -> F_HL2_SERVICE_STATUS
    - used for detecting for instance when a device is not reporting for 
        too much time (it could mean something strange with the operator)

Here are the transactions to implement:

- get/set device settings
- get reference position for the device (it has been already set by the control unit)
- send calibration done -> obtain near positions (first calibration)
- get near positions 
- service status and last operation status available 

### positions collection and data integration 

(proposal, not implemented)

the positions connection is divided into two tables:

- STAGING : the table collects the direct export from the device
- QUALITY : the positions are collected, cleanned and made available to the
    other devices. A data integrator moves data from one table to another one

In this implementation, the data integrator is very simple, but in a real 
situation it will deal with a number of problems on the data:

- data skew : due to a not perfect first calibration, a certain amout of error
    affects the measurements
- geolocation association : for the devices not able to obtain directly latitude
    and longitude, these informations could be integrated by this module
- ... 

====================================================== */

-- DROP TABLE IF EXISTS sar.D_HL2_USER_DEVICE_SETTINGS;
-- CREATE TABLE sar.D_HL2_USER_DEVICE_SETTINGS (

--     DEVICE_ID CHAR(24) NOT NULL
--     , USER_ID CHAR(24) NOT NULL
--     , CONFIGURATION_PROFILE_ID INT NOT NULL
--         DEFAULT 0
--     , CONFIGURATION_PROFILE_DS VARCHAR(500)
--         DEFAULT ''

--     -- user setting overwrite
--     , USER_HEIGHT_VL FLOAT(15) NOT NULL
--         DEFAULT 1.85

--     -- positions daabase settings
--     , BASE_HEIGHT_VL FLOAT(15) NOT NULL
--         DEFAULT 0.8
--     , BASE_DISTANCE_VL FLOAT(15) NOT NULL
--         DEFAULT 0.5
--     , DISTANCE_TOLLERANCE_VL FLOAT NOT NULL
--         DEFAULT 0.1
    
--     -- dynamic sort: cluster size
--     , USER_CLUSTER_FL BOOLEAN NOT NULL
--         DEFAULT true
--     , CLUSTER_SIZE_VL INT NOT NULL
--         DEFAULT 25

--     -- dynamic sort: max indices
--     , USE_MAX_INDICES_FL BOOLEAN NOT NULL
--         DEFAULT true
--     , MAX_INDICES_VL INT NOT NULL
--         DEFAULT 10
    
--     -- logging settings
--     , LOG_LAYER_VL INT NOT NULL 
--         DEFAULT 1
    
--     -- other settings
--     , REFERENCE_POSITION_ID CHAR(24)
--         DEFAULT null
    
--     -- metadata
--     , CREATED_TS TIMESTAMP NOT NULL
--         DEFAULT CURRENT_TIMESTAMP
--     , UPDATED_TS TIMESTAMP NOT NULL
--         DEFAULT CURRENT_TIMESTAMP

--     , PRIMARY KEY ( DEVICE_ID, USER_ID, CONFIGURATION_PROFILE_ID )
-- );
-- COMMENT 
--     ON TABLE sar.D_HL2_USER_DEVICE_SETTINGS 
--     IS 'each HoloLens2 device must have custom settings loaded at startup by the device from server';
-- COMMENT 
--     ON COLUMN sar.D_HL2_USER_DEVICE_SETTINGS.REFERENCE_POSITION_ID 
--     IS 'a reference position for the calibration can be binded to a configuration profile for more flexibility of the device';
-- COMMENT 
--     ON COLUMN sar.D_HL2_USER_DEVICE_SETTINGS.CONFIGURATION_PROFILE_ID 
--     IS 'each user can have different config profiles';

DROP SEQUENCE IF EXISTS sar.F_HL2_STAGING_WAYPOINTS_SEQUENCE;
CREATE SEQUENCE sar.F_HL2_STAGING_WAYPOINTS_SEQUENCE
    AS BIGINT
    INCREMENT BY 1
    START WITH 1
;

DROP TABLE IF EXISTS sar.F_HL2_STAGING_WAYPOINTS;
CREATE TABLE sar.F_HL2_STAGING_WAYPOINTS (

    F_HL2_QUALITY_WAYPOINTS_PK BIGINT NOT NULL
        DEFAULT nextval('sar.F_HL2_STAGING_WAYPOINTS_SEQUENCE')
    -- , CURRENT_WAYPOINT_FK BIGINT
    --     DEFAULT NULL

    , DEVICE_ID CHAR(24) NOT NULL
    , SESSION_TOKEN_ID TEXT NOT NULL
    , SESSION_TOKEN_INHERITED_ID TEXT
        DEFAULT NULL

    -- position local identifier (inherited)
    , LOCAL_POSITION_ID INT NOT NULL
    -- local position identifier directly from the request
    , REQUEST_POSITION_ID INT NOT NULL
        DEFAULT -1

    -- area center
    , U_REFERENCE_POSITION_ID TEXT NOT NULL
    , U_LEFT_HANDED_REFERENCE_FL BOOLEAN NOT NULL
        DEFAULT true
    , UX_VL FLOAT(15) NOT NULL
    , UY_VL FLOAT(15) NOT NULL
    , UZ_VL FLOAT(15) NOT NULL
    , U_SOURCE_FROM_SERVER_FL BOOLEAN NOT NULL
        DEFAULT false
    
    -- measures
    , LOCAL_AREA_INDEX_ID INT NOT NULL
        DEFAULT 0
    -- , AREA_RADIUS_VL FLOAT(4) NOT NULL
    --     DEFAULT 0.5
    , WAYPOINT_CREATED_TS TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP
    
    -- alignment algorihm results
    , ALIGNMENT_ALIGNED_WITH_WAYPOINT_FK BIGINT
        DEFAULT NULL
    , ALIGNMENT_TYPE_FL BOOLEAN NOT NULL
        DEFAULT false
    , ALIGNMENT_QUALITY_VL FLOAT
        DEFAULT NULL
    , ALIGNMENT_DISTANCE_VL FLOAT
        DEFAULT NULL
    , ALIGNMENT_DISTANCE_FROM_WAYPOINT_FK FLOAT
        DEFAULT NULL
    
    -- metadata
    , CREATED_TS TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP
);
COMMENT 
    ON COLUMN sar.F_HL2_STAGING_WAYPOINTS.ALIGNMENT_ALIGNED_WITH_WAYPOINT_FK 
    IS 'reference to the aligned point if the point is redundant';
COMMENT 
    ON COLUMN sar.F_HL2_STAGING_WAYPOINTS.ALIGNMENT_TYPE_FL 
    IS 'true if the point has been found redundant';
COMMENT 
    ON COLUMN sar.F_HL2_STAGING_WAYPOINTS.ALIGNMENT_QUALITY_VL 
    IS 'when the algorithm classifies the point, it also gives a quality for that decision';
COMMENT 
    ON COLUMN sar.F_HL2_STAGING_WAYPOINTS.SESSION_TOKEN_INHERITED_ID 
    IS 'the token is used for inheriting sessions from previously defined ones. If null, the point is a first record, unexplored.';
COMMENT 
    ON COLUMN sar.F_HL2_STAGING_WAYPOINTS.LOCAL_POSITION_ID 
    IS 'LOCAL_POSITION_ID is a local identifier, generated by HoloLens2 to internally indexing positions';
COMMENT 
    ON COLUMN sar.F_HL2_STAGING_WAYPOINTS.U_LEFT_HANDED_REFERENCE_FL 
    IS 'Unity always works in a left handed reference coordinates system, so it is TRUE by default';
COMMENT 
    ON COLUMN sar.F_HL2_STAGING_WAYPOINTS.LOCAL_AREA_INDEX_ID 
    IS 'reference to table sar.F_HL2_STAGING_AREA_INDEX';

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
    
    -- area index support
    , AREA_INDEX_FK BIGINT NOT NULL
        DEFAULT 0

    -- relative
    , U_LEFT_HANDED_REFERENCE_FL BOOLEAN NOT NULL
        DEFAULT true
    , U_X FLOAT(15) NOT NULL
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
    , CREATED_TS TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP
    , DELETED_FL BOOLEAN NOT NULL
        DEFAULT false
    
    , PRIMARY KEY ( F_HL2_QUALITY_WAYPOINTS_PK )
);

DROP TABLE IF EXISTS sar.F_HL2_STAGING_PATHS;
CREATE TABLE sar.F_HL2_STAGING_PATHS (
    
    DEVICE_ID CHAR(24) NOT NULL
    , SESSION_TOKEN_ID TEXT NOT NULL
    , SESSION_TOKEN_INHERITED_ID TEXT
        DEFAULT NULL
    , U_REFERENCE_POSITION_ID TEXT NOT NULL

    , WAYPOINT_1_STAGING_FK INT
    , WAYPOINT_2_STAGING_FK INT
    , PATH_DISTANCE FLOAT
        DEFAULT NULL
    
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
    , CREATED_TS TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP
    , DELETED_FL BOOLEAN NOT NULL
        DEFAULT false
    
    , PRIMARY KEY ( F_HL2_QUALITY_WAYPOINTS_PK )
);

DROP SEQUENCE IF EXISTS sar.F_HL2_STAGING_AREA_INDEX_SEQUENCE;
CREATE SEQUENCE sar.F_HL2_STAGING_AREA_INDEX_SEQUENCE
    AS BIGINT
    INCREMENT BY 1
    START WITH 1
;

DROP TABLE IF EXISTS sar.F_HL2_STAGING_AREA_INDEX;
CREATE TABLE sar.F_HL2_STAGING_AREA_INDEX (

    F_HL2_STAGING_AREA_INDEX_PK BIGINT NOT NULL
        DEFAULT nextval('sar.F_HL2_STAGING_AREA_INDEX_SEQUENCE')
    
    , DEVICE_ID CHAR(24) NOT NULL
    , SESSION_TOKEN_ID TEXT NOT NULL

    , LOCAL_AREA_INDEX_INIT_ID INT NOT NULL
    , LOCAL_AREA_INDEX_FINAL_ID INT NOT NULL
    
    -- metadata
    , CREATED_TS TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP
);
COMMENT 
    ON TABLE sar.F_HL2_STAGING_AREA_INDEX
    IS 'it contains the area renamings from the device';
COMMENT 
    ON COLUMN sar.F_HL2_STAGING_AREA_INDEX.LOCAL_AREA_INDEX_INIT_ID
    IS 'the first area index assigned to waypoint by the device when a area change is detected';
COMMENT 
    ON COLUMN sar.F_HL2_STAGING_AREA_INDEX.LOCAL_AREA_INDEX_FINAL_ID
    IS 'the positon database can rename a area with a known index when a lonk between unknown area and known area is detected at runtime';

DROP SEQUENCE IF EXISTS sar.F_HL2_QUALITY_AREA_INDEX_SEQUENCE;
CREATE SEQUENCE sar.F_HL2_QUALITY_AREA_INDEX_SEQUENCE
    AS BIGINT
    INCREMENT BY 1
    START WITH 1
;

DROP TABLE IF EXISTS sar.F_HL2_QUALITY_AREA_INDEX;
CREATE TABLE sar.F_HL2_QUALITY_AREA_INDEX (

    F_HL2_QUALITY_AREA_INDEX_PK BIGINT NOT NULL
        DEFAULT nextval('sar.F_HL2_QUALITY_AREA_INDEX_SEQUENCE')

    , AREA_INDEX_FIRST_ID INT NOT NULL
    , AREA_INDEX_FINAL_ID INT NOT NULL
    
    -- metadata
    , CREATED_TS TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP
);

DROP TABLE IF EXISTS sar.D_HL2_REFERENCE_POSITIONS;
CREATE TABLE sar.D_HL2_REFERENCE_POSITIONS (

    REFERENCE_POSITION_ID CHAR(24) NOT NULL
    , REFERENCE_POSITION_DS VARCHAR(500)
        DEFAULT NULL

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
    , Q_W FLOAT(15) NOT NULL
        DEFAULT 0.0
    
    -- metadata
    , CREATED_TS TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP

    , PRIMARY KEY ( REFERENCE_FROM_ID, REFERENCE_TO_ID )
);

DROP TABLE IF EXISTS sar.F_HL2_SERVICE_STATUS;
CREATE TABLE sar.F_HL2_SERVICE_STATUS (

    DEVICE_ID CHAR(24) NOT NULL
    , SESSION_TOKEN_ID TEXT NOT NULL
    
    -- device status
    , DEVICE_STATUS_DS VARCHAR(100) 
        DEFAULT NULL
    , DEVICE_STATUS_TS TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP

    -- metadata
    , CREATED_TS TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP
    
    , PRIMARY KEY ( DEVICE_ID, SESSION_TOKEN_ID, DEVICE_STATUS_TS )
);










/* ======================================================

## IoT data integration

in this project, sensory data are integrated by a data integrator asking
data to a external server. The same approach can be used for every other
passive device.

The data model is intentionally simple for the data integrators:

- a table storing all the measurements, updated in append -> F_IOT_MEASUREMENTS

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
    , CREATED_TS TIMESTAMP NOT NULL
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

- CREATED_TS TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
- DELETED_FL BOOLEAN NOT NULL DEFAULT false

```sql
-- metadata
, CREATED_TS TIMESTAMP NOT NULL
    DEFAULT CURRENT_TIMESTAMP
, DELETED_FL BOOLEAN NOT NULL
    DEFAULT false
```

====================================================== */