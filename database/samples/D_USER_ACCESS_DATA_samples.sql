
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
    MD5('anoth3rBr3akabl3P0sswArd')
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
    MD5('s3vHngLh_F3s')
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
    MD5('aaaaaa')
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
    MD5('1234viafasulla')
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
    MD5('de_hboia')
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
    MD5('casseruola96')
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
    MD5('isaBellaMaConI_baffi')
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
    MD5(';:SAR::456ae256361')
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
    MD5('YM14438686')
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
    MD5('chicchiricchJH_')
)
;

SELECT * FROM sar.D_USER_ACCESS_DATA;