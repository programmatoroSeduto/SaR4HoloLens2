
/* ======================================================

# sar.L_DEVICE_USER samples

====================================================== */

UPDATE sar.L_DEVICE_USER
    SET DELETED_FL = true
WHERE DEVICE_ID IN (
    CONCAT('SARHL2_ID', '8651165355', '_DEVC' ),
    CONCAT('SARHL2_ID', '0931557300', '_DEVC' )
)
;

INSERT INTO sar.L_DEVICE_USER (
    DEVICE_ID,
    USER_ID
) VALUES

/* DEVICE
    code: 
        CONCAT('SARHL2_ID', '8651165355', '_DEVC' ),
    descriptive:
        hololens2
        laboratorium hololens 2 SAR project implementation v1.0
        Microsoft HoloLens2
    allowed users:
        CONCAT('SARHL2_ID', '8849249249', '_USER' ),
        CONCAT('SARHL2_ID', '2894646521', '_USER' ),
        CONCAT('SARHL2_ID', '4243264423', '_USER' ),
    
    example purposes:
        a hololens2 device used by SAR operators
*/
( 
    CONCAT('SARHL2_ID', '8651165355', '_DEVC' ),
    CONCAT('SARHL2_ID', '8849249249', '_USER' )
),
( 
    CONCAT('SARHL2_ID', '8651165355', '_DEVC' ),
    CONCAT('SARHL2_ID', '2894646521', '_USER' )
),
( 
    CONCAT('SARHL2_ID', '8651165355', '_DEVC' ),
    CONCAT('SARHL2_ID', '4243264423', '_USER' )
),

/* DEVICE
    code: 
        CONCAT('SARHL2_ID', '0931557300', '_DEVC' ),
    descriptive:
        hololens2
        external device not used for SAR
        Microsoft HoloLens2
    allowed users:
        CONCAT('SARHL2_ID', '8849249249', '_USER' ),
        CONCAT('SARHL2_ID', '2315989415', '_USER' ),
        CONCAT('SARHL2_ID', '2894646521', '_USER' ),
    
    example purposes:
        example of a HoloLens2 device not allowed to communicate
        with the server. It has no access
*/
( 
    CONCAT('SARHL2_ID', '0931557300', '_DEVC' ),
    CONCAT('SARHL2_ID', '8849249249', '_USER' )
),
( 
    CONCAT('SARHL2_ID', '0931557300', '_DEVC' ),
    CONCAT('SARHL2_ID', '2315989415', '_USER' )
),
( 
    CONCAT('SARHL2_ID', '0931557300', '_DEVC' ),
    CONCAT('SARHL2_ID', '2894646521', '_USER' )
)
;
INSERT INTO sar.L_DEVICE_USER (
    DEVICE_ID,
    USER_ID
) VALUES 
( 'SARHL2_ID7864861468_DEVC', 'SARHL2_ID2894646521_USER' );