using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;

using Project.Scripts.Components;
using Project.Scripts.Utils;

namespace Packages.SAR4HL2NetworkingSettings.Utils
{ 
    // ===== BASE REQUEST RESPONSE ===== //

    [Serializable]
    public class api_base_request
    {

    }

    [Serializable]
    public class api_base_response
    {
        public string timestamp_received;
        public string timestamp_sent;
        public int status;
        public string status_detail;
    }



    // ===== USER LOGIN ===== //

    [Serializable]
    public class api_user_login_request : api_base_request
    {
        public string user_id;
        public string approver_id;
        public string access_key;
    }

    [Serializable]
    public class api_user_login_response : api_base_response
    {
        public string session_token;
    }



    // ===== USER LOGOUT ===== //

    [Serializable]
    public class api_user_logout_request : api_base_request
    {
        public string user_id;
        public string session_token;
    }

    [Serializable]
    public class api_user_logout_response : api_base_response
    {
        public List<string> logged_out_devices;
    }



    // ===== DEVICE LOGIN ===== //

    [Serializable]
    public class api_device_login_request : api_base_request
    {
        public string user_id;
        public string device_id;
        public string session_token;
    }

    [Serializable]
    public class api_device_login_response : api_base_response
    {

    }



    // ===== DEVICE LOGIN ===== //

    [Serializable]
    public class api_device_logout_request : api_base_request
    {
        public string user_id;
        public string device_id;
        public string session_token;
    }

    [Serializable]
    public class api_device_logout_response : api_base_response
    {

    }
}

