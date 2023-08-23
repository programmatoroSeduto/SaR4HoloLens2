
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

## Functions Area

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
WHERE USER_ID IN ( 'SARHL2_ID8849249249_USER', 'SARHL2_ID2894646521_USER' )

-- select DEVICEs
SELECT ldu.USER_ID, dev.device_id FROM sar.D_DEVICE dev LEFT JOIN sar.L_DEVICE_USER ldu ON ( dev.DEVICE_ID = ldu.DEVICE_ID )
WHERE NOT(dev.DELETED_FL) AND dev.DEVICE_TYPE_DS='Microsoft HoloLens2'
AND ldu.USER_ID IN ( 'SARHL2_ID8849249249_USER', 'SARHL2_ID2894646521_USER' )
ORDER BY 1, 2;

-- just to be sure, close all the previously opened sessions
SELECT user_device_logout();

-- let's open first session
-- USER: SARHL2_ID2894646521_USER -> HL2: SARHL2_ID0931557300_DEVC
SELECT user_device_login( 'SARHL2_ID2894646521_USER', 'SARHL2_ID2894646521_USER', 'SARHL2_ID0931557300_DEVC' );

-- let's open second session
-- USER: SARHL2_ID8849249249_USER -> HL2: SARHL2_ID8651165355_DEVC
SELECT user_device_login( 'SARHL2_ID8849249249_USER', 'SARHL2_ID8849249249_USER', 'SARHL2_ID8651165355_DEVC' );

-- check the USER sessions opened
SELECT * FROM sar.F_USER_ACTIVITY WHERE USER_END_AT_TS IS NULL;

-- check the DEVICE sessions opened
SELECT * FROM sar.F_DEVICE_ACTIVITY WHERE DEVICE_OFF_AT_TS IS NULL;












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
- (SERVER) check quality
	- no points from quality
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
	status_defaul : '',
	max_id : ...,
	alignment : [
		{ local_id : 0, server_id : o }, 
		...
	]
}

### How to manage area renamings

the device just sends areas with 0 if the flag 'with_areas' is false.
When it is true, the server also expects new zones, but this option
is used only immediately after the device logs out. 

====================================================== */

-- create reference point -> 'SARHL2_ID1234567890_REFP'
-- ...













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

-- ...












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

