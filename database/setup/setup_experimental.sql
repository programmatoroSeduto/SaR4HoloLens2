
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

