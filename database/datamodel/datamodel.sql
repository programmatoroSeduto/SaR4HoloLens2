
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
- device : SARHL2_ID00000000_DEV
- reference position : SARHL2_ID00000000_REFPOS

conventions:

- each ID starts with SARHL2_
- the ID part has prefix 'ID' and has 8 digits after that
- the end of the name is the ype of entity

Each entity is stored into the 'sar' datamodel, and they
are assigned *authorizations* to them as flags. 

## Special codes

- the codes with ID00000000 are technical codes, not to use.
- the system considers some special positions:
    - pcDevPosID = "SARHL2_ID90909091_REFPOS"
    - pcDevCalibPosID = "SARHL2_ID06600660_REFPOS"
    - deviceCalibPosID = "SARHL2_ID12700385_REFPOS"
    - deviceNoCalibPosID = "SARHL2_66660000_REFPOS"
    - prodDevicePosID = "SARHL2_ID00000000_REFPOS"

In particular the last one, 'SARHL2_ID00000000_REFPOS', is
used by active devices to ask their reference position. 

====================================================== */

DROP SCHEMA IF EXISTS SAR CASCADE;
CREATE SCHEMA sar;










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

## TRANSACTION (login) : USER activity begins

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

## TRANSACTION (logout) : USER activity ends

API:
    api/access/logout
    - REQUEST:
        - user id
        - session token
    - RESPONSE:
        - empty

the request is sent by the control unit. Only the name of the user is
required here. 

- CHECK 
    - (D_USER) the user code exists
    - (F_USER_ACTIVITY) the token is correct
- UPDATE logout
    - (F_USER_ACTIVITY) close the row
    - (F_ACTIVITY_LOG) log transaction
    - check and close devices associated to the user

## Trapdoor Requests

The more infos you have about how a user can and cannot do, the more
the system can react to a malicious scenario if any. 

In this data model, there are situations in which the type of access
is uncompatible with how it is known that the entity should behave. You
can perform a request as admin, for instance, when you're not a admin. 
given how the request is meant, this type of wrong access it's likely
to be malicious: I put user_id=user_approver_id into the request, this
is not a common mistake. In this case, the request acts as a "trapdoor 
request", allowing to identify a potentially dangerous situation. 

There are situations in which sometimes we could be sure that the 
provided request is suspicious or potentially dangerous. 

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
    , USER_ID_ADMIN_FL BOOLEAN NOT NULL
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
    , USER_CREATION_TS TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP
    , USER_UPDATE_TS TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP
    , USER_DELETED_FL BOOLEAN NOT NULL
        DEFAULT false
    , USER_DELETED_TS TIMESTAMP
        DEFAULT NULL
    
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

)

DROP TABLE IF EXISTS sar.F_USER_ACTIVITY;
CREATE TABLE sar.F_USER_ACTIVITY (
    
    USER_ID CHAR(24) NOT NULL
    , USER_APPROVER_ID CHAR(24) NOT NULL
    
    -- operation timestamps
    , USER_START_AT_TS TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP
    , USER_END_AT_TS TIMESTAMP 

    -- session data
    , USER_ACCESS_TOKEN_ID TEXT NOT NULL

    , PRIMARY KEY ( USER_ID, START_AT_TS )

);










/* ======================================================

## log table

This table allows the contro center to check the requrests made upon
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

    F_ACTIVITY_LOG_PK INT NOT NULL
        DEFAULT nextval('sar.F_ACTIVITY_LOG_SEQUENCE')
    
    , LOG_CREATION_TS TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP
    
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

    , PRIMARY KEY ( F_ACTIVITY_LOG_PK )

);










/* ======================================================

## devices

once the user obtained the access to the database, it can be
assigned a device to their. References to the device item is
stored in the table D_DEVICES, with their type and which user
is holding the resource.

### Device IDs

devices follow this naming convention:

- SARHL2_ID00000000_DEV
- prefix: SARHL2
- ID followed by a 8-digit identifier
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

## TRANSACTION (access) : DEVICE assign to user

API: api/device/login
    - REQUEST:
        - user id
        - device id
        - user session token
    - RESPONSE:
        - OK or not OK ...?

The request is performed directly by the user, once enabled a
session token. Here are the steps:

- (CHECK) user
    - (D_USER) if the user exists
    - (F_USER_ACTIVITY) if the user is active
    - (F_USER_ACTIVITY) if the session ID is still valid
- (CHECK) device
    - (D_DEVICE) if the device exists
    - (D_DEVICE) if the device is holdable -> DEVICE_IS_HOLDABLE_FL
    - (L_DEVICE_USER) if the user is allowed to hold the device
    - (F_DEVICE_ACTIVITY) if the device is not hold by another user
- (UPDATE) device session data
    - (F_DEVICE_ACTIVITY) open a new row with the session ID
    - (F_ACTIVITY_LOG) register the activity

## TRANSACTION (release) : DEVICE release

API: api/device/logout
    - REQUEST:
        - user id
        - device id
        - user session token
    - RESPONSE:
        - OK or not OK ...?

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

    -- device metadata
    , DEVICE_CREATION_TS TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP
    , DEVICE_DELETED_FL BOOL NOT NULL
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

    L_DEVICE_USER_PK CHAR(8) NOT NULL
        DEFAULT LPAD(CAST(nextval('sar.L_DEVICE_USER_SEQUENCE') AS TEXT), 8, '0')

    , DEVICE_ID CHAR(24) NOT NULL
    , USER_ID CHAR(24) NOT NULL

    -- relation metadata
    , CREATED_TS TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP
    , DELETED_FL BOOLEAN NOT NULL
        DEFAULT false

    , PRIMARY KEY ( L_DEVICE_USER_PK )
);

DROP TABLE IF EXISTS sar.F_DEVICE_ACTIVITY;
CREATE TABLE sar.F_DEVICE_ACTIVITY (

    DEVICE_ID CHAR(24) NOT NULL
    , USER_ACCESS_TOKEN_ID TEXT NOT NULL
    
    -- operation timestamps
    , DEVICE_ON_AT_TS TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP
    , DEVICE_OFF_AT_TS TIMESTAMP 

    , PRIMARY KEY ( DEVICE_ID, USER_ACCESS_TOKEN_ID )

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
    , USER_ACCESS_TOKEN_ID TEXT NOT NULL

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
        DEFAUL 0.5
    
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

    F_HL2_QUALITY_WAYPOINTS_PK INT NOT NULL
        DEFAULT nextval('F_HL2_QUALITY_WAYPOINTS_SEQUENCE')

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
    
    , CREATED_TS TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP
);

DROP TABLE IF EXISTS sar.F_HL2_STAGING_PATHS;
CREATE TABLE sar.F_HL2_STAGING_PATHS (
    
    DEVICE_ID CHAR(24) NOT NULL
    , USER_ACCESS_TOKEN_ID TEXT NOT NULL

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
CREATE PATH sar.F_HL2_QUALITY_PATHS (

    F_HL2_QUALITY_WAYPOINTS_PK INT NOT NULL
        DEFAULT nextval('F_HL2_QUALITY_PATHS_SEQUENCE')

    -- SOURCE : the first user/device which found the position
    , SOURCE_USER_ID CHAR(24) NOT NULL
    , SOURCE_DEVICE_ID CHAR(24) NOT NULL

    -- metadata
    , CREATED_TS TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP
    
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
    , USER_ACCESS_TOKEN_ID TEXT NOT NULL
    
    -- device status
    , DEVICE_STATUS_DS VARCHAR(100) 
        DEFAULT NULL
    , DEVICE_STATUS_TS TIMESTAMP NOT NULL
        DEFAULT CURRENT_TIMESTAMP
    
    PRIMARY KEY ( DEVICE_ID, USER_ACCESS_TOKEN_ID, DEVICE_STATUS_TS )
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
    , USER_ACCESS_TOKEN_ID TEXT NOT NULL

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
    , DEVICE_STATUS_TS TIMESTAMP NOT NULL
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
    - USER_ACCESS_TOKEN_ID TEXT NOT NULL
        - hashed

Metadata:

- CREATED_DT TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
- DELETED_FL BOOLEAN NOT NULL DEFAULT false

User Metadata:

- USER_CREATION_TS TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
    - the time the record is created for the first time
- USER_UPDATE_TS TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
    - the time the record as been created / updated (never null)
- USER_DELETED_FL BOOLEAN NOT NULL DEFAULT false
    - if the record has been deleted or not
- USER_DELETED_TS TIMESTAMP DEFAULT NULL
    - when the user has been deleted

Final reviews:

- BOOL instead of BOOLEAN
- numerical precision adn suffixes
- sequences always used as common INT fields
- level out all the metadata (classify tables and SCD techniques)
- final list of transactions and sources

====================================================== */