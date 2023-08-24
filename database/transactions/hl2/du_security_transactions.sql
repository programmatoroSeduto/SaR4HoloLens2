
/* ======================================================

# The SAR PROJECT : The Download Upload Protocol

This SQL script describes the main security checks implemented
in the class 

```
main_app
    .api_transactions
        .api_security_transactions
            FILE:  ud_security_support
            CLASS: ud_security_support 
```

List of supported security checks:

- try to get the real session token
- check if that fake session token is 

====================================================== */








/* ======================================================

def try_get_real_token_from_fake(self, 
    user_id, 
    device_id, 
    owner_token, 
    fake_token)

====================================================== */

SELECT 
	USER_SESSION_TOKEN_ID 
FROM sar.F_SESSION_ALIAS
WHERE 1=1
AND USER_ID  = %(user_id)s
AND DEVICE_ID  = %(device_id)s
AND OWNER_SESSION_TOKEN_ID = %(owner_token)s
AND FAKE_SESSION_TOKEN_ID = %(fake_token)s;



/* ======================================================

def get_fake_token_infos(self, 
    fake_token)

the functin returns None when the token does not exist. 

====================================================== */

SELECT 
	USER_ID, DEVICE_ID, OWNER_SESSION_TOKEN_ID, USER_SESSION_TOKEN_ID 
FROM sar.F_SESSION_ALIAS
WHERE 1=1
AND FAKE_SESSION_TOKEN_ID = %(fake_token)s
LIMIT 1; -- the TOKEN SHOULD be unique 



/* ======================================================

def has_fake_token(self, 
    user_id, 
    device_id, 
    owner_token)

the functin returns None when the token does not exist. 

====================================================== */

SELECT 
	USER_SESSION_TOKEN_ID, FAKE_SESSION_TOKEN_ID, SALT_ID 
FROM sar.F_SESSION_ALIAS
WHERE 1=1
AND USER_ID  = %(user_id)s
AND DEVICE_ID  = %(device_id)s
AND OWNER_SESSION_TOKEN_ID = %(owner_token)s
LIMIT 1; -- a USER SHOULD have ONLY one fake TOKEN opened FOR the device...



/* ======================================================

def create_fake_session_token(self, 
    user_id, 
    device_id, 
    owner_token,
    user_token) -> bool

the function fails if another token with the same user and device exists. 
(use the function has_session_token)

====================================================== */

-- insert phase (query to review)
INSERT INTO sar.F_SESSION_ALIAS 
SELECT 
    %(user_id)s AS USER_ID ,
    %(device_id)s AS DEVICE_ID ,
    %(owner_token)s AS OWNER_SESSION_TOKEN_ID ,
    data_tab.session_to_hide AS USER_SESSION_TOKEN_ID ,
    data_tab.salt_code AS SALT_ID ,
    MD5(
        CONCAT( data_tab.salt_code, data_tab.session_to_hide, data_tab.salt_code )
    ) AS FAKE_SESSION_TOKEN_ID
FROM (
    SELECT 
        MD5(FLOOR(RANDOM()*100000000)::TEXT) AS salt_code,
        %(user_token)s AS session_to_hide
) AS data_tab ;



/* ======================================================

...

====================================================== */