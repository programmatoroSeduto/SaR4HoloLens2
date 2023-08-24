
/* ======================================================

# The SAR PROJECT : The Download Upload Protocol

## Motivations

This protocol allows the devices and the server to work with
positions measurements directly in staging area, having in
mind there requirements:

- create as less new positions as possible across sessions
- allow session sharing and collaboration
- make easier the work of a data processor in charge to move
	points from staging area to the quality area

The analysis will proceed using a scenario in which the stagin
area is created step by step from scratch. 

====================================================== */









/* ======================================================

## Testing Area

====================================================== */

-- PERFORM statement
-- https://www.postgresql.org/docs/current/plpgsql-statements.html#PLPGSQL-STATEMENTS-SQL-NORESULT
CREATE OR REPLACE FUNCTION do_nothing()
	RETURNS void
	LANGUAGE plpgsql
AS $$ BEGIN
	SELECT 1;
END $$;
-- EXECUTE(do_nothing()); -- not working
-- PERFORM do_nothing(); -- NOT WORKING
DROP FUNCTION do_nothing;

-- user ID from function
SELECT sar_user_id(7864861468);

-- parse JSON to Postgres table
-- https://stackoverflow.com/questions/25785575/how-to-parse-json-using-json-populate-recordset-in-postgres
CREATE TYPE json_schema AS (
	id int, 
	description varchar(100)
);
WITH raw_data AS (
SELECT * 
FROM JSON_POPULATE_RECORDSET(null::json_schema, 
'[
	{"id":8268427,"description":"ciao"},
	{"id":86426222,"description":"hi"},
	{"id":666},
	{"description" : "hola"}
]')
)
SELECT 
	COALESCE(id, -1) AS id,
	COALESCE(description, 'MISSING DESCRIPTION') AS description
FROM raw_data;
DROP TYPE json_schema;









/* ======================================================

## Custom Functions Area

====================================================== */

-- create function to register a user with its device
CREATE OR REPLACE FUNCTION user_device_login( user_id CHAR(24), approver_id CHAR(24), device_id CHAR(24) )
	RETURNS TEXT 
	LANGUAGE plpgsql
AS $$ 
DECLARE 
	session_token_id TEXT;
BEGIN
	WITH create_token AS (
		INSERT INTO sar.F_USER_ACTIVITY (
		    USER_ID, 
		    USER_APPROVER_ID,
		    USER_SESSION_TOKEN_ID
		)
		VALUES (
		    user_id,
		    approver_id,
		
		    MD5( CONCAT(
		        FLOOR(RANDOM() * 1000000), 
		        user_id,
		        FLOOR(RANDOM() * 1000000), 
		        approver_id,
		        FLOOR(RANDOM() * 1000000)
		    ) )
		)
		RETURNING
		    USER_SESSION_TOKEN_ID
	)
	SELECT USER_SESSION_TOKEN_ID
	INTO session_token_id
	FROM create_token;
	
	INSERT INTO sar.F_DEVICE_ACTIVITY (
	    DEVICE_ID, USER_SESSION_TOKEN_ID
	) VALUES (
	    device_id, session_token_id
	);
	
	RETURN session_token_id;
END $$;

-- a function to logout each user
CREATE OR REPLACE FUNCTION user_device_logout( )
	RETURNS BOOLEAN
	LANGUAGE plpgsql
AS $$ BEGIN
	UPDATE sar.F_DEVICE_ACTIVITY
	SET 
	    DEVICE_OFF_AT_TS = CURRENT_TIMESTAMP
	WHERE 
	    DEVICE_OFF_AT_TS IS NULL;
	
	UPDATE sar.F_USER_ACTIVITY
	SET
	    USER_END_AT_TS = CURRENT_TIMESTAMP
	WHERE 
		USER_END_AT_TS IS NULL;

	RETURN TRUE;
END $$ ;

-- get session token from tables (if the session is opened)
CREATE OR REPLACE FUNCTION get_session_of( arg_user_id TEXT, arg_device_id TEXT )
	RETURNS TEXT
	LANGUAGE plpgsql
AS $$ 
DECLARE 
	return_session_token_id TEXT DEFAULT NULL::TEXT;
BEGIN
	SELECT 
		dev.USER_SESSION_TOKEN_ID
	FROM sar.F_USER_ACTIVITY
		AS usr
	LEFT JOIN sar.F_DEVICE_ACTIVITY
		AS dev
		ON ( usr.USER_SESSION_TOKEN_ID = dev.USER_SESSION_TOKEN_ID )
	INTO return_session_token_id
	WHERE usr.USER_ID = arg_user_id
	AND dev.DEVICE_ID = arg_device_id;
	
	RETURN return_session_token_id;
END $$ ;











/* ======================================================

## users login

there are two users using HoloLens2, eventually at the same time:

- USER: SARHL2_ID2894646521_USER -> HL2: SARHL2_ID0931557300_DEVC
- USER: SARHL2_ID8849249249_USER -> HL2: SARHL2_ID8651165355_DEVC

Each user is correctly logged in. 

====================================================== */

-- select USER
SELECT user_id FROM sar.D_USER 
WHERE NOT(DELETED_FL) AND USER_ADMIN_FL;
-- SARHL2_ID8849249249_USER

-- users
SELECT * FROM SAR.d_user 
WHERE USER_ID IN ( 'SARHL2_ID8849249249_USER', 'SARHL2_ID2894646521_USER' );

-- select DEVICEs
SELECT ldu.USER_ID, dev.device_id FROM sar.D_DEVICE dev LEFT JOIN sar.L_DEVICE_USER ldu ON ( dev.DEVICE_ID = ldu.DEVICE_ID )
WHERE NOT(dev.DELETED_FL) AND dev.DEVICE_TYPE_DS='Microsoft HoloLens2'
AND ldu.USER_ID IN ( 'SARHL2_ID8849249249_USER', 'SARHL2_ID2894646521_USER' )
ORDER BY 1, 2;

-- just to be sure, close all the previously opened sessions
SELECT user_device_logout();

-- let's open first session
-- USER 1: SARHL2_ID2894646521_USER -> HL2: SARHL2_ID0931557300_DEVC
SELECT user_device_login( 'SARHL2_ID2894646521_USER', 'SARHL2_ID2894646521_USER', 'SARHL2_ID0931557300_DEVC' );
SELECT get_session_of( 'SARHL2_ID2894646521_USER', 'SARHL2_ID0931557300_DEVC' );
-- 71fe96d81a11a32a39ba410d812181ad

-- let's open second session
-- USER 2: SARHL2_ID8849249249_USER -> HL2: SARHL2_ID8651165355_DEVC
SELECT user_device_login( 'SARHL2_ID8849249249_USER', 'SARHL2_ID8849249249_USER', 'SARHL2_ID8651165355_DEVC' );
SELECT get_session_of( 'SARHL2_ID8849249249_USER', 'SARHL2_ID8651165355_DEVC' );
-- 8e55b08c1c9270cfcd6cc45c68f7f7af

-- check the USER sessions opened
SELECT * FROM sar.F_USER_ACTIVITY WHERE USER_END_AT_TS IS NULL;

-- check the DEVICE sessions opened
SELECT * FROM sar.F_DEVICE_ACTIVITY WHERE DEVICE_OFF_AT_TS IS NULL;

-- reference point -> 'SARHL2_ID1234567890_REFP'
SELECT * FROM sar.D_HL2_REFERENCE_POSITIONS;












/* ======================================================

## first download/upload

Assumptions:

- user and device are correctly logged in
- the quality tables are empty
- the staging tables are empty
- only a set of reference points is given
- the device knows in advance its reference point for calibration
- connectivity is quite good, hence the HL2 device can update frequently the server
	- it results in many requests with small payload
- ignore quality
	- I expect to integrate qaulity with a small update upon this algorihm

How the scenario proceeds:

- (HL2) performs calibration
- (HL2) after calibration, download is called
- (SERVER) ... check quality ... (ignore now)
- (SERVER) try to find a previously defined session
	- no session returned from this query, te table is empty
- (SERVER) register zero-pos as original, not inherited
	- ID 0 is aligned between server and device
- (SERVER) returns empty

- (HL2) ... exploring area ...

- (HL2) time to update: upload request sent
- (SERVER) checks if that session exists
	- found the device session
- (SERVER) check if the session is inherited by another one
	- also by checking out the JSON request, 
	- the field based_on is empty: original request
- (SERVER) insert points in staging area
- (SERVER) generate and send alignment

### REQUEST example (upload)

```json
{
	based_on : '',
	reference_pos : 'SARHL2_ID1234567890_REFP',
	with_areas : false,
	... waypoints ...
	... paths ...
	... area renamings ...
}
```

### RESPONSE example (upload)

- the field 'aligned' allows to keep aligned local IDs on the device with server IDs
- server IDs are the one valid IDs: HL2 adapts infos based on alignment
- the alignment also indicates which waypoints have been loaded

{
	status : '',
	status_defaul : '',
	max_id : ...,
	alignment : [
		{ local_id : 0, server_id : ' }, 
		...
	]
}

### How to manage area renamings

the device just sends areas with 0 if the flag 'with_areas' is false.
When it is true, the server also expects new zones, but this option
is used only immediately after the device logs out. 

====================================================== */

-- (api:DOWNLOAD)
/* DOWNLOAD REQUEST
```json
{
	-- inherited from base hl2 class
	user_id : 'SARHL2_ID2894646521_USER',
	device_id : 'SARHL2_ID0931557300_DEVC',
	session_token : '71fe96d81a11a32a39ba410d812181ad',
	
	-- request data
	base_on : '', -- empty at first
	reference-pos : 'SARHL2_ID1234567890_REFP'
	current_pos : [x,y,z], -- unused
	radius : 500.0f, -- unused
}
```
*/

-- check table status before request (table is empty now)
SELECT * FROM sar.F_HL2_STAGING_WAYPOINTS;

-- does the session exist in staging? 
SELECT 
(COUNT(*) > 0)::BOOLEAN AS SESSION_DEFINED_IN_STAGING_FL
FROM sar.F_HL2_STAGING_WAYPOINTS
WHERE SESSION_TOKEN_ID = get_session_of( 'SARHL2_ID2894646521_USER', 'SARHL2_ID0931557300_DEVC' );

-- find data from previously available sessions (query is empty now)
SELECT DISTINCT 
SESSION_TOKEN_ID, 
SESSION_TOKEN_INHERITED_ID, -- IF it IS NULL, the SESSION IS original
CREATED_TS
FROM sar.F_HL2_STAGING_WAYPOINTS
WHERE 1=1
	AND LOCAL_AREA_INDEX_ID = 0 
	AND U_REFERENCE_POSITION_ID = 'SARHL2_ID1234567890_REFP'
ORDER BY 
	CREATED_TS DESC
LIMIT 2;
-- 'AND LOCAL_AREA_INDEX_ID = 0' : 
-- 		in this case, the query does not return any USER1 info

-- currently there are no available sessions
-- this session is new for sure, return the reference position ID
-- USER 1: SARHL2_ID2894646521_USER -> HOLOLENS2 DEVICE 1: SARHL2_ID0931557300_DEVC
INSERT INTO sar.F_HL2_STAGING_WAYPOINTS (
	DEVICE_ID,
	SESSION_TOKEN_ID, 
	-- SESSION_TOKEN_INHERITED_ID is NULL by default
	U_REFERENCE_POSITION_ID, U_SOURCE_FROM_SERVER_FL,
	UX_VL, UY_VL, UZ_VL,
	LOCAL_POSITION_ID, 
	LOCAL_AREA_INDEX_ID
	-- AREA_RADIUS_VL ???
) VALUES (
	'SARHL2_ID0931557300_DEVC',
	get_session_of( 'SARHL2_ID2894646521_USER', 'SARHL2_ID0931557300_DEVC' ),
	'SARHL2_ID1234567890_REFP', TRUE,
	0.00, 0.00, 0.00, 
	0, -- FIRST LOCAL positions IS ALWAYS zero since the device IS IN calibration point
	0  -- FIRST area_index IS ALWAYS zero
)
RETURNING 
	*;

-- check insert result
SELECT * FROM sar.F_HL2_STAGING_WAYPOINTS;

-- no paths to insert 
SELECT * FROM sar.F_HL2_STAGING_PATHS;

-- area index must be registerd for the session
INSERT INTO sar.F_HL2_STAGING_AREA_INDEX (
	DEVICE_ID,
	SESSION_TOKEN_ID, -- please change with SESSION_TOKEN_ID! 
	LOCAL_AREA_INDEX_INIT_ID, LOCAL_AREA_INDEX_FINAL_ID
) VALUES (
	'SARHL2_ID0931557300_DEVC',
	'6d6c431788c340e9589139f65a7e3914',
	0, 0
)
RETURNING
	*;

-- check the insert 
SELECT * FROM sar.F_HL2_STAGING_AREA_INDEX;

/* DOWNLOAD RESPONSE

- 'base_on' is empty : the session is not inherited. 

```json
{
	-- inherited from base
	status : 200,
	status_detail : 'sessione enabled; first session found',
	
	-- response content for download
	based_on : '',
	max_id : 0,
	waypoints_alignment : {}
}
```
*/

/* ======================================================

### First download -- logics so far

- (HL2) DOWNLOAD from device
	- immediately afer calibration
	- the database is completely empty given
		- reference point
- (SERVER) get infos
	- get sessions ordered from the most recent one
	- is the asking session currently active?
- is the session active also in staging? --> NO
	- add the reference point to the DB
	- create area index 0
- (SERVER) is there another session enabled? --> NO
	- (nothing to do: the information already inserted has NULL as inherited session)
- return
	- max_id : 0
	- waypoints_alignment : empty

====================================================== */

-- (api:UPLOAD)
/* UPLOAD REQUEST

- the client sends only new positions 
- hence, '0' is not sent in this case since it is assumed to be in sync with server
- neither zones with area ID different from 0 are sent (simplification)

```json
{
	-- inherited from base hl2 class
	"user_id" : "SARHL2_ID2894646521_USER",
	"device_id" : "SARHL2_ID0931557300_DEVC",
	"session_token" : "71fe96d81a11a32a39ba410d812181ad",
	
	-- request data
	"base_on" : "",
	"ref_id" : "SARHL2_ID1234567890_REFP",
	"radius" : 0.8,
	"waypoints" : [
		{
			"pos_id" : 1,
			"area_id" : 0,
			"v" : [ 0, 0, 1 ],
			"tstmp" : "2023/08/23, 00:00:02"
		},
		{
			"pos_id" : 2,
			"area_id" : 0,
			"v" : [ 0, 0, 2 ],
			"tstmp" : "2023/08/23, 00:00:03"
		},
		{
			"pos_id" : 3,
			"area_id" : 0,
			"v" : [ 0, 0, 3 ],
			"tstmp" : "2023/08/23, 00:00:05"
		},
		{
			"pos_id" : 4,
			"area_id" : 0,
			"v" : [ 1, 0, 0 ],
			"tstmp" : "2023/08/23, 00:00:10"
		}
	],
	"paths" : [
		{"wp1" : 0, "wp1" : 1, "tstmp" : "2023/08/23, 00:00:02"},
		{"wp1" : 1, "wp1" : 2, "tstmp" : "2023/08/23, 00:00:03"},
		{"wp1" : 2, "wp1" : 3, "tstmp" : "2023/08/23, 00:00:05"}
	],
	"area_renamings" : {
		0 : 0
	}
}
```
*/

-- table inspection 
SELECT * FROM sar.F_HL2_STAGING_WAYPOINTS;

-- does the session exist in staging? --> YES
SELECT 
(COUNT(*) > 0)::BOOLEAN AS SESSION_DEFINED_IN_STAGING_FL
FROM sar.F_HL2_STAGING_WAYPOINTS
WHERE SESSION_TOKEN_ID = get_session_of( 'SARHL2_ID2894646521_USER', 'SARHL2_ID0931557300_DEVC' );

-- the session is not inherited by anther one based_on is empty)
-- to be sure, check into the database (why doesn't the request report the session ID??? strange, and possibly dangerous)
SELECT DISTINCT 
SESSION_TOKEN_ID, 
SESSION_TOKEN_INHERITED_ID,
CREATED_TS
FROM sar.F_HL2_STAGING_WAYPOINTS
WHERE 1=1
	AND LOCAL_AREA_INDEX_ID = 0 
	AND U_REFERENCE_POSITION_ID = 'SARHL2_ID1234567890_REFP'
	AND SESSION_TOKEN_ID = get_session_of( 'SARHL2_ID2894646521_USER', 'SARHL2_ID0931557300_DEVC' )
	AND SESSION_TOKEN_INHERITED_ID IS NULL;
-- the session exists and it is not inherited, yuppie!

-- get MAX of session ID (earchon inherited if the session is inherited)
SELECT 
MAX(LOCAL_POSITION_ID) AS MAX_LOCAL_POSITION_ID 
FROM sar.F_HL2_STAGING_WAYPOINTS
WHERE SESSION_TOKEN_ID = get_session_of( 'SARHL2_ID2894646521_USER', 'SARHL2_ID0931557300_DEVC' );
-- in the general case, the session ID should be the inherited one

-- ALIGNMENT ALGORITHM
-- in this example, the waypoint is compared with only one point, but it gives the idea
-- load JSON structure in one query
CREATE TYPE json_schema AS (
	pos_id int,
	area_id int,
	v vector(3)
	-- tstmp VARCHAR(24)
);

DROP TABLE IF EXISTS sar.D_ALIGNMENT_ALGORITHM_DATA_SOURCE;
CREATE TABLE sar.D_ALIGNMENT_ALGORITHM_DATA_SOURCE AS

WITH request_data AS (
SELECT 
	pos_id,
	area_id,
	v
	-- tstmp
FROM (SELECT * FROM JSON_POPULATE_RECORDSET(NULL::json_schema,
'[
	{
		"pos_id" : 1,
		"area_id" : 0,
		"v" : [ 0, 0, 1.9 ],
		"tstmp" : "2023/08/23, 00:00:02"
	},
	{
		"pos_id" : 2,
		"area_id" : 0,
		"v" : [ 0, 0, 4.05 ],
		"tstmp" : "2023/08/23, 00:00:03"
	},
	{
		"pos_id" : 3,
		"area_id" : 0,
		"v" : [ 0, 0, 7.97 ],
		"tstmp" : "2023/08/23, 00:00:05"
	},
	{
		"pos_id" : 4,
		"area_id" : 0,
		"v" : [ 2.05, 0, 0 ],
		"tstmp" : "2023/08/23, 00:00:10"
	}
]')
) AS json_raw_data )
, session_data AS (
SELECT 
	F_HL2_QUALITY_WAYPOINTS_PK AS align_with_fk,
	LOCAL_POSITION_ID AS pos_id,
	LOCAL_AREA_INDEX_ID AS area_id,
	to_vector3( UX_VL, UY_VL, UZ_VL )::vector(3) AS v
	-- tmstp
FROM sar.F_HL2_STAGING_WAYPOINTS
)
, cross_data AS (
SELECT 
	request_data.pos_id AS req_pos_id,
	session_data.pos_id AS loc_pos_id,
	session_data.align_with_fk AS align_with_fk,
	request_data.area_id AS req_area_id,
	session_data.area_id AS loc_area_id,
	( request_data.v <-> session_data.v ) AS dist
FROM request_data CROSS JOIN session_data
LEFT JOIN ( SELECT 1.0::FLOAT AS threshold_vl ) AS th ON (1=1)
)
, analysis_data AS (
SELECT 
	req_pos_id,
	loc_pos_id,
	align_with_fk,
	req_area_id,
	loc_area_id,
	cross_data.dist AS dist,
	CASE 
		WHEN cross_data.dist <= th.threshold_vl - th.toll THEN TRUE
		WHEN cross_data.dist >= th.threshold_vl + th.toll THEN FALSE 
		ELSE (RANDOM()>0.5)::BOOLEAN
	END AS WP_IS_REDUNDANT_FL,
	CASE 
		WHEN cross_data.dist <= th.threshold_vl - th.toll 
			THEN ROUND(max_of( 
				((th.threshold_vl - cross_data.dist) / th.threshold_vl)::NUMERIC, 
				0::NUMERIC ) * 100, 2)
		WHEN cross_data.dist >= th.threshold_vl + th.toll
			THEN ROUND(max_of( 
				(1 - 1.00*exp( 1.0*(th.threshold_vl - cross_data.dist) ))::NUMERIC,
				0::NUMERIC ) * 100, 2)
		ELSE 0.001
	END AS QUALITY_VL
FROM cross_data
LEFT JOIN ( SELECT 
	1.0::FLOAT AS threshold_vl,
	0.01::FLOAT AS toll 
) AS th ON (1=1)
)
SELECT DISTINCT
	req_pos_id,
	FIRST_VALUE(align_with_fk)
		OVER ( PARTITION BY req_pos_id ORDER BY dist ASC )
		AS align_with_fk,
	FIRST_VALUE(dist)
		OVER ( PARTITION BY req_pos_id ORDER BY dist ASC )
		AS dist,
	FIRST_VALUE(WP_IS_REDUNDANT_FL) 
		OVER ( PARTITION BY req_pos_id ORDER BY dist ASC )
		AS WP_IS_REDUNDANT_FL,
	FIRST_VALUE(QUALITY_VL)
		OVER ( PARTITION BY req_pos_id ORDER BY dist ASC )
		AS QUALITY_VL
FROM analysis_data
ORDER BY
	req_pos_id, dist;
-- JOIN alternative positions using result of FIRST_VALUE(align_with_fk)

DROP TYPE json_schema;
-- WP_IS_REDUNDANT_FL is true : this point is aligned with anoher one already present in the table

SELECT * FROM sar.D_ALIGNMENT_ALGORITHM_DATA_SOURCE
ORDER BY 1, 2;

-- let's imagine to use the table for each waypoint
-- WAYPOINT WITH LOCAL ID 1
SELECT
	req_pos_id, WP_IS_REDUNDANT_FL, QUALITY_VL, DIST
FROM sar.D_ALIGNMENT_ALGORITHM_DATA_SOURCE;
-- is IT redundant? --> NO

-- print true before false
-- SELECT * FROM ( SELECT TRUE AS col UNION SELECT FALSE AS col ) AS q ORDER BY col DESC;

-- (in this case) no point is redundant
-- insert all of them (directly using JSON format!)
CREATE TYPE json_schema AS (
	pos_id int,
	area_id int,
	v vector(3),
	tstmp VARCHAR(24)
);
INSERT INTO sar.F_HL2_STAGING_WAYPOINTS (
	DEVICE_ID,
	U_REFERENCE_POSITION_ID, 
	SESSION_TOKEN_ID,
	LOCAL_POSITION_ID, 
	UX_VL,
	UY_VL,
	UZ_VL,
	LOCAL_AREA_INDEX_ID,
	AREA_RADIUS_VL,
	ALIGNMENT_QUALITY_VL,
	CREATED_TS
)
SELECT 
	'SARHL2_ID0931557300_DEVC' AS DEVICE_ID,
	'SARHL2_ID1234567890_REFP' AS U_REFERENCE_POSITION_ID,
	get_session_of( 'SARHL2_ID2894646521_USER', 'SARHL2_ID0931557300_DEVC' ) AS SESSION_TOKEN_ID,
	pos_id AS LOCAL_POSITION_ID,
	component_of(v, 1) AS UX_VL,
	component_of(v, 2) AS UY_VL,
	component_of(v, 3) AS UZ_VL,
	area_id AS LOCAL_AREA_INDEX_ID,
	1.0 AS AREA_RADIUS_VL,
	alignment_algorithm.QUALITY_VL AS ALIGNMENT_QUALITY_VL,
	TO_TIMESTAMP(tstmp, 'YYYY-MM-DD HH:MI:SS') AS CREATED_TS
FROM (SELECT * FROM JSON_POPULATE_RECORDSET(NULL::json_schema,
'[
	{
		"pos_id" : 1,
		"area_id" : 0,
		"v" : [ 0, 0, 1.9 ],
		"tstmp" : "2023/08/23, 12:00:02"
	},
	{
		"pos_id" : 2,
		"area_id" : 0,
		"v" : [ 0, 0, 4.05 ],
		"tstmp" : "2023/08/23, 12:00:03"
	},
	{
		"pos_id" : 3,
		"area_id" : 0,
		"v" : [ 0, 0, 7.97 ],
		"tstmp" : "2023/08/23, 12:00:05"
	},
	{
		"pos_id" : 4,
		"area_id" : 0,
		"v" : [ 2.05, 0, 0 ],
		"tstmp" : "2023/08/23, 12:00:10"
	}
]')
) AS json_raw_data
LEFT JOIN sar.D_ALIGNMENT_ALGORITHM_DATA_SOURCE
	AS alignment_algorithm
	ON ( json_raw_data.pos_id = alignment_algorithm.req_pos_id )
RETURNING
	*;

DROP TYPE json_schema;
SELECT * FROM sar.F_HL2_STAGING_WAYPOINTS;

-- PATHS : just rename and load inside the same session (not the inherited one)
INSERT INTO sar.F_HL2_STAGING_PATHS (
	DEVICE_ID,
	SESSION_TOKEN_ID,
	
	LOCAL_WAYPOINT_1_ID,
	LOCAL_WAYPOINT_2_ID
) VALUES
(
	'SARHL2_ID8651165355_DEVC',
	get_session_of( 'SARHL2_ID8849249249_USER', 'SARHL2_ID8651165355_DEVC' ),
	0, 1
),

(
	'SARHL2_ID8651165355_DEVC',
	get_session_of( 'SARHL2_ID8849249249_USER', 'SARHL2_ID8651165355_DEVC' ),
	1, 2
),
(
	'SARHL2_ID8651165355_DEVC',
	get_session_of( 'SARHL2_ID8849249249_USER', 'SARHL2_ID8651165355_DEVC' ),
	2, 3
),
(
	'SARHL2_ID8651165355_DEVC',
	get_session_of( 'SARHL2_ID8849249249_USER', 'SARHL2_ID8651165355_DEVC' ),
	0, 4
)
RETURNING
	*;

-- AREA RENAMINGS : ...? ignore it, pls

























/* ======================================================

## Concurrency on first download/upload

Let's suppose another user logs in and acquires its device. Assumptions:

- another device have previously logged in
	- hence a session is already opened

### DOWNLOAD scenario

Let's assume that the request is issued before the first
device uploads new informations (without loss of generality). 

- (HL2) new user acquires resource and calibrates
	- a download request is issued
- (SERVER) check quality
	- no points from quality
- (SERVER) try to find a previously defined session
	- there's one session already opened
	- with the same reference point
	- the server, when possible, enforces collaboration, hence the new session
		will be based on the other already opened one
- (SERVER) find every point in that radius WRT the current position
	- based on that session
	- (btw there's only one point currently: the current position)
- (SERVER) create positions inside the table
	- each of those positions will be based on the other session

### REQUEST download

```json
{
	current_pos : [x,y,z],
	radius : 500.0f, -- meters
}
```

### RESPONSE dowload

```json
{
	based_on : '...', -- not empty
	... waypoints ...
	... paths ...
	... area renamings ...
}
```

### UPLOAD Sscenario

- (HL2) time to update: upload request sent
- (SERVER) check if that session exists in staging
	- it exists
- (SERVER) check if the session is inherited by another one
	- also by checking out the JSON request, 
	- the field based_on IS NOT empty: inherited session
- (SERVER) concurrency check
	- get the session token of the inherited session
	- if that session is updating now, return 403 (please retry again...)
	- let's assume I am the first
- (SERVER) get MAX of ID for (inherited session union current session)
- (SERVER) starts PROCEDURE:
	- PREFILTERING : skip the wp if its ID is aready in this session
	- get distances from the points of this session UNION the inherited one
		- SELECT only new points from server data
	
	(foreach wp sent to the server)
	(1) -> WPs ALIGNMENT STEP: try to find "real ID" or to assig another one
	- find a distance which is under a certain threshold (MIN of dist must be gteq than)
	- IF foud a point under the threshold
		- ID of wp is te one within the distance
		- with quality:
			MAX( (threshold - dist) / threshold, 0 )
	- ELSE 
		- store a new ID
		- with quality:
			MAX( 1 - a * exp( b * (threshold - dist) ), 0 )
	- generate alignment table
	
	(foreach path sent to the server)
	(2) -> PATHS ALIGNMENT STEP: assign paths IDs depeding on alignment
	- check associated WPs
	- and rewrite waypoints IDs
	- check for area transitions

- (SERVER) send infos


### REQUEST upload

```json
{
	based_on : '...', -- not empty
	reference_pos : 'SARHL2_ID1234567890_REFP',
	with_areas : false,
	current_pos : [ x,y,z ],
	radius : 500.0f,
	... waypoints ...
	... paths ...
	... area renamings ...
}
```

### RESPONSE example (upload)

- the field 'aligned' allows to keep aligned local IDs on the device with server IDs
- server IDs are the one valid IDs: HL2 adapts infos based on alignment
- the alignment also indicates which waypoints have been loaded

{
	status : '',
	status_detail : '',
	max_id : ...,
	wps_alignment : [
		{ local_id : 1, server_id : 57 }, 
		...
	]
}

### How to deal with concurrency

...

### Alignment Quality Measurement

The problem involves alignments of points located on the border
of the selected threshold. 

The alignment is as good as its quality is near to 1:

- threshold
- dist : distance between the "redundant" point and the "real" one
- quality measurement:
	(threshold - dist) / threshold

you can also assign a quality for the "disalignments". In this case,
the quality can be formulated in this way:

- threshold
- dist : distance between the two points
- quality measurement:
	1 - a * exp( b * (threshold - dist) )
	'a' is a cooefficient for having the measure gt from a certain distance
- the quality is as good as he value is near to 1

====================================================== */

-- get session ID (just for testing)
SELECT get_session_of( 'SARHL2_ID8849249249_USER', 'SARHL2_ID8651165355_DEVC' );

-- another request arrives immediately after calibration
-- (api:DOWNLOAD)
/* DOWNLOAD REQUEST
```json
{
	-- inherited from base hl2 class
	user_id : 'SARHL2_ID8849249249_USER',
	device_id : 'SARHL2_ID8651165355_DEVC',
	session_token : '8e55b08c1c9270cfcd6cc45c68f7f7af',
	
	-- request data
	base_on : '', -- first request: it is empty
	reference-pos : 'SARHL2_ID1234567890_REFP'
	current_pos : [x,y,z],
	radius : 500.0f
}
```
*/

-- does the session exist in staging? --> NO
SELECT 
(COUNT(*) > 0)::BOOLEAN AS SESSION_DEFINED_IN_STAGING_FL
FROM sar.F_HL2_STAGING_WAYPOINTS
WHERE SESSION_TOKEN_ID = get_session_of( 'SARHL2_ID8849249249_USER', 'SARHL2_ID8651165355_DEVC' );

-- search for a good session to inherit
SELECT DISTINCT 
SESSION_TOKEN_ID, 
SESSION_TOKEN_INHERITED_ID, -- IF it IS NULL, the SESSION IS original
MAX(CREATED_TS) AS CREATED_TS
FROM sar.F_HL2_STAGING_WAYPOINTS
WHERE 1=1
	AND LOCAL_AREA_INDEX_ID = 0 
	AND U_REFERENCE_POSITION_ID = 'SARHL2_ID1234567890_REFP'
GROUP BY 1,2
ORDER BY 
	CREATED_TS DESC
LIMIT 1;
-- the first session is the most recent one

-- this is the session to inherit
-- insert the first session waypoint
INSERT INTO sar.F_HL2_STAGING_WAYPOINTS (
	DEVICE_ID,
	SESSION_TOKEN_ID, 
	SESSION_TOKEN_INHERITED_ID,
	U_REFERENCE_POSITION_ID, U_SOURCE_FROM_SERVER_FL,
	UX_VL, UY_VL, UZ_VL,
	LOCAL_POSITION_ID, 
	LOCAL_AREA_INDEX_ID
	-- AREA_RADIUS_VL ???
) VALUES (
	'SARHL2_ID8651165355_DEVC',
	get_session_of( 'SARHL2_ID8849249249_USER', 'SARHL2_ID8651165355_DEVC' ),
	get_session_of( 'SARHL2_ID2894646521_USER', 'SARHL2_ID0931557300_DEVC' ), -- inherited
	'SARHL2_ID1234567890_REFP', TRUE,
	0.00, 0.00, 0.00, 
	0, -- FIRST LOCAL positions IS ALWAYS zero since the device IS IN calibration point
	0  -- FIRST area_index IS ALWAYS zero
)
RETURNING 
	*;

-- (counter-check) is my current session inheriting data from another session?
SELECT DISTINCT 
SESSION_TOKEN_ID, 
SESSION_TOKEN_INHERITED_ID, -- it IS NOT NULL now 
MAX(CREATED_TS) AS CREATED_TS
FROM sar.F_HL2_STAGING_WAYPOINTS
WHERE SESSION_TOKEN_ID = get_session_of( 'SARHL2_ID8849249249_USER', 'SARHL2_ID8651165355_DEVC' )
GROUP BY 1,2
ORDER BY CREATED_TS DESC;

-- get max IDX on inherited SESSION 
SELECT 
MAX(LOCAL_POSITION_ID) AS MAX_LOCAL_POSITION_ID 
FROM sar.F_HL2_STAGING_WAYPOINTS
WHERE SESSION_TOKEN_ID = get_session_of( 'SARHL2_ID2894646521_USER', 'SARHL2_ID0931557300_DEVC' );
-- using inherited session id

-- get all the positions inside a given radius
SELECT 
*
FROM sar.F_HL2_STAGING_WAYPOINTS
WHERE SESSION_TOKEN_ID = get_session_of( 'SARHL2_ID2894646521_USER', 'SARHL2_ID0931557300_DEVC' )
-- let's assume a radius = 3
--    the current pos is due to based_on empty 
AND dist( UX_VL, UY_VL, UZ_VL, 0, 0, 0 ) <= 3; 


/* ======================================================

### First download -- logics so far

- (HL2) DOWNLOAD from device
	- immediately afer calibration
	- the database IS NOT empty
		- in particular, another session is generating data
- (SERVER) get infos
	- get sessions ordered from the most recent one
	- is the asking session currently active?
- is the session active also in staging? --> NO
	- add the reference point to the DB
	- create area index 0
- (SERVER) is there another session enabled? --> NO
	- (nothing to do: the information already inserted has NULL as inherited session)
- return
	- max_id : 0
	- waypoints_alignment : empty

====================================================== */
















/* ======================================================

## Third download

Assumptions:

- positions IDs are aligned wih the server

Scenario:

- (HL2) time to get infos from server!
	- asking wrt a current pos
	- and a given radius around the current pos
	- NO NEED FOR a wps exclusion list as well, since
		1. IDs are aligned
		2. the server knows what has been sent to device
			(just SELECT DISTNCT IDs from staging wps table)
- (SERVER) check session and checkout session history
	- try to find the inherited session
	- for that reference point
	- for that user session token
- (SERVER) starts PROCEDURE:
		- get exclusion list
		- search points by radius given the distance between
			the point and the current position
			exclude the already sent ones
		- get known paths involving waypoints
		
		(for each waypoint into the table, iterate)
		- find paths related to wp
			(for each path starting from this wp) 
			- find wp inside the table
			- and iterate over the foud wp only if it is new	

====================================================== */

-- ...











/* ======================================================

## 

====================================================== */

-- ...











/* ======================================================

## 

====================================================== */

-- ...











/* ======================================================

## CLEANUP

====================================================== */

-- close opened sessions
SELECT user_device_logout();

