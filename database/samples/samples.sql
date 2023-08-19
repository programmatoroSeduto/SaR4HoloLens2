
/* ======================================================

# Samples Script

Use this script to populate the database. It could help when 
you need to do a experiment. 

====================================================== */










/* ====================================================== 

## USERS

- https://it.fakenamegenerator.com/gen-random-it-it.php

====================================================== */

TRUNCATE TABLE sar.D_USER;
INSERT INTO sar.D_USER (
    user_id,
    user_is_external_fl,
    user_name_ds, user_phone_number_1_ds, user_phone_number_2_ds, user_address_ds,
    user_height_vl, 
    user_admin_fl, auth_hold_device_fl, auth_access_user_fl, auth_access_device_fl, auth_update_user_fl, auth_update_device_fl
) 
VALUES 
 ( -- ADMIN user with full access; he can't hold a device
    'SARHL2_ID1124200849_USER',
    false,
    'Mario Rossi', '334 971 7302', null, 'via fasulla 1234, Firenze (FI)', 
    1.85,
    true, false, true, true, true, true
)
,( -- EXTERNAL user with no access to the data
    'SARHL2_ID5566448822_USER',
    true,
    'Rosaria Toscano', null, null, 'Via Belviglieri, 67 00153-Roma RM', 
    null,
    false, false, false, false, false, false
)
,( -- OPERATOR user with full device access (but not other users)
    'SARHL2_ID7070151656_USER',
    false,
    'Crispino Piccio', null, null, 'Via Belviglieri, 68 00153-Roma RM', 
    1.85,
    false, true, false, true, false, true
)
,( -- OPERATOR can read user data and nothing else
    'SARHL2_ID9876543215_USER',
    false,
    'Cesio Milano', '0310 9042143', null, 'Via Duomo, 55 57029-Venturina LI', 
    null,
    false, false, true, false, false, false
)
;

TRUNCATE TABLE sar.D_USER_ACCESS_DATA;
INSERT INTO sar.D_USER_ACCESS_DATA (
    user_id,
    user_access_code_id
) 
VALUES
( -- ADMIN mario rossi
    'SARHL2_ID1124200849_USER', MD5('aaaaaa')
)
, ( -- EXTERNAL Rosaria Toscano
    'SARHL2_ID5566448822_USER', MD5('bbbbbb')
)
, ( -- OPERATOR Crispino Piccio
    'SARHL2_ID7070151656_USER', MD5('cccccc')
)
;










/* ====================================================== 

## Activity Log

====================================================== */

-- ... --










/* ====================================================== 

## Devices

====================================================== */

-- ... --










/* ======================================================

## Hololens2 Support

====================================================== */

-- ... --










/* ======================================================

## IoT data integration

====================================================== */

-- ... --