
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

/* ======================================================

## Protocol-related functions

these functions enable a more rigorous support for the implementation
and testing of the algorithm. 

Template for functions:

CREATE OR REPLACE FUNCTION aaaa( bbbb TEXT, cccc TEXT )
	RETURNS TEXT
	LANGUAGE plpgsql
AS $$ 
DECLARE 
	dddd
BEGIN
	eeee
END $$ ;

====================================================== */

CREATE OR REPLACE FUNCTION session_in_staging_fl( arg_session_id TEXT, arg_refpos_id TEXT )
	RETURNS BOOLEAN
	LANGUAGE plpgsql
AS $$ 
DECLARE 
	USER_FOUND_FL BOOLEAN;
BEGIN
	SELECT 
		(COUNT(*)>0) AS USER_FOUND_FL
	FROM sar.F_HL2_STAGING_WAYPOINTS
	INTO USER_FOUND_FL
	WHERE 1=1
	AND U_REFERENCE_POSITION_ID = arg_refpos_id
	AND SESSION_TOKEN_ID = arg_session_id;
	
	RETURN USER_FOUND_FL;
END $$ ;

CREATE OR REPLACE FUNCTION inheritable_session_id( arg_refpos_id TEXT )
	RETURNS TEXT
	LANGUAGE plpgsql
AS $$ 
DECLARE 
	INHERITABLE_SESSION_ID TEXT;
BEGIN
	SELECT DISTINCT
		SESSION_TOKEN_ID
	FROM sar.F_HL2_STAGING_WAYPOINTS
	INTO INHERITABLE_SESSION_ID
	WHERE 1=1
	AND U_REFERENCE_POSITION_ID = arg_refpos_id
	AND SESSION_TOKEN_INHERITED_ID IS NULL;
	
	RETURN INHERITABLE_SESSION_ID;
END $$ ;

-- register_staging_session_father(arg_device_id, arg_session_token_id, arg_refpos_id)
CREATE OR REPLACE FUNCTION register_staging_session_father( 
	arg_device_id TEXT,
	arg_session_token_id TEXT,
	arg_refpos_id TEXT
	)
	RETURNS BIGINT
	LANGUAGE plpgsql
AS $$ 
DECLARE 
	NEW_KEY_FK BIGINT DEFAULT NULL;
BEGIN
	WITH insert_step AS ( 
		INSERT INTO sar.F_HL2_STAGING_WAYPOINTS (
			-- F_HL2_QUALITY_WAYPOINTS_PK,
			 DEVICE_ID,
			 SESSION_TOKEN_ID,
			 SESSION_TOKEN_INHERITED_ID,
			 LOCAL_POSITION_ID,
			 REQUEST_POSITION_ID,
			 U_REFERENCE_POSITION_ID,
			 -- U_LEFT_HANDED_REFERENCE_FL,
			 UX_VL,
			 UY_VL, 
			 UZ_VL, 
			 U_SOURCE_FROM_SERVER_FL, 
			 LOCAL_AREA_INDEX_ID,
			 -- WAYPOINT_CREATED_TS,
			 ALIGNMENT_ALIGNED_WITH_WAYPOINT_FK,
			 ALIGNMENT_TYPE_FL,
			 ALIGNMENT_QUALITY_VL,
			 ALIGNMENT_DISTANCE_VL,
			 ALIGNMENT_DISTANCE_FROM_WAYPOINT_FK
		)
		VALUES (
			-- F_HL2_QUALITY_WAYPOINTS_PK,
			 arg_device_id, -- DEVICE_ID,
			 arg_session_token_id, -- SESSION_TOKEN_ID,
			 NULL, -- SESSION_TOKEN_INHERITED_ID,
			 0, -- LOCAL_POSITION_ID,
			 0, -- REQUEST_POSITION_ID,
			 arg_refpos_id, -- U_REFERENCE_POSITION_ID,
			 -- U_LEFT_HANDED_REFERENCE_FL,
			 0.0, -- UX_VL,
			 0.0, -- UY_VL, 
			 0.0, -- UZ_VL, 
			 TRUE, -- U_SOURCE_FROM_SERVER_FL, 
			 0, -- LOCAL_AREA_INDEX_ID,
			 -- WAYPOINT_CREATED_TS,
			 NULL, -- ALIGNMENT_ALIGNED_WITH_WAYPOINT_FK,
			 FALSE, -- ALIGNMENT_TYPE_FL,
			 100.0, -- ALIGNMENT_QUALITY_VL,
			 0.00, -- ALIGNMENT_DISTANCE_VL,
			 NULL --ALIGNMENT_DISTANCE_FROM_WAYPOINT_FK
		)
		RETURNING * )
	SELECT
		F_HL2_QUALITY_WAYPOINTS_PK
	INTO NEW_KEY_FK
	FROM insert_step;
	
	RETURN NEW_KEY_FK;
END $$ ;

-- register_staging_session_child(arg_device_id, arg_session_token_id, arg_session_token_inherited_id, arg_refpos_id)
DROP FUNCTION IF EXISTS register_staging_session_child;
CREATE FUNCTION register_staging_session_child( 
	arg_device_id CHAR(24),
	arg_session_token_id TEXT,
	arg_session_token_inherited_id TEXT,
	arg_refpos_id CHAR(24)
	)
	RETURNS BIGINT
	LANGUAGE plpgsql
AS $$ 
DECLARE 
	NEW_KEY_FK BIGINT DEFAULT NULL;
BEGIN
	WITH insert_step AS ( 
		INSERT INTO sar.F_HL2_STAGING_WAYPOINTS (
			-- F_HL2_QUALITY_WAYPOINTS_PK,
			 DEVICE_ID,
			 SESSION_TOKEN_ID,
			 SESSION_TOKEN_INHERITED_ID,
			 LOCAL_POSITION_ID,
			 REQUEST_POSITION_ID,
			 U_REFERENCE_POSITION_ID,
			 -- U_LEFT_HANDED_REFERENCE_FL,
			 UX_VL,
			 UY_VL, 
			 UZ_VL, 
			 U_SOURCE_FROM_SERVER_FL, 
			 LOCAL_AREA_INDEX_ID,
			 -- WAYPOINT_CREATED_TS,
			 ALIGNMENT_ALIGNED_WITH_WAYPOINT_FK,
			 ALIGNMENT_TYPE_FL,
			 ALIGNMENT_QUALITY_VL,
			 ALIGNMENT_DISTANCE_VL,
			 ALIGNMENT_DISTANCE_FROM_WAYPOINT_FK
		)
		SELECT 
			-- F_HL2_QUALITY_WAYPOINTS_PK,
			 arg_device_id, -- DEVICE_ID,
			 arg_session_token_id, -- SESSION_TOKEN_ID,
			 arg_session_token_inherited_id, -- SESSION_TOKEN_INHERITED_ID,
			 0, -- LOCAL_POSITION_ID,
			 0, -- REQUEST_POSITION_ID,
			 arg_refpos_id, -- U_REFERENCE_POSITION_ID,
			 -- U_LEFT_HANDED_REFERENCE_FL,
			 0.0, -- UX_VL,
			 0.0, -- UY_VL, 
			 0.0, -- UZ_VL, 
			 TRUE, -- U_SOURCE_FROM_SERVER_FL, 
			 0, -- LOCAL_AREA_INDEX_ID,
			 -- WAYPOINT_CREATED_TS,
			 tab.ALIGNMENT_ALIGNED_WITH_WAYPOINT_FK, -- ALIGNMENT_ALIGNED_WITH_WAYPOINT_FK,
			 TRUE, -- ALIGNMENT_TYPE_FL,
			 100.0, -- ALIGNMENT_QUALITY_VL,
			 0.00, -- ALIGNMENT_DISTANCE_VL,
			 tab.ALIGNMENT_ALIGNED_WITH_WAYPOINT_FK -- ALIGNMENT_DISTANCE_FROM_WAYPOINT_FK
		FROM (
			SELECT
				ALIGNMENT_ALIGNED_WITH_WAYPOINT_FK
			FROM sar.F_HL2_STAGING_WAYPOINTS
			WHERE 1=1
			AND LOCAL_POSITION_ID = 0
			AND U_REFERENCE_POSITION_ID = arg_refpos_id
			AND SESSION_TOKEN_ID = arg_session_token_inherited_id
			AND SESSION_TOKEN_INHERITED_ID IS NULL
			ORDER BY 1 ASC
			LIMIT 1
		) AS tab
		RETURNING * )
	SELECT
		F_HL2_QUALITY_WAYPOINTS_PK
	INTO NEW_KEY_FK
	FROM insert_step;
	
	RETURN NEW_KEY_FK;
END $$ ;

DROP FUNCTION IF EXISTS get_near_waypoints;
CREATE FUNCTION get_near_waypoints(
	arg_x FLOAT, 
	arg_y FLOAT, 
	arg_z FLOAT, 
	arg_radius FLOAT,
	arg_refp_id CHAR(24),
	arg_session_id CHAR(24))
	RETURNS TABLE (
		F_HL2_QUALITY_WAYPOINTS_PK BIGINT,
		LOCAL_POSITION_ID INT,
		SESSION_TOKEN_ID TEXT,
		SESSION_TOKEN_INHERITED_ID TEXT
	)
	LANGUAGE plpgsql
AS $$ BEGIN
	RETURN QUERY (
	SELECT DISTINCT
		wp.F_HL2_QUALITY_WAYPOINTS_PK,
		wp.LOCAL_POSITION_ID,
		wp.SESSION_TOKEN_ID,
		wp.SESSION_TOKEN_INHERITED_ID
	FROM sar.F_HL2_STAGING_WAYPOINTS 
		AS wp
	WHERE 1=1
	AND NOT(wp.ALIGNMENT_TYPE_FL)
	AND wp.U_REFERENCE_POSITION_ID = arg_refp_id
	AND dist(wp.UX_VL, wp.UY_VL, wp.UZ_VL, arg_x, arg_y, arg_z) < arg_radius
	AND (
		( wp.SESSION_TOKEN_INHERITED_ID IS NULL AND wp.SESSION_TOKEN_ID = arg_session_id )
		OR 
		wp.SESSION_TOKEN_INHERITED_ID = arg_session_id
		)
	);
END $$;

DROP FUNCTION IF EXISTS get_session_generic_waypoints;
CREATE FUNCTION get_session_generic_waypoints(
	arg_refp_id CHAR(24),
	arg_session_id CHAR(24))
	RETURNS TABLE (
		F_HL2_QUALITY_WAYPOINTS_PK BIGINT,
		LOCAL_POSITION_ID INT,
		SESSION_TOKEN_ID TEXT,
		SESSION_TOKEN_INHERITED_ID TEXT
	)
	LANGUAGE plpgsql
AS $$ BEGIN
	RETURN QUERY (
	SELECT DISTINCT
		wp.F_HL2_QUALITY_WAYPOINTS_PK,
		wp.LOCAL_POSITION_ID,
		wp.SESSION_TOKEN_ID,
		wp.SESSION_TOKEN_INHERITED_ID
	FROM sar.F_HL2_STAGING_WAYPOINTS 
		AS wp
	WHERE 1=1
	AND NOT(wp.ALIGNMENT_TYPE_FL)
	AND wp.U_REFERENCE_POSITION_ID = arg_refp_id
	AND (
		( wp.SESSION_TOKEN_INHERITED_ID IS NULL AND wp.SESSION_TOKEN_ID = arg_session_id )
		OR 
		wp.SESSION_TOKEN_INHERITED_ID = arg_session_id
		)
	);
END $$;

DROP FUNCTION IF EXISTS get_known_points_by_session;
CREATE FUNCTION get_known_points_by_session(arg_session_id CHAR(24))
	RETURNS TABLE (
		F_HL2_QUALITY_WAYPOINTS_PK BIGINT,
		LOCAL_POSITION_ID INT
	)
	LANGUAGE plpgsql
AS $$ BEGIN
	RETURN QUERY (
	SELECT DISTINCT
		wp.F_HL2_QUALITY_WAYPOINTS_PK,
		wp.LOCAL_POSITION_ID
	FROM sar.F_HL2_STAGING_WAYPOINTS 
		AS wp
	WHERE 1=1
	AND wp.SESSION_TOKEN_ID = arg_session_id
	);
END $$;

DROP FUNCTION IF EXISTS get_unknown_points_by_session;
CREATE FUNCTION get_unknown_points_by_session(
	arg_refpos_id CHAR(24), 
	arg_session_inherited_id TEXT,
	arg_session_id TEXT)
	RETURNS TABLE (
		F_HL2_QUALITY_WAYPOINTS_PK BIGINT,
		LOCAL_POSITION_ID INT
	)
	LANGUAGE plpgsql
AS $$ BEGIN
	RETURN QUERY (
	SELECT DISTINCT
		wp.F_HL2_QUALITY_WAYPOINTS_PK,
		wp.LOCAL_POSITION_ID
	FROM get_session_generic_waypoints(
		arg_refpos_id,
		arg_session_inherited_id 
		) AS wp
	LEFT JOIN get_known_points_by_session(
		arg_session_id
		) AS known_wp
		ON ( wp.LOCAL_POSITION_ID = known_wp.LOCAL_POSITION_ID )
	WHERE 1=1
	AND known_wp.LOCAL_POSITION_ID IS NULL
	);
END $$;

DROP FUNCTION IF EXISTS current_position_id;
CREATE FUNCTION current_position_id(
	arg_refpos_id CHAR(24),
	arg_session_inherited_id CHAR(24),
	arg_x FLOAT,
	arg_y FLOAT,
	arg_z FLOAT
	)
	RETURNS TEXT
	LANGUAGE plpgsql
AS $$ 
DECLARE 
	LOCAL_POSITION_ID INT;
BEGIN
	WITH tab AS ( 
		SELECT
			ROW_NUMBER() OVER ( 
				ORDER BY dist( UX_VL, UY_VL, UZ_VL, arg_x, arg_y, arg_z ) ASC
				) AS TAB_ORDER,
			wps.LOCAL_POSITION_ID
		FROM get_session_generic_waypoints(
			arg_refpos_id,
			arg_session_inherited_id )
			AS wps
		LEFT JOIN sar.F_HL2_STAGING_WAYPOINTS
			AS wps_data
			ON ( wps.F_HL2_QUALITY_WAYPOINTS_PK = wps_data.F_HL2_QUALITY_WAYPOINTS_PK )
	)
	SELECT 
		tab.LOCAL_POSITION_ID
	INTO LOCAL_POSITION_ID
	FROM tab
	WHERE TAB_ORDER = 1;
	
	RETURN LOCAL_POSITION_ID;
END $$ ;

DROP FUNCTION IF EXISTS get_unknown_waypoints_in_radius;
CREATE FUNCTION get_unknown_waypoints_in_radius(
	arg_refpos_id CHAR(24),
	arg_session_inherited_id CHAR(24),
	arg_session_id CHAR(24),
	arg_x FLOAT,
	arg_y FLOAT,
	arg_z FLOAT,
	arg_radius FLOAT 
	)
	RETURNS TABLE (
		F_HL2_QUALITY_WAYPOINTS_PK BIGINT,
		LOCAL_POSITION_ID INT,
		DISTANCE_FROM_SOURCE_VL FLOAT
	)
	LANGUAGE plpgsql
AS $$ BEGIN
	RETURN QUERY(
	SELECT 
		wp.F_HL2_QUALITY_WAYPOINTS_PK,
		wp.LOCAL_POSITION_ID,
		dist( UX_VL, UY_VL, UZ_VL, arg_x, arg_y, arg_z )::FLOAT AS DISTANCE_FROM_SOURCE_VL
	FROM get_unknown_points_by_session(
		arg_refpos_id,
		arg_session_inherited_id,
		arg_session_id
	) AS wp_base
	JOIN sar.F_HL2_STAGING_WAYPOINTS
		AS wp
		ON ( wp_base.F_HL2_QUALITY_WAYPOINTS_PK = wp.F_HL2_QUALITY_WAYPOINTS_PK )
	WHERE 1=1
	AND dist( UX_VL, UY_VL, UZ_VL, arg_x, arg_y, arg_z ) < arg_radius
	);
END $$ ;