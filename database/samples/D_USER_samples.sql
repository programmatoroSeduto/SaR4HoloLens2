
/* ======================================================

# D_USER samples

====================================================== */

/* USER
    code: 
        0000000001
        CONCAT('SARHL2_ID', '0000000000', '_USER' ),
    approver:
        0000000001
        CONCAT('SARHL2_ID', '0000000000', '_USER' ),
    name:
        aaaaaa
    properties:
        admin: X
        external: X
        can have device: X
        access users: X
        access device: X
    access key:
        MD5('aaaaaa')
    
    example purposes:

(
    CONCAT('SARHL2_ID', '0000000000', '_USER' ),
    null,
    'TheMasterOfPuppets',
    true, false,
    true,
    true, true, true, true
)
*/
delete from sar.D_USER;
INSERT INTO sar.D_USER (
    USER_ID,
    USER_APPROVED_BY_ID,
    USER_NAME_DS,
    USER_ADMIN_FL, USER_IS_EXTERNAL_FL,
    AUTH_HOLD_DEVICE_FL,
    AUTH_ACCESS_USER_FL, AUTH_ACCESS_DEVICE_FL, AUTH_UPDATE_USER_FL, AUTH_UPDATE_DEVICE_FL

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
        MD5('aaaaaa')
    
    example purposes:
        a test admin user with full access
*/
(
    CONCAT('SARHL2_ID', '0000000001', '_USER' ),
    null,
    'TheMasterOfPuppets',
    true, false,
    true,
    true, true, true, true
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
, (
    CONCAT('SARHL2_ID', '0000100001', '_USER' ),
    CONCAT('SARHL2_ID', '0000000001', '_USER' ),
    'Pippo',
    false, false,
    true,
    false, true, false, true
)
;