
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