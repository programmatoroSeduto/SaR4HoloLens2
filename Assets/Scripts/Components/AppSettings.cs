using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Project.Scripts.Utils;
using Packages.DiskStorageServices.Components;
using Packages.PositionDatabase.Components;

using Packages.SAR4HL2NetworkingServices.Utils;
using Packages.SAR4HL2NetworkingServices.Components;

namespace Project.Scripts.Components
{
    public class AppSettings : MonoBehaviour
    {
        // ===== GUI ===== //

        [Header("service settings")]
        [Tooltip("OperatorUniversalUserID (10 digits)")]
        public string OperatorUniversalUserID = "SARHL2_ID8849249249_USER";
        [Tooltip("The access key")]
        public string UserAccessKey = "anoth3rBr3akabl3P0sswArd";
        [Tooltip("Approver User ID (10 digits)")]
        public string ApproverUserID = "SARHL2_ID8849249249_USER";
        [Tooltip("OperatorUniversalDeviceID (10 digits)")]
        public string OperatorUniversalDeviceID = "SARHL2_ID8651165355_DEVC";

        [Header("Startup Settings")]
        [Tooltip("Entry Point Component")]
        public EntryPoint EntryPointComponent = null;
        [Tooltip("Calibration Position")]
        public string CalibrationPositionID = "SARHL2_ID1234567890_REFP";
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
        [Tooltip("Server GameObject Handle")]
        public SarHL2Client SarServerComponent = null;
        [Tooltip("Server IP address")]
        public string SarServerURL = "http://131.175.204.169/sar/";
        [Tooltip("Either to execute the connection on start or not (you can use the voice command in case)")]
        public bool ConnectOnStart = true;

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



        // ===== UNITY CALLBACKS ===== //


        private void Start()
        {
            running = true;
            StaticLogger.Info(this, "Starting application ... ");

#if WINDOWS_UWP
            if( 
                StartupMode != StartupModes.DeviceDevelopment || 
                StartupMode != StartupModes.DeviceDevelopmentNoCalibration || 
                StartupMode != StartupModes.DeviceProduction 
            )
            {
                StartupMode = StartupModes.DeviceProduction;
                OnValidate();
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
            SetSarServer();
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
                    // EntryPointComponent.enabled = false;
                    UseLogLayer = true;
                    SetLogObjet(true, true, true, true); // enable all logs
                    break;
                case StartupModes.DeviceDevelopment:
                    SetDebugMode(false);
                    EntryPointComponent.enabled = true;
                    UseLogLayer = true;
                    SetLogObjet(true, false, true, true, 5); // enable all logs except for info
                    break;
                case StartupModes.PcDevelopmentWithCalibration:
                    SetDebugMode(true);
                    EntryPointComponent.enabled = true;
                    UseLogLayer = true;
                    SetLogObjet(true, true, true, true); // enable all logs
                    break;
                case StartupModes.DeviceDevelopmentNoCalibration:
                    SetDebugMode(false);
                    EntryPointComponent.enabled = false;
                    UseLogLayer = true;
                    SetLogObjet(true, false, true, true, 3); // enable all logs except for info
                    break;
                case StartupModes.PcProduction:
                    SetDebugMode(false);
                    EntryPointComponent.enabled = true;
                    UseLogLayer = true;
                    SetLogObjet(true, false, true, true);
                    break;
                case StartupModes.DeviceProduction:
                    SetDebugMode(false);
                    UseLogLayer = true;
                    EntryPointComponent.enabled = true;
                    SetLogObjet(true, false, false, false, 1); // only main logging
                    break;
            }
        }

        public void SetLogObjet(bool lMain=true, bool lInfo = true, bool lWarn=true, bool lError = true, int layer = int.MaxValue)
        {
            LoggerMain.gameObject.SetActive(lMain);
            LoggerInfo.gameObject.SetActive(lInfo);
            StaticLogger.PrintInfo = ( lMain || lInfo ); 

            LoggerWarning.gameObject.SetActive(lWarn);
            StaticLogger.PrintWarn = lWarn;

            LoggerError.gameObject.SetActive(lError);
            StaticLogger.PrintErr = true;

            UseLogLayer = (layer == int.MaxValue);
            if (UseLogLayer)
                StaticLogger.CurrentLogLayer = layer;
            else
                StaticLogger.CurrentLogLayer = int.MaxValue;
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

        public void SetSarServer(string url = "")
        {
            if (url == "")
                url = SarServerURL;
            else
                SarServerURL = url;

            StaticAppSettings.SetOpt("SarServerURL", SarServerURL);
            if(SarServerComponent != null)
            {
                SarServerComponent.ServerURL = this.SarServerURL;
                SarServerComponent.UserId = this.OperatorUniversalUserID;
                SarServerComponent.UserApproverID = this.ApproverUserID;
                SarServerComponent.UserAccessKey = this.UserAccessKey;
                SarServerComponent.ConnectOnStart = ConnectOnStart;
                SarServerComponent.DeviceId = this.OperatorUniversalDeviceID;
                SarServerComponent.ReferencePositionId = this.CalibrationPositionID;

                StaticAppSettings.SetObject("SarServerComponent", SarServerComponent);
                SarAPI.Client = SarServerComponent;
            }
        }
    }
}