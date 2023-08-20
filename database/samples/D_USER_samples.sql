
/* ======================================================

# D_USER samples

====================================================== */

/* USER
    code: 
        CONCAT('SARHL2_ID', '0000000000', '_USER' ),
    approver:
        CONCAT('SARHL2_ID', '0000000000', '_USER' ),
    name:
        aaaaaa
    access key:
        MD5('aaaaaa')
    
    example purposes:
        --
, (
    CONCAT('SARHL2_ID', '0000000000', '_USER' ),
    null, -- approver
    'aaa', -- name
    true, false, -- admin, external
    true, -- can have device
    true, true, true, true -- r_user, r_dev, w_user, w_dev
)
*/
DELETE FROM sar.D_USER;
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
        CONCAT('SARHL2_ID', '8849249249', '_USER' ),
    approver:
        --
    name:
        Francesco Ganci
    access key:
        MD5('anoth3rBr3akabl3P0sswArd')
    
    example purposes:
        admin user with full access
*/
(
    CONCAT('SARHL2_ID', '8849249249', '_USER' ),
    null, -- approver
    'Francesco Ganci', -- name
    true, false, -- admin, external
    true, -- can have device
    true, true, true, true -- r_user, r_dev, w_user, w_dev
)
/* USER
    code: 
        CONCAT('SARHL2_ID', '2894646521', '_USER' ),
    approver:
        --
    name:
        Carmine Recchiuto
    access key:
        MD5('s3vHngLh_F3s')
    
    example purposes:
        another admin user with full access
*/
, (
    CONCAT('SARHL2_ID', '2894646521', '_USER' ),
    null, -- approver
    'Carmine Recchiuto', -- name
    true, false, -- admin, external
    true, -- can have device
    true, true, true, true -- r_user, r_dev, w_user, w_dev
)
/* USER
    code: 
        CONCAT('SARHL2_ID', '9924613168', '_USER' ),
    approver:
        CONCAT('SARHL2_ID', '8849249249', '_USER' ),
    name:
        Petronilla Panicucci
    access key:
        MD5('aaaaaa')
    
    example purposes:
        An example of external user, allowed to access dev data
        but not user neither has writing auths. 
*/
, (
    CONCAT('SARHL2_ID', '9924613168', '_USER' ),
    CONCAT('SARHL2_ID', '8849249249', '_USER' ), -- approver
    'Petronilla Panicucci', -- name
    false, true, -- admin, external
    false, -- can have device
    false, true, false, false -- r_user, r_dev, w_user, w_dev
)
/* USER
    code: 
        CONCAT('SARHL2_ID', '3216546890', '_USER' ),
    approver:
        CONCAT('SARHL2_ID', '2894646521', '_USER' ),
    name:
        Ermes Siciliano
    access key:
        MD5('1234viafasulla')
    
    example purposes:
        An example of SAR operator, not allowed to use a device
        but allowed to access user data (rw) and device data (r)
*/
, (
    CONCAT('SARHL2_ID', '3216546890', '_USER' ),
    CONCAT('SARHL2_ID', '2894646521', '_USER' ), -- approver
    'Ermes Siciliano', -- name
    false, false, -- admin, external
    false, -- can have device
    true, true, false, true -- r_user, r_dev, w_user, w_dev
)
/* USER
    code: 
        CONCAT('SARHL2_ID', '2315989415', '_USER' ),
    approver:
        CONCAT('SARHL2_ID', '8849249249', '_USER' ),
    name:
        Affiano Toscano
    access key:
        MD5('de_hboia')
    
    example purposes:
        An example of SAR operator: he can only read sensors, and
        he's allowed to have a device, but he has not writing auths. 
*/
, (
    CONCAT('SARHL2_ID', '2315989415', '_USER' ),
    CONCAT('SARHL2_ID', '8849249249', '_USER' ), -- approver
    'Affiano Toscano', -- name
    false, false, -- admin, external
    true, -- can have device
    false, true, false, false -- r_user, r_dev, w_user, w_dev
)
/* USER
    code: 
        CONCAT('SARHL2_ID', '4243264423', '_USER' ),
    approver:
        CONCAT('SARHL2_ID', '8849249249', '_USER' ),
    name:
        Lorenzo Terranova
    access key:
        MD5('casseruola96')
    
    example purposes:
        An example of SAR operator with full access to devices. 
*/
, (
    CONCAT('SARHL2_ID', '4243264423', '_USER' ),
    CONCAT('SARHL2_ID', '8849249249', '_USER' ), -- approver
    'Lorenzo Terranova', -- name
    false, false, -- admin, external
    true, -- can have device
    false, true, false, true -- r_user, r_dev, w_user, w_dev
)
/* USER
    code: 
        CONCAT('SARHL2_ID', '1236814232', '_USER' ),
    approver:
        CONCAT('SARHL2_ID', '2894646521', '_USER' ),
    name:
        Isabella Nucci
    access key:
        MD5('isaBellaMaConI_baffi')
    
    example purposes:
        SAR operator from central unit. She has full access
        but she can't hold a device neither she's a admin. 
*/
, (
    CONCAT('SARHL2_ID', '1236814232', '_USER' ),
    CONCAT('SARHL2_ID', '2894646521', '_USER' ), -- approver
    'Isabella Nucci', -- name
    false, false, -- admin, external
    false, -- can have device
    true, true, true, true -- r_user, r_dev, w_user, w_dev
)
/* USER
    code: 
        CONCAT('SARHL2_ID', '0000003185', '_USER' ),
    approver:
        CONCAT('SARHL2_ID', '8849249249', '_USER' ),
    name:
        ACTIVE DIRECTORY -- SavingLivesInc
    access key:
        MD5(';:SAR::456ae256361')
    
    example purposes:
        A example of technical user: a external company able to
        read data from the sensorsin by the server. 
*/
, (
    CONCAT('SARHL2_ID', '0000003185', '_USER' ),
    CONCAT('SARHL2_ID', '2894646521', '_USER' ), -- approver
    'ACTIVE DIRECTORY - SavingLives Inc', -- name
    false, true, -- admin, external
    false, -- can have device
    false, true, false, false -- r_user, r_dev, w_user, w_dev
)
/* USER
    code: 
        CONCAT('SARHL2_ID', '2648476658', '_USER' ),
    approver:
        CONCAT('SARHL2_ID', '8849249249', '_USER' ),
    name:
        Valentina Gallo
    access key:
        MD5('YM14438686')
    
    example purposes:
        An example of SAR operator: she can only read sensors and users. 
        Not allowed to have a device. 
*/
, (
    CONCAT('SARHL2_ID', '2648476658', '_USER' ),
    CONCAT('SARHL2_ID', '8849249249', '_USER' ), -- approver
    'Valentina Gallo', -- name
    false, false, -- admin, external
    false, -- can have device
    true, true, false, false -- r_user, r_dev, w_user, w_dev
)
/* USER
    code: 
        CONCAT('SARHL2_ID', '9782446036', '_USER' ),
    approver:
        CONCAT('SARHL2_ID', '8849249249', '_USER' ),
    name:
        Filiberto Esposito
    access key:
        MD5('chicchiricchJH_')
    
    example purposes:
        SAR operator capable to have a device, but not to read and write
        except for devices (read only). 
*/
, (
    CONCAT('SARHL2_ID', '9782446036', '_USER' ),
    CONCAT('SARHL2_ID', '8849249249', '_USER' ), -- approver
    'Filiberto Esposito', -- name
    false, false, -- admin, external
    true, -- can have device
    false, true, false, false -- r_user, r_dev, w_user, w_dev
)
;
SELECT * FROM sar.D_USER;