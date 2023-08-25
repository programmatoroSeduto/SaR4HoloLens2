DROP TYPE IF EXISTS json_schema;
CREATE TYPE json_schema AS (
	wp1 BIGINT,
	wp2 BIGINT,
	dist FLOAT,
	pt_timestamp TIMESTAMP
);
WITH 
request_data AS (
SELECT
    wp1,
    wp2,
    dist,
    pt_timestamp
FROM JSON_POPULATE_RECORDSET(NULL::json_schema,
'[
	{"wp1" : 0, "wp2" : 1, "dist" : 1.0, "pt_timestamp" : "2023/08/23, 00:00:02"},
	{"wp1" : 1, "wp2" : 2, "dist" : 1.0, "pt_timestamp" : "2023/08/23, 00:00:03"},
	{"wp1" : 2, "wp2" : 3, "dist" : 1.0, "pt_timestamp" : "2023/08/23, 00:00:05"},
	{"wp1" : 0, "wp2" : 4, "dist" : 1.0, "pt_timestamp" : "2023/08/23, 00:00:09"}
]')
) -- SELECT * FROM request_data;
, waypoints_base AS (
SELECT 
	*
FROM sar.F_HL2_STAGING_WAYPOINTS
WHERE 1=1
AND U_REFERENCE_POSITION_ID = 'SARHL2_ID1234567890_REFP'
AND SESSION_TOKEN_ID = 'bf001756272ed463ce1d522470967e38'
) -- SELECT * FROM waypoints_base;
, paths_renamed AS (
SELECT 
	wp1map.SESSION_TOKEN_ID AS SESSION_TOKEN_ID,
	wp1map.SESSION_TOKEN_INHERITED_ID AS SESSION_TOKEN_INHERITED_ID,
	wp1map.U_REFERENCE_POSITION_ID AS U_REFERENCE_POSITION_ID,
	wp1map.F_HL2_QUALITY_WAYPOINTS_PK AS WAYPOINT_1_STAGING_FK,
	wp2map.F_HL2_QUALITY_WAYPOINTS_PK AS WAYPOINT_2_STAGING_FK,
	COALESCE(
		request_data.dist,
		dist( wp1map.UX_VL, wp1map.UY_VL, wp1map.UZ_VL, wp2map.UX_VL, wp2map.UY_VL, wp2map.UZ_VL )
	) AS PATH_DISTANCE,
	COALESCE(
		request_data.pt_timestamp,
		CURRENT_TIMESTAMP
	) AS CREATED_TS
FROM request_data
LEFT JOIN waypoints_base 
	AS wp1map
	ON( request_data.wp1 = wp1map.REQUEST_POSITION_ID )
LEFT JOIN waypoints_base 
	AS wp2map
	ON( request_data.wp2 = wp2map.REQUEST_POSITION_ID )
) -- SELECT * FROM paths_renamed;
, paths_renamed_filtered AS (
SELECT 
	* 
FROM paths_renamed
WHERE ( WAYPOINT_1_STAGING_FK, WAYPOINT_2_STAGING_FK ) NOT IN (
	SELECT 
		WAYPOINT_1_STAGING_FK, WAYPOINT_2_STAGING_FK
	FROM sar.F_HL2_STAGING_PATHS
	WHERE 1=1
	AND SESSION_TOKEN_ID = 'bf001756272ed463ce1d522470967e38'
	AND SESSION_TOKEN_INHERITED_ID = '42b9a74748e1d20befb8f0df94c0f1cc'
	AND U_REFERENCE_POSITION_ID = 'SARHL2_ID1234567890_REFP'
	UNION ALL 
	SELECT 
		WAYPOINT_2_STAGING_FK, WAYPOINT_1_STAGING_FK
	FROM sar.F_HL2_STAGING_PATHS
	WHERE 1=1
	AND SESSION_TOKEN_ID = 'bf001756272ed463ce1d522470967e38'
	AND SESSION_TOKEN_INHERITED_ID = '42b9a74748e1d20befb8f0df94c0f1cc'
	AND U_REFERENCE_POSITION_ID = 'SARHL2_ID1234567890_REFP'
)
) -- SELECT * FROM paths_renamed_filtered;
, insert_step AS (
INSERT INTO sar.F_HL2_STAGING_PATHS (
	DEVICE_ID,
	SESSION_TOKEN_ID,
	SESSION_TOKEN_INHERITED_ID,
	U_REFERENCE_POSITION_ID,
	WAYPOINT_1_STAGING_FK,
	WAYPOINT_2_STAGING_FK,
	PATH_DISTANCE,
	CREATED_TS )
SELECT
	'SARHL2_ID8651165355_DEVC' AS DEVICE_ID,
	*
FROM paths_renamed_filtered
RETURNING *
) -- SELECT * FROM insert_step;
SELECT COUNT(*) AS inserted_paths FROM insert_step;










----------------------------------------------------------------------
SELECT get_session_of( 'SARHL2_ID8849249249_USER', 'SARHL2_ID8651165355_DEVC' ); -- USER2
SELECT get_session_of( 'SARHL2_ID2894646521_USER', 'SARHL2_ID0931557300_DEVC' ); -- USER1

-- this is the session to inherit
-- insert the first session waypoint
INSERT INTO sar.F_HL2_STAGING_WAYPOINTS (
	DEVICE_ID,
	SESSION_TOKEN_ID, 
	SESSION_TOKEN_INHERITED_ID,
	U_REFERENCE_POSITION_ID, U_SOURCE_FROM_SERVER_FL,
	UX_VL, UY_VL, UZ_VL,
	LOCAL_POSITION_ID, 
	LOCAL_AREA_INDEX_ID,
	REQUEST_POSITION_ID
	-- AREA_RADIUS_VL ???
) VALUES (
	'SARHL2_ID8651165355_DEVC',
	get_session_of( 'SARHL2_ID8849249249_USER', 'SARHL2_ID8651165355_DEVC' ),
	get_session_of( 'SARHL2_ID2894646521_USER', 'SARHL2_ID0931557300_DEVC' ), -- inherited
	'SARHL2_ID1234567890_REFP', TRUE,
	0.00, 0.00, 0.00, 
	0, -- FIRST LOCAL positions IS ALWAYS zero since the device IS IN calibration point
	0, -- FIRST area_index IS ALWAYS zero
	0
)
RETURNING 
	*;