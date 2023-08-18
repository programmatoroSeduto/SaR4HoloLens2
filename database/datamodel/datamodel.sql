
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
    , USER_ACCESS_CODE_ID CHAR()

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

    F_ACTIVITY_LOG_PK CHAR(8) NOT NULL
        DEFAULT LPAD(CAST(nextval('sar.F_ACTIVITY_LOG_SEQUENCE') AS TEXT), 8, '0')
    
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

## TRANSACTION (get-device) : DEVICE assign to user

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
    - if the user exists
    - if the user is active
    - if the session ID is still valid
- (CHECK) device
    - if the device is holdable -> DEVICE_IS_HOLDABLE_FL
    - if the user is allowed to hold the device
- (UPDATE) device session data

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