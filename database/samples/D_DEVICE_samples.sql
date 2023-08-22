
/* ======================================================

# sar.D_DEVICE samples

====================================================== */

DELETE FROM sar.D_DEVICE
    WHERE 1=1;
INSERT INTO sar.D_DEVICE (
    DEVICE_ID,
    DEVICE_DS, DEVICE_LOCAL_DS, DEVICE_TYPE_DS,
    DEVICE_IS_HOLDABLE_FL
    , CAP_LOCATION_GEO_FL, CAP_LOCATION_RELATIVE_FL
    , CAP_EXCHANGE_SEND_FL, CAP_EXCHANGE_RECEIVE_FL
    , CAP_USAGE_AUTONOMOUS, CAP_USAGE_WEARABLE
    , AUTH_ACCESS_USER_FL, AUTH_ACCESS_DEVICE_FL
    , AUTH_UPDATE_USER_FL, AUTH_UPDATE_DEVICE_FL
)
VALUES

/* DEVICE
    code: 
        0000000001
    descriptive:
        aaaa
        bbbb
        cccc
    allowed users:
        --
    
    example purposes:

(
    CONCAT('SARHL2_ID', '0000000001', '_DEVC' ),
    'aaa', -- dev ds
    'bbb', -- dev local ds
    'ccc', -- dev type (deails about the manufacturer or the model of the sensor)
    true, -- holdable
    false, true, -- geo, rel
    true, true, -- send receive
    false, true, -- autonomous, wearable
    true, true, -- user read, dev read
    true, true -- user write, dev write
)
*/
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
    'hololens2', 
    'laboratorium hololens 2 SAR project implementation v2.0', 
    'Microsoft HoloLens2',
    true, -- holdable
    false, true, -- geo, rel
    true, true, -- send receive
    false, true, -- autonomous, wearable
    false, true, -- user read, dev read
    false, true -- user write, dev write
)
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
, (
    CONCAT('SARHL2_ID', '0931557300', '_DEVC' ),
    'hololens2', 
    'laboratorium hololens 2 MySceneUnderstanding v1.0', 
    'Microsoft HoloLens2',
    true, -- holdable
    false, true, -- geo, rel
    true, true, -- send receive
    false, true, -- autonomous, wearable
    false, false, -- user read, dev read
    false, false -- user write, dev write
)
/* DEVICE
    code: 
        CONCAT('SARHL2_ID', '6524946500', '_DEVC' ),
    descriptive:
        env sensor
        Thingy Environmental Sensor
        Nordic Thingy:91
    allowed users:
        --
    
    example purposes:
        this is a sensor allowed with no active capabilities, 
        to be integrated by the server: it cannot be hold by
        a user
*/
, (
    CONCAT('SARHL2_ID', '6524946500', '_DEVC' ),
    'env sensor', 
    'Thingy Environmental Sensor', 
    'Nordic Thingy:91',
    false, -- holdable
    true, false, -- geo, rel
    false, false, -- send receive
    false, false, -- autonomous, wearable
    false, false, -- user read, dev read
    false, false -- user write, dev write
)
/* DEVICE
    code: 
        CONCAT('SARHL2_ID', '9429845501', '_DEVC' ),
    descriptive:
        env sensor
        Thingy Environmental Sensor
        Nordic Thingy:91
    allowed users:
        --
    
    example purposes:
        another available sensor  of the same kinf of 
        the one with code SARHL2_ID65249465_DEVC
*/
, (
    CONCAT('SARHL2_ID', '9429845501', '_DEVC' ),
    'env sensor', 
    'Thingy Environmental Sensor', 
    'Nordic Thingy:91',
    false, -- holdable
    true, false, -- geo, rel
    false, false, -- send receive
    false, false, -- autonomous, wearable
    false, false, -- user read, dev read
    false, false -- user write, dev write
)
/* DEVICE
    code: 
        CONCAT('SARHL2_ID', '9942894900', '_DEVC' ),
    descriptive:
        Aerial Drone
        scout drone fleet B dji mavic
        DJI Mavic 3
    allowed users:
        --
    
    example purposes:
        the daabase can also collect authonomous systems such as a aerial
        drone, integrated with the database. 
*/

, (
    CONCAT('SARHL2_ID', '9942894900', '_DEVC' ),
    'Aerial Drone', -- dev ds
    'scout drone fleet B dji mavic', -- dev local ds
    'DJI Mavic 3', -- dev type (deails about the manufacturer or the model of the sensor)
    false, -- holdable
    true, true, -- geo, rel
    true, true, -- send receive
    true, false, -- autonomous, wearable
    false, false, -- user read, dev read
    true, true -- user write, dev write
)
/* DEVICE
    code: 
        CONCAT('SARHL2_ID', '2364847654', '_DEVC' ),
    descriptive:
        Robot
        spot -- robot fleet A no.2 system v1.2
        Boston Dynamics Spot
    allowed users:
        --
    
    example purposes:
        A  example of terrestrian robot
*/
, (
    CONCAT('SARHL2_ID', '2364847654', '_DEVC' ),
    'Robot', -- dev ds
    'spot -- robot fleet A no.2 system v1.2', -- dev local ds
    'Boston Dynamics Spot', -- dev type (deails about the manufacturer or the model of the sensor)
    false, -- holdable
    false, true, -- geo, rel
    true, true, -- send receive
    true, false, -- autonomous, wearable
    false, true, -- user read, dev read
    false, true -- user write, dev write
)
/* DEVICE
    code: 
        CONCAT('SARHL2_ID', '8216347654', '_DEVC' ),
    descriptive:
        Robot
        spot -- robot fleet A no.4 system v0.9
        Boston Dynamics Spot
    allowed users:
        --
    
    example purposes:
        A  example of terrestrian robot
*/
, (
    CONCAT('SARHL2_ID', '8216347654', '_DEVC' ),
    'Robot', -- dev ds
    'spot -- robot fleet A no.4 system v0.9', -- dev local ds
    'Boston Dynamics Spot', -- dev type (deails about the manufacturer or the model of the sensor)
    false, -- holdable
    true, false, -- geo, rel
    true, true, -- send receive
    true, false, -- autonomous, wearable
    false, true, -- user read, dev read
    false, true -- user write, dev write
)
;
SELECT * FROM sar.D_DEVICE;