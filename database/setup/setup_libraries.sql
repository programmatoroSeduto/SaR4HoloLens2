
/* ======================================================

# The SAR PROJECT : Libraries and Custom SQL Functions

## Custom Packages

- vectorial computations support Ffor KNN/Distance queries

## Custom functions

- 'dist' : distance between two vectors by their coordinates
- 'component_of' : a component of a vector ype
- 'to_vector3' : three numbers to  vector3 object

====================================================== */



-- his extension allows to usethe type 'vector' and to use operators such as
-- '<->' : eucledian distance
-- '<#>' : scalar product (NOTE WELL: negative scalar product)
-- https://www.postgresql.org/docs/current/sql-createextension.html
DROP EXTENSION IF EXISTS vector CASCADE;
CREATE EXTENSION vector ;
-- don't use schemas to install extensions!
-- https://stackoverflow.com/questions/75904637/how-to-fix-postgres-error-operator-does-not-exist-using-pgvector

-- to check if everything worked fine with the installation
SELECT * FROM PG_CATALOG.PG_EXTENSION;












/* ======================================================

## UserDefined functions -- Geometry

- 'dist' : distance between two vectors by their coordinates
- 'component_of' : a component of a vector ype
- 'to_vector3' : three numbers to  vector3 object

====================================================== */

-- distance between two 3D points
CREATE OR REPLACE FUNCTION dist( Ax float, Ay float, Az float, Bx float, By float, Bz float )
	RETURNS float(15)
	LANGUAGE plpgsql
AS
$$ BEGIN 
	RETURN sqrt((Bx - Ax)*(Bx - Ax) + (By - Ay)*(By - Ay) + (Bz - Az)*(Bz - Az));
END $$;




-- extract oe coordinate from vector() object
CREATE OR REPLACE FUNCTION component_of( v vector(3), compno int )
	RETURNS float
	LANGUAGE plpgsql
AS $$
DECLARE 
	compval float;
BEGIN
	WITH vtext_data AS (
	SELECT SUBSTR( v::TEXT, 2, LENGTH(v::TEXT)-2 )::TEXT AS vtxt
	)
	SELECT SPLIT_PART( v.vtxt, ',', compno )::float
	INTO compval
	FROM vtext_data AS v;

	RETURN compval;
END
$$;




-- cast three values to vector3
CREATE OR REPLACE FUNCTION to_vector3( Px float, Py float, Pz float )
	RETURNS vector(3)
	LANGUAGE plpgsql
AS $$
BEGIN
	RETURN CONCAT('[',Px,',',Py,',',Pz,']')::vector(3);
END
$$;





/* ======================================================

## UserDefined functions -- Utilities

- 'dist' : distance between two vectors by their coordinates
- 'component_of' : a component of a vector ype
- 'to_vector3' : three numbers to  vector3 object

====================================================== */

CREATE OR REPLACE FUNCTION sar_user_id( code bigint )
	RETURNS CHAR(24)
	LANGUAGE plpgsql
AS $$ BEGIN
	RETURN CONCAT( 'SARHL2_ID', LPAD(code::TEXT, 10, '0'), '_USER' );
END $$;





CREATE OR REPLACE FUNCTION sar_device_id( code bigint )
	RETURNS CHAR(24)
	LANGUAGE plpgsql
AS $$ BEGIN
	RETURN CONCAT( 'SARHL2_ID', LPAD(code::TEXT, 10, '0'), '_DEVC' );
END $$;





CREATE OR REPLACE FUNCTION sar_reference_point_id( code bigint )
	RETURNS CHAR(24)
	LANGUAGE plpgsql
AS $$ BEGIN
	RETURN CONCAT( 'SARHL2_ID', LPAD(code::TEXT, 10, '0'), '_REFP' );
END $$;





-- minimum between a set of numbers (max 5 values)
CREATE OR REPLACE FUNCTION max_of(
	val1 NUMERIC ,
	val2 NUMERIC , 
	val3 NUMERIC DEFAULT -9999999999,
	val4 NUMERIC DEFAULT -9999999999,
	val5 NUMERIC DEFAULT -9999999999
)
	RETURNS NUMERIC 
	LANGUAGE plpgsql
AS $$ 
DECLARE 
	max_from_query NUMERIC;
BEGIN
	SELECT MAX(val)::NUMERIC AS val
	INTO max_from_query
	FROM (
		SELECT val1::NUMERIC AS val
		UNION
		SELECT val2::NUMERIC AS val
		UNION
		SELECT val3::NUMERIC AS val
		UNION
		SELECT val4::NUMERIC AS val
		UNION
		SELECT val5::NUMERIC AS val
	) AS val_set;
	
	RETURN max_from_query;
END $$;





-- minimum between a set of numbers (max 5 values)
CREATE OR REPLACE FUNCTION min_of(
	val1 NUMERIC ,
	val2 NUMERIC , 
	val3 NUMERIC DEFAULT 9999999999,
	val4 NUMERIC DEFAULT 9999999999,
	val5 NUMERIC DEFAULT 9999999999
)
	RETURNS NUMERIC 
	LANGUAGE plpgsql
AS $$ 
DECLARE 
	max_from_query NUMERIC;
BEGIN
	SELECT MIN(val)::NUMERIC AS val
	INTO max_from_query
	FROM (
		SELECT val1::NUMERIC AS val
		UNION
		SELECT val2::NUMERIC AS val
		UNION
		SELECT val3::NUMERIC AS val
		UNION
		SELECT val4::NUMERIC AS val
		UNION
		SELECT val5::NUMERIC AS val
	) AS val_set;
	
	RETURN max_from_query;
END $$;












/* ======================================================

## Utility Functions

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
	arg_session_id TEXT)
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
CREATE FUNCTION get_known_points_by_session(arg_session_id text)
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
		COALESCE(arg_session_inherited_id, arg_session_id)
		) AS wp
	LEFT JOIN get_known_points_by_session(
		arg_session_id
		) AS known_wp
		ON ( wp.LOCAL_POSITION_ID = known_wp.LOCAL_POSITION_ID )
	WHERE 1=1
	AND known_wp.LOCAL_POSITION_ID IS NULL
	);
END $$;

DROP FUNCTION IF EXISTS get_current_position_local_id;
CREATE FUNCTION get_current_position_local_id(
	arg_refpos_id CHAR(24),
	arg_session_inherited_id TEXT,
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

DROP FUNCTION IF EXISTS get_unknown_paths_in_radius;
CREATE FUNCTION get_unknown_paths_in_radius(
	arg_refpos_id CHAR(24),
	arg_session_inherited_id CHAR(24),
	arg_session_id CHAR(24),
	arg_x FLOAT,
	arg_y FLOAT,
	arg_z FLOAT,
	arg_radius FLOAT 
	)
	RETURNS TABLE (
		WAYPOINT_1_STAGING_FK BIGINT,
		WP1_LOCAL_POS_ID INT,
		WAYPOINT_2_STAGING_FK BIGINT,
		WP2_LOCAL_POS_ID INT,
		DISTANCE_VL FLOAT,
		CREATED_TS TIMESTAMP
	)
	LANGUAGE plpgsql
AS $$ BEGIN
	RETURN QUERY (
	
	WITH 
	unknown_wps AS (
		SELECT DISTINCT
			LOCAL_POSITION_ID
		FROM get_unknown_waypoints_in_radius(
			arg_refpos_id,
			arg_session_inherited_id,
			arg_session_id,
			arg_x, arg_y, arg_z, arg_radius
		)
	)
	, paths AS (
		SELECT DISTINCT
			wp1.F_HL2_QUALITY_WAYPOINTS_PK AS WAYPOINT_1_STAGING_FK,
			wp1.LOCAL_POSITION_ID AS WP1_LOCAL_POS_ID,
			wp2.F_HL2_QUALITY_WAYPOINTS_PK AS WAYPOINT_2_STAGING_FK,
			wp2.LOCAL_POSITION_ID AS WP2_LOCAL_POS_ID,
			dist( wp1.UX_VL, wp1.UY_VL, wp1.UZ_VL, wp2.UX_VL, wp2.UY_VL, wp2.UZ_VL )::FLOAT AS DISTANCE_VL,
			COALESCE(wp1.WAYPOINT_CREATED_TS, wp1.CREATED_TS) AS CREATED_TS
		FROM sar.F_HL2_STAGING_PATHS
			AS pt
		JOIN sar.F_HL2_STAGING_WAYPOINTS
			AS wp1
			ON ( pt.WAYPOINT_1_STAGING_FK = wp1.F_HL2_QUALITY_WAYPOINTS_PK )
		LEFT JOIN unknown_wps AS unknown_wp1
			ON ( wp1.LOCAL_POSITION_ID = unknown_wp1.LOCAL_POSITION_ID )
		JOIN sar.F_HL2_STAGING_WAYPOINTS
			AS wp2
			ON ( pt.WAYPOINT_2_STAGING_FK = wp2.F_HL2_QUALITY_WAYPOINTS_PK )
		LEFT JOIN unknown_wps AS unknown_wp2
			ON ( wp2.LOCAL_POSITION_ID = unknown_wp2.LOCAL_POSITION_ID )
		WHERE 1=1
		AND pt.U_REFERENCE_POSITION_ID = arg_refpos_id
		AND (
			( pt.SESSION_TOKEN_INHERITED_ID IS NULL AND pt.SESSION_TOKEN_ID = arg_session_inherited_id )
			OR
			( pt.SESSION_TOKEN_INHERITED_ID = arg_session_inherited_id )
		)
		AND ( unknown_wp1.LOCAL_POSITION_ID IS NOT NULL OR unknown_wp2.LOCAL_POSITION_ID IS NOT NULL )
	)
	SELECT PATHS.WAYPOINT_1_STAGING_FK, paths.WP1_LOCAL_POS_ID, PATHS.WAYPOINT_2_STAGING_FK, paths.WP2_LOCAL_POS_ID, paths.DISTANCE_VL, paths.CREATED_TS
	FROM paths
	UNION 
	SELECT PATHS.WAYPOINT_1_STAGING_FK, paths.WP1_LOCAL_POS_ID, PATHS.WAYPOINT_2_STAGING_FK, paths.WP2_LOCAL_POS_ID, paths.DISTANCE_VL, paths.CREATED_TS
	FROM paths
	
	);
END $$ ;