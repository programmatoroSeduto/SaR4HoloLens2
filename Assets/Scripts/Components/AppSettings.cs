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

        [Header("Startup Settings")]
        [Tooltip("Entry Point Component")]
        public EntryPoint EntryPointComponent = null;
        [Tooltip("Calibration Position")]
        public string CalibrationPositionID = "SARHL2_ID00000000_REFPOS";
        [Tooltip("Startup mode")]
        public StartupModes StartupMode = StartupModes.PcDevelopment;
        [Tooltip("The component in charge to perform the calibration")]
        public CalibrationUtility CalibrationUnit = null;

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
        [Header("other Settings")]
        [Tooltip("The debug mode allows to develop in editor and to test the application using a particular context on the device during the development of the application")]
        public bool DebugMode = false;

        // global objects
        [Header("Global Objects")]
        [Header("Storage hub if defined")]
        public StorageHubOneShot StorageHub = null;
        [Header("Global position database if defined")]
        public PositionsDatabase PositionsDatabaseReference = null;

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



        // ===== PUBLIC ===== //

        // set reference to position database
        public PositionsDatabase PositionsDatabase
        {
            get
            {
                PositionsDatabase db = StaticAppSettings.GetObject("PositionsDatabase", null) as PositionsDatabase;
                if (db != null)
                    return db;
                else if (PositionsDatabaseReference != null)
                {
                    PositionsDatabase = PositionsDatabaseReference;
                    return PositionsDatabaseReference;
                }
                else
                    return null;
            }
            set
            {
                StaticAppSettings.SetObject("PositionsDatabase", value);
                PositionsDatabaseReference = value;
            }
        }



        // ===== PRIVATE ===== //

        // variables set done? 
        private bool setEnvDone = false;
        // is the component running?
        private bool running = false;
        // PC dev position ID
        private readonly string pcDevPosID = "SARHL2_ID90909091_REFPOS";
        // pc calibration position ID default
        private readonly string pcDevCalibPosID = "SARHL2_ID06600660_REFPOS";
        // device calibration pos ID
        private readonly string deviceCalibPosID = "SARHL2_ID12700385_REFPOS";
        // device without calibration pos ID
        private readonly string deviceNoCalibPosID = "SARHL2_66660000_REFPOS";
        // production refpos (is stands for "ask to the server pls")
        private readonly string prodDevicePosID = "SARHL2_ID00000000_REFPOS";



        // ===== UNITY CALLBACKS ===== //


        private void Start()
        {
            running = true;
            StaticLogger.Info(this, "Starting application ... ");

#if RUNNING_ON_DEVICE
            switch (StartupMode)
            {
                case StartupModes.Undefined:
                    StartupMode = StartupModes.DeviceDevelopment;
                    break;
                case StartupModes.PcDevelopment:
                    StartupMode = StartupModes.DeviceDevelopment;
                    break;
                case StartupModes.DeviceDevelopment:
                    break;
                case StartupModes.PcDevelopmentWithCalibration:
                    StartupMode = StartupModes.DeviceDevelopment;
                    break;
                case StartupModes.DeviceDevelopmentNoCalibration:
                    break;
                case StartupModes.DeviceProduction:
                    break;
            }      
#endif


            SetEnvironment();
            setEnvDone = true;
        }

        private void Update()
        {
            
        }

        private void OnDestroy()
        {
            StaticLogger.Info(this, "Closing application ... ");
            running = false;
        }



        // ===== ENVIRONMENT SETUP ===== //

        public void SetEnvironment()
        {
            if (UseLogLayer)
                StaticLogger.CurrentLogLayer = LogLayer;
            else
                StaticLogger.CurrentLogLayer = int.MaxValue;

            if (IgnoreListMessages)
                StaticLogger.SuppressedLogs.Clear();
            else
                StaticLogger.SuppressedLogs = new HashSet<string>(SuppressedLogs);

            StaticLogger.Info(this, "Setting up environment ... ");

            StaticAppSettings.AppSettings = this;

            StaticAppSettings.SetOpt("OperatorUniversalUserID", OperatorUniversalUserID);
            StaticAppSettings.SetOpt("OperatorUniversalDeviceID", OperatorUniversalDeviceID);

            StaticAppSettings.SetObject("EntryPointComponent", EntryPointComponent);
            SetCalibrationPositionID(CalibrationPositionID);
            StaticAppSettings.SetObject("StartupMode", StartupMode);
            SetStartupMode();
            StaticAppSettings.SetObject("CalibrationUnit", CalibrationUnit);
            StaticTransform.CalibrationComponent = CalibrationUnit;

            StaticAppSettings.SetOpt("UserHeight", UserHeight.ToString());
            StaticAppSettings.SetOpt("ServerIpAddress", ServerIpAddress);
            StaticAppSettings.SetOpt("ServerPortNo", ServerPortNo);
            SetDebugMode(DebugMode, forceChange: true);

            StaticAppSettings.SetObject("StorageHub", StorageHub);
            StaticAppSettings.SetObject("PositionsDatabase", PositionsDatabaseReference);

            StaticAppSettings.SetObject("LoggerMain", LoggerMain);
            StaticAppSettings.SetObject("LoggerInfo", LoggerInfo);
            StaticAppSettings.SetObject("LoggerWarning", LoggerWarning);
            StaticAppSettings.SetObject("LoggerError", LoggerError);

            StaticLogger.Info(this, "Setting up environment ... OK ");
        }



        // ===== GUI CHANGE ===== //

        [ExecuteInEditMode]
        public void OnValidate()
        {
            if (running) return;

            SetStartupMode();
        }

        public void SetStartupMode()
        {
            switch (StartupMode)
            {
                case StartupModes.Undefined:
                    break;
                case StartupModes.PcDevelopment:
                    SetDebugMode(true);
                    SetCalibrationPositionID(pcDevPosID);
                    EntryPointComponent.enabled = false;
                    break;
                case StartupModes.DeviceDevelopment:
                    SetDebugMode(true);
                    SetCalibrationPositionID(deviceCalibPosID);
                    EntryPointComponent.enabled = true;
                    break;
                case StartupModes.PcDevelopmentWithCalibration:
                    SetDebugMode(true);
                    SetCalibrationPositionID(pcDevCalibPosID);
                    EntryPointComponent.enabled = true;
                    break;
                case StartupModes.DeviceDevelopmentNoCalibration:
                    SetDebugMode(true);
                    SetCalibrationPositionID(deviceNoCalibPosID);
                    EntryPointComponent.enabled = false;
                    break;
                case StartupModes.PcProduction:
                    SetDebugMode(false);
                    SetCalibrationPositionID(pcDevCalibPosID);
                    EntryPointComponent.enabled = true;
                    break;
                case StartupModes.DeviceProduction:
                    SetDebugMode(false);
                    SetCalibrationPositionID(prodDevicePosID);
                    EntryPointComponent.enabled = true;
                    break;
            }
        }

        public void SetDebugMode(bool opt, bool forceChange = false)
        {
            if(opt && (!DebugMode || forceChange))
            {
                DebugMode = true;
                if(running) 
                    StaticAppSettings.SetOpt("IsDebugMode", "true");
            }
            else if (!opt && (DebugMode || forceChange))
            {
                DebugMode = false;
                if (running)
                    StaticAppSettings.SetOpt("IsDebugMode", "false");
            }
        }

        public void SetCalibrationPositionID(string id)
        {
            CalibrationPositionID = id;
            if (running)
                StaticAppSettings.SetOpt("CalibrationPositionID", id);
        }
    }
}