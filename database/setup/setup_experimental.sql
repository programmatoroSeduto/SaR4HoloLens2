
/* ======================================================

# The SAR PROJECT : setup experimental

Use this script for doing tests when you need to restart 
frequently the Docker service. 

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

-- get opened session for a user (or NULL)
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
-- USER: SARHL2_ID2894646521_USER -> HL2: SARHL2_ID0931557300_DEVC
SELECT user_device_login( 'SARHL2_ID2894646521_USER', 'SARHL2_ID2894646521_USER', 'SARHL2_ID0931557300_DEVC' );
-- 62e78fc224f5856c2f9630a9ac6d4c0b

-- let's open second session
-- USER: SARHL2_ID8849249249_USER -> HL2: SARHL2_ID8651165355_DEVC
SELECT user_device_login( 'SARHL2_ID8849249249_USER', 'SARHL2_ID8849249249_USER', 'SARHL2_ID8651165355_DEVC' );
-- 7607cdb68c36e0d0f7e462a16309451a

-- check the USER sessions opened
SELECT * FROM sar.F_USER_ACTIVITY WHERE USER_END_AT_TS IS NULL;

-- check the DEVICE sessions opened
SELECT * FROM sar.F_DEVICE_ACTIVITY WHERE DEVICE_OFF_AT_TS IS NULL;

-- reference point -> 'SARHL2_ID1234567890_REFP'
SELECT * FROM sar.D_HL2_REFERENCE_POSITIONS;

-- register USER1 first waypoint
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

-- insert new waypoints
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

-- insert new paths
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