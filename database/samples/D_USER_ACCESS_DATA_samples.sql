
/* ======================================================

# sar.D_USER_ACCESS_DATA samples

====================================================== */

UPDATE sar.D_USER_ACCESS_DATA
    SET DELETED_FL = true
WHERE 1=1
;

INSERT INTO sar.D_USER_ACCESS_DATA (
    USER_ID,
    USER_ACCESS_CODE_ID
) VALUES
/* USER
    code: 
        0000000001
        CONCAT('SARHL2_ID', '0000000001', '_USER' ),
    approver:
        ---
    name:
        TheMasterOfPuppets
    properties:
        admin: X
        external: 
        can have device: X
        access users: X
        access device: X
    access key:
        aaaaaa
    
    example purposes:
        a test admin user with full access
*/ 
(
    CONCAT('SARHL2_ID', '0000000001', '_USER' ),
    MD5('aaaaaa')
)
/* USER
    code: 
        0000100001
        CONCAT('SARHL2_ID', '0000100001', '_USER' ),
    approver:
        0000000001
        CONCAT('SARHL2_ID', '0000000000', '_USER' ),
    name:
        'Pippo'
    properties:
        admin: 
        external: 
        can have device: X
        access users: X
        access device: X
    access key:
        MD5('xxxxxx')
    
    example purposes:
        a example user that needs to be approved by the user. 
        This user allows to test the the login functonality. He can
        hold a device and read/write with it. 
*/
,(
    CONCAT('SARHL2_ID', '0000100001', '_USER' ),
    MD5('xxxxxx')
)
;

SELECT * FROM sar.D_USER_ACCESS_DATA;