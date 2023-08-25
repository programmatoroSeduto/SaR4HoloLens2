DROP TYPE IF EXISTS json_schema;
CREATE TYPE json_schema AS (
	pos_id int,
	area_id int,
	v vector(3),
	wp_timestamp TIMESTAMP
);
WITH 
request_data AS (
SELECT
    pos_id,
    area_id,
    v,
    wp_timestamp
FROM JSON_POPULATE_RECORDSET(NULL::json_schema,
'[
	{
		"pos_id" : 1,
		"area_id" : 0,
		"v" : [ 0, 0, 1.9 ],
		"wp_timestamp" : "2023/08/23 18:00:02"
	},
	{
		"pos_id" : 2,
		"area_id" : 0,
		"v" : [ 0, 0, 4.05 ],
		"wp_timestamp" : "2023/08/23 18:00:03"
	},
	{
		"pos_id" : 3,
		"area_id" : 0,
		"v" : [ 0, 0, 7.97 ],
		"wp_timestamp" : "2023/08/23 18:00:05"
	},
	{
		"pos_id" : 4,
		"area_id" : 0,
		"v" : [ 2.05, 0, 0 ],
		"wp_timestamp" : "2023/08/23 18:00:10"
	}
]')
WHERE 1=1
AND pos_id NOT IN (
	SELECT DISTINCT 
		LOCAL_POSITION_ID
	FROM sar.F_HL2_STAGING_WAYPOINTS
	WHERE 1=1
	AND SESSION_TOKEN_ID = 'bf001756272ed463ce1d522470967e38'
	AND U_REFERENCE_POSITION_ID = 'SARHL2_ID1234567890_REFP'
)
) -- SELECT * FROM request_data;
, session_data AS (
SELECT 
	F_HL2_QUALITY_WAYPOINTS_PK AS align_with_fk,
	LOCAL_POSITION_ID AS pos_id,
	LOCAL_AREA_INDEX_ID AS area_id,
	to_vector3( UX_VL, UY_VL, UZ_VL )::vector(3) AS v,
	COALESCE(WAYPOINT_CREATED_TS, CREATED_TS) AS wp_timestamp
FROM sar.F_HL2_STAGING_WAYPOINTS
WHERE 1=1
AND NOT(ALIGNMENT_TYPE_FL)
AND U_REFERENCE_POSITION_ID = 'SARHL2_ID1234567890_REFP'
AND (
	SESSION_TOKEN_INHERITED_ID IS NULL
	OR
	SESSION_TOKEN_INHERITED_ID = '42b9a74748e1d20befb8f0df94c0f1cc'
)
) -- SELECT * FROM session_data;
, cross_data AS (
SELECT 
	request_data.pos_id AS req_pos_id,
	session_data.pos_id AS loc_pos_id,
	( request_data.v <-> session_data.v ) AS dist,
	request_data.v AS req_v,
	session_data.v AS loc_v,
	session_data.align_with_fk AS align_with_fk,
	request_data.area_id AS req_area_id,
	session_data.area_id AS loc_area_id,
	request_data.wp_timestamp AS req_timestamp,
	session_data.wp_timestamp AS loc_timestamp
FROM request_data 
LEFT JOIN session_data ON (1=1)
) -- SELECT * FROM cross_data ORDER BY req_pos_id, dist, loc_pos_id;
, analysis_data AS (
SELECT 
	req_pos_id,
	loc_pos_id,
	align_with_fk,
	req_area_id,
	loc_area_id,
	req_v,
	loc_v,
	cross_data.dist AS dist,
	CASE 
		WHEN cross_data.dist IS NULL THEN FALSE
		WHEN cross_data.dist <= th.threshold_vl - th.toll THEN TRUE
		WHEN cross_data.dist >= th.threshold_vl + th.toll THEN FALSE 
		ELSE (RANDOM()>0.5)::BOOLEAN
	END AS WP_IS_REDUNDANT_FL,
	CASE 
		WHEN cross_data.dist IS NULL THEN 100.0
		WHEN cross_data.dist <= th.threshold_vl - th.toll 
			THEN ROUND(max_of( 
				((th.threshold_vl - cross_data.dist) / th.threshold_vl)::NUMERIC, 
				0::NUMERIC ) * 100, 2)
		WHEN cross_data.dist >= th.threshold_vl + th.toll
			THEN ROUND(max_of( 
				(1 - 1.00*exp( 4.75*(th.threshold_vl - cross_data.dist) ))::NUMERIC,
				0::NUMERIC ) * 100, 2)
		ELSE 0.001
	END AS QUALITY_VL,
	req_timestamp,
	loc_timestamp
FROM cross_data
LEFT JOIN ( SELECT 
	1.3::FLOAT AS threshold_vl,
	0.01::FLOAT AS toll 
) AS th ON (1=1)
) -- SELECT * FROM analysis_data ORDER BY req_pos_id, dist, loc_pos_id;
, classification_data AS (
SELECT DISTINCT
	req_pos_id,
	FIRST_VALUE(align_with_fk)
		OVER ( PARTITION BY req_pos_id ORDER BY dist ASC )
		AS align_with_fk,
	FIRST_VALUE(loc_pos_id)
		OVER ( PARTITION BY req_pos_id ORDER BY dist ASC )
		AS align_with_loc_pos_id,
	FIRST_VALUE(req_v)
		OVER ( PARTITION BY req_pos_id ORDER BY dist ASC )
		AS req_v,
	FIRST_VALUE(loc_v)
		OVER ( PARTITION BY req_pos_id ORDER BY dist ASC )
		AS loc_v,
	FIRST_VALUE(dist)
		OVER ( PARTITION BY req_pos_id ORDER BY dist ASC )
		AS dist,
	FIRST_VALUE(WP_IS_REDUNDANT_FL) 
		OVER ( PARTITION BY req_pos_id ORDER BY dist ASC )
		AS WP_IS_REDUNDANT_FL,
	FIRST_VALUE(QUALITY_VL)
		OVER ( PARTITION BY req_pos_id ORDER BY dist ASC )
		AS QUALITY_VL,
	FIRST_VALUE(req_timestamp)
		OVER ( PARTITION BY req_pos_id ORDER BY dist ASC )
		AS req_timestamp,
	FIRST_VALUE(loc_timestamp)
		OVER ( PARTITION BY req_pos_id ORDER BY dist ASC )
		AS loc_timestamp
FROM analysis_data
) -- SELECT * FROM classification_data;
, set_wps_new AS (
INSERT INTO sar.F_HL2_STAGING_WAYPOINTS (DEVICE_ID, SESSION_TOKEN_ID, SESSION_TOKEN_INHERITED_ID, LOCAL_POSITION_ID, REQUEST_POSITION_ID, U_REFERENCE_POSITION_ID,UX_VL, UY_VL, UZ_VL, U_SOURCE_FROM_SERVER_FL, LOCAL_AREA_INDEX_ID,WAYPOINT_CREATED_TS, ALIGNMENT_ALIGNED_WITH_WAYPOINT_FK,ALIGNMENT_TYPE_FL, ALIGNMENT_QUALITY_VL, ALIGNMENT_DISTANCE_VL,ALIGNMENT_DISTANCE_FROM_WAYPOINT_FK)
SELECT 
	'SARHL2_ID8651165355_DEVC' AS DEVICE_ID,
	'bf001756272ed463ce1d522470967e38' AS SESSION_TOKEN_ID,
	'42b9a74748e1d20befb8f0df94c0f1cc' SESSION_TOKEN_INHERITED_ID,
	req_pos_id AS LOCAL_POSITION_ID,
	req_pos_id AS REQUEST_POSITION_ID,
	'SARHL2_ID1234567890_REFP' AS U_REFERENCE_POSITION_ID,
	component_of(req_v, 1) AS UX_VL,
	component_of(req_v, 2) AS UY_VL,
	component_of(req_v, 3) AS UZ_VL,
	FALSE AS U_SOURCE_FROM_SERVER_FL,
	0 AS LOCAL_AREA_INDEX_ID,
	req_timestamp AS WAYPOINT_CREATED_TS,
	NULL::BIGINT AS ALIGNMENT_ALIGNED_WITH_WAYPOINT_FK,
	FALSE AS ALIGNMENT_TYPE_FL,
	QUALITY_VL AS ALIGNMENT_QUALITY_VL,
	dist AS ALIGNMENT_DISTANCE_VL,
	align_with_fk AS ALIGNMENT_DISTANCE_FROM_WAYPOINT_FK 
FROM classification_data
WHERE NOT(WP_IS_REDUNDANT_FL)
RETURNING *
) -- SELECT * FROM set_wps_new;
, set_wps_aligned AS (
INSERT INTO sar.F_HL2_STAGING_WAYPOINTS (DEVICE_ID, SESSION_TOKEN_ID, SESSION_TOKEN_INHERITED_ID, LOCAL_POSITION_ID, REQUEST_POSITION_ID, U_REFERENCE_POSITION_ID,UX_VL, UY_VL, UZ_VL, U_SOURCE_FROM_SERVER_FL, LOCAL_AREA_INDEX_ID,WAYPOINT_CREATED_TS, ALIGNMENT_ALIGNED_WITH_WAYPOINT_FK,ALIGNMENT_TYPE_FL, ALIGNMENT_QUALITY_VL, ALIGNMENT_DISTANCE_VL,ALIGNMENT_DISTANCE_FROM_WAYPOINT_FK)
SELECT 
	'SARHL2_ID8651165355_DEVC' AS DEVICE_ID,
	'bf001756272ed463ce1d522470967e38' AS SESSION_TOKEN_ID,
	'42b9a74748e1d20befb8f0df94c0f1cc' SESSION_TOKEN_INHERITED_ID,
	align_with_loc_pos_id AS LOCAL_POSITION_ID,
	req_pos_id AS REQUEST_POSITION_ID,
	'SARHL2_ID1234567890_REFP' AS U_REFERENCE_POSITION_ID,
	component_of(loc_v, 1) AS UX_VL,
	component_of(loc_v, 2) AS UY_VL,
	component_of(loc_v, 3) AS UZ_VL,
	TRUE AS U_SOURCE_FROM_SERVER_FL,
	0 AS LOCAL_AREA_INDEX_ID,
	loc_timestamp AS WAYPOINT_CREATED_TS,
	align_with_fk AS ALIGNMENT_ALIGNED_WITH_WAYPOINT_FK,
	TRUE AS ALIGNMENT_TYPE_FL,
	QUALITY_VL AS ALIGNMENT_QUALITY_VL,
	dist AS ALIGNMENT_DISTANCE_VL,
	align_with_fk AS ALIGNMENT_DISTANCE_FROM_WAYPOINT_FK 
FROM classification_data
WHERE WP_IS_REDUNDANT_FL
RETURNING *
) -- SELECT * FROM set_wps_new UNION ALL SELECT * FROM set_wps_aligned;
SELECT 1;
DROP TYPE json_schema;