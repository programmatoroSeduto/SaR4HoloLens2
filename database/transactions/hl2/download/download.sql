
/* ======================================================

# TRANSACTION (HL2) : Download Positions To Device 

API: api/hl2/{HL2_device}/download
    - PATH
        - device_id
    - REQUEST:
        - user_id
        - session_token
        - full_import
        - reference_pos_id
        - current_pos_vector
        - data_radous
    - RESPONSE:
        - OK|KO
        - ... JSON export to hl2 ...
        - ... support informations for HL2 ...

## High-level export procedure

- full import
    - ... a distance-based import with fixed distance ...
- not full import
    - ... based on distance ...

## Requests examples

...

## Responses examples

...

====================================================== */










/* ====================================================== 

## CHECK -- TRANSACTION QUERY

====================================================== */

SELECT 

*

FROM sar.D_TABLE
;










/* ====================================================== 

## CHECK -- OPERATIVE PROCEDURE

```

```

====================================================== */










/* ====================================================== 

## SUCCESS -- TRANSACTION

- ...

====================================================== */

BEGIN;

-- ... --

COMMIT;










/* ====================================================== 

## ERROR -- TRANSACTION

====================================================== */

BEGIN;

-- ... --

COMMIT;










/* ====================================================== 

## ENDING NOTES

...

====================================================== */