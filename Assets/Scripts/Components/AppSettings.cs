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
        // ===== GUI ===== //

        [Header("service settings")]
        [Tooltip("OperatorUniversalUserID (8 digits)")]
        public string OperatorUniversalUserID = "SARHL2_ID00000000_USER";
        [Tooltip("OperatorUniversalDeviceID (8 digits)")]
        public string OperatorUniversalDeviceID = "SARHL2_ID00000000_DEV";

        // User Settings
        [Header("User settings")]
        [Tooltip("User Height")]
        public float UserHeight = 1.85f;

        // server connection settings
        [Header("server connection settings")]
        [Header("Server IP address")]
        public string ServerIpAddress = "127.0.0.1";
        [Header("Server IP Port number")]
        public string ServerPortNo = "5000";

        // other settings
        [Header("Development Settings")]
        [Tooltip("The debug mode allows to develop in editor and to test the application using a particular context on the device during the development of the application")]
        public bool DebugMode = false;

        // global objects
        [Header("Global Objects")]
        [Header("Storage hub if defined")]
        public StorageHubOneShot StorageHub = null;
        [Header("Global position database if defined")]
        public PositionsDatabase PositionsDatabase = null;

        [Header("Project Logger Opions")]
        [Tooltip("Wether use or not the log level feature")]
        public bool UseLogLayer = true;
        [Tooltip("Each log layer greater than this number will be suppressed")]
        public int LogLayer = 0;
        [Tooltip("Wether ignore or not some common messages")]
        public bool IgnoreListMessages = true;
        [Tooltip("Messages to ignore")]
        public List<string> SuppressedLogs = new List<string>
        {
            "Please remove the CanvasRenderer component from the [Label] GameObject as this component is no longer necessary."
        };

        [Header("Logging objects")]
        [Tooltip("Component writing the GENERAL log file")]
        public TxtWriter LoggerMain = null;
        [Tooltip("Component writing the INFOs on file")]
        public TxtWriter LoggerInfo = null;
        [Tooltip("Component writing the WARNINGs on file")]
        public TxtWriter LoggerWarning = null;
        [Tooltip("Component writing the ERRORs on file")]
        public TxtWriter LoggerError = null;

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

            StaticAppSettings.SetObject("LoggerMain", LoggerMain);
            StaticAppSettings.SetObject("LoggerInfo", LoggerInfo);
            StaticAppSettings.SetObject("LoggerWarning", LoggerWarning);
            StaticAppSettings.SetObject("LoggerError", LoggerError);

            if (UseLogLayer)
                StaticLogger.CurrentLogLayer = LogLayer;
            else
                StaticLogger.CurrentLogLayer = int.MaxValue;

            if (IgnoreListMessages)
                StaticLogger.SuppressedLogs.Clear();
            else
                StaticLogger.SuppressedLogs = new HashSet<string>(SuppressedLogs);
        }

    }
}
