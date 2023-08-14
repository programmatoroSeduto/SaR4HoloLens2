using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Project.Scripts.Utils;
using Packages.DiskStorageServices.Components;
using Packages.PositionDatabase.Components;

namespace Project.Scripts.Components
{
    public class AppSettings : MonoBehaviour
    {
        // service settings
        public string OperatorUniversalUserID = "SARHL2_ID0000000_USER";
        public string OperatorUniversalDeviceID = "SARHL2_ID0000000_DEV";

        // User Settings
        public float UserHeight = 1.85f;

        // server connection settings
        public string ServerIpAddress = "127.0.0.1";
        public string ServerPortNo = "5000";

        // other settings
        public bool DebugMode = false;

        // global objects
        public StorageHubOneShot StorageHub = null;
        public PositionsDatabase PositionsDatabase = null;

        private void Start()
        {
            StaticAppSettings.SetOpt("OperatorUniversalUserID", OperatorUniversalUserID);
            StaticAppSettings.SetOpt("OperatorUniversalDeviceID", OperatorUniversalDeviceID);

            StaticAppSettings.SetOpt("UserHeight", UserHeight.ToString());
            StaticAppSettings.SetOpt("ServerIpAddress", ServerIpAddress);
            StaticAppSettings.SetOpt("ServerPortNo", ServerPortNo);
            StaticAppSettings.SetOpt("IsDebugMode", (DebugMode ? "true" : "false"));

            StaticAppSettings.SetObject("AppSettings", this);
            StaticAppSettings.SetObject("StorageHub", StorageHub);
            StaticAppSettings.SetObject("PositionsDatabase", PositionsDatabase);
        }

    }
}
