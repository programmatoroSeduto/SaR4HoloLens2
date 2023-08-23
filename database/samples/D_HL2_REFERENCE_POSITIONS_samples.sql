
/* ======================================================

# sar.D_HL2_REFERENCE_POSITIONS 

====================================================== */

-- delete previously defined samples
DELETE FROM sar.D_HL2_REFERENCE_POSITIONS;

-- samples
INSERT INTO sar.D_HL2_REFERENCE_POSITIONS (
	REFERENCE_POSITION_ID, 
	REFERENCE_POSITION_DS
)
VALUES 
( 
	sar_reference_point_id(1234567890),
	'OPERATIVE PROCEDURE: 1) stay on the center of the red cross on the floor, 2) look at the red cross on the wall'
);

-- samples check
SELECT * FROM sar.D_HL2_REFERENCE_POSITIONS;