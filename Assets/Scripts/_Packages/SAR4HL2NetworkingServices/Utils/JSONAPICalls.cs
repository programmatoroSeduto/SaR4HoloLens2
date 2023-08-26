using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;

using Project.Scripts.Components;
using Project.Scripts.Utils;

namespace Packages.SAR4HL2NetworkingSettings.Utils
{
    // ===== BASE DATA CLASSES ===== //

    [Serializable]
    public class data_base_pack
    {

    }



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



    // ===== HL2 GENERIC ===== //

    [Serializable]
    public class api_hl2_base_request : api_base_request
    {
        public string user_id;
        public string device_id;
        public string session_token;
    }

    [Serializable]
    public class api_hl2_base_response : api_base_response
    {

    }

    [Serializable]
    public class data_hl2_waypoint : data_base_pack
    {
        public int pos_id;
        public int area_id;
        public List<float> v;
        public string wp_timestamp;
    }

    [Serializable]
    public class data_hl2_path : data_base_pack
    {
        public int wp1;
        public int wp2;
        public float dist;
        public string pt_timestamp;
    }

    [Serializable]
    public class data_hl2_align_item : data_base_pack
    {
        public int request_position_id;
        public int aligned_position_id;
    }



    // ===== HL2 DOWNLOAD ===== //

    [Serializable]
    public class api_hl2_download_request : api_hl2_base_request
    {
        public string based_on;
        public string ref_id;
        public List<float> center;
        public float radius;
    }

    [Serializable]
    public class api_hl2_download_response : api_hl2_base_response
    {
        public string based_on;
        public string ref_id;
        public int max_id;
        public List<data_hl2_waypoint> waypoints;
        public List<data_hl2_path> paths;
    }



    // ===== HL2 UPLOAD ===== //

    [Serializable]
    public class api_hl2_upload_request : api_hl2_base_request
    {
        public string based_on;
        public string ref_id;
        public List<data_hl2_waypoint> waypoints;
        public List<data_hl2_path> paths;
    }

    [Serializable]
    public class api_hl2_upload_response : api_hl2_base_response
    {
        public int max_id;
        public List<data_hl2_align_item> wp_alignment;
    }



    // ===== HL2 SETTINGS FROM SERVER ===== //

    [Serializable]
    public class api_hl2_settings_request : api_hl2_base_request
    {
        public int config_profile_id;
    }

    [Serializable]
    public class api_hl2_settings_response : api_hl2_base_response
    {
        public bool found_profile;
        public int config_profile_id;

        public float user_height;
        public float base_height;
        public float base_distance;
        public float distance_tollerance;

        public bool use_cluster;
        public int cluster_size;

        public bool use_max_indices;
        public int max_indices;

        public int log_layer;

        public string ref_id;
    }
}

