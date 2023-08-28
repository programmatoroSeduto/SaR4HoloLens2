using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Project.Scripts.Utils;
using Packages.DiskStorageServices.Components;
using Packages.PositionDatabase.Components;

using Packages.SAR4HL2NetworkingServices.Utils;
using Packages.SAR4HL2NetworkingServices.Components;
using Packages.SarExplorationFeatures.Components;
using Packages.StorageManager.Components;

namespace Project.Scripts.Components
{
    public class ProjectAppSettings : ProjectMonoBehaviour
    {
        [Header("Device and User Credentials")]
        public string DeviceID = "SARHL2_ID8651165355_DEVC";
        public string UserID = "SARHL2_ID8849249249_USER";
        public string ApproverUserID = "SARHL2_ID8849249249_USER";
        public string UserAccessKey = "anoth3rBr3akabl3P0sswArd";
        public string ReferencePositionID = "SARHL2_ID1234567890_REFP";

        [Header("User Data")]
        [Min(0.5f)]
        public float UserHeight = 1.85f;

        [Header("Internal Logging settings")]
        public bool PrintInfo = true;
        public bool PrintWarn = true;
        public bool PrintErr = true;
        public bool UseLogLayer = true;
        public int LogLayer = 0;
        public bool IgnoreListMessages = true;
        public List<string> SuppressedLogs = new List<string>
        {
            "Please remove the CanvasRenderer component from the [Label] GameObject as this component is no longer necessary."
        };

        [Header("Networking Settings")]
        public SarHL2Client SarServerComponent = null;
        public PositionDatabaseClientUtility DatabaseClientComponent = null;
        public bool UseTestConnection = false;
        public string SarServerURLTest = "http://localhost:5000";
        public string SarServerURL = "http://131.175.204.169/sar/";
        public bool UseConnectionTimeout = true;
        [Min(2)] public int ConnectionTimeout = 60;
        [Min(10.0f)] public float UploadTime = 60.0f;
        [Min(10.0f)] public float DownloadTime = 120.0f;
        [Min(0)] public int MaxRetryCountConnection = 10;
        [Min(0.0f)] public float ConnectionRetryDelay = 30.0f;
        [Min(1.0f)] public float UpdateRadius = 30.0f;

        [Header("Calibration Settings")]
        public bool UseStubCalibration = false;
        public CalibrationUtility CalibrationUtilityComponent = null;

        [Header("Positions Database Settings")]
        public PositionsDatabase DatabaseReference = null;
        public float BaseDistance = 0.5f;
        public float BaseHeight = 0.8f;
        public float DistanceTolerance = 0.05f;
        public GameObject ReferenceObject = null;

        [Header("Positions Low-Level Settings (Dynamic Sort)")]
        public bool UseClusters = false;
        public int ClusterSize = 4;
        public bool UseMaxIndices = false;
        public int MaxIndices = 2;

        [Header("HL2 Experience Settings")]
        public SarExplorationControlUnit ControlUnitReference = null;
        public PathDrawer DrawerReference = null;
        public MinimapStructure StructureReference = null;
        public GameObject VisualRoot = null;

        [Header("HL2 Experience: SpatialAround Feature Settings")]
        public float DrawableRadius = 10.0f;
        public float MarkerHeightPercent = 0.15f;
        public List<float> Intensities = new List<float> { 1.0f, 2.0f, 5.0f, 10.0f, 25.0f };

        [Header("Other Settings")]
        public bool DefineGlobalDebugMode = false;

        private void Start()
        {
            if(StaticAppSettings.IsEnvUWP) CustomDeviceSetup();

            SetupStaticLog();
            SetupNeworking();
            SetupCalibration();
            SetupPosDatabase();
            SetupControlUnit();
            SetupGlobalParameters();
            
            Ready(disableComponent: true);
        }

        private void CustomDeviceSetup()
        {
            UseTestConnection = false;
            DefineGlobalDebugMode = false;
            UseStubCalibration = false;
        }

        private void SetupStaticLog()
        {
            StaticLogger.PrintInfo = this.PrintInfo;
            StaticLogger.PrintWarn = this.PrintWarn;
            StaticLogger.PrintErr = this.PrintErr;
            StaticLogger.CurrentLogLayer = (UseLogLayer ? LogLayer : int.MaxValue);
            if (IgnoreListMessages)
                foreach (string s in SuppressedLogs)
                    StaticLogger.SuppressedLogs.Add(s);
        }

        private void SetupNeworking()
        {
            SarAPI.ApiURL = SarServerURL;
            if (SarServerComponent != null)
                SarAPI.Client = SarServerComponent;

            if (SarServerComponent != null)
            {
                SarServerComponent.ServerURL = UseTestConnection ? SarServerURLTest : SarServerURL;
                SarServerComponent.ConnectOnStart = false;
                SarServerComponent.ConnectionTimeout = (UseConnectionTimeout ? ConnectionTimeout : -1);
                SarServerComponent.UserId = UserID;
                SarServerComponent.UserApproverID = ApproverUserID;
                SarServerComponent.DeviceId = DeviceID;
                SarServerComponent.ReferencePositionId = ReferencePositionID;
                SarServerComponent.UserAccessKey = UserAccessKey;
            }
        }

        private void SetupCalibration()
        {
            if (CalibrationUtilityComponent != null)
            {
                StaticTransform.CalibrationComponent = CalibrationUtilityComponent;
                if(DatabaseReference != null)
                    CalibrationUtilityComponent.DatabaseReference = DatabaseReference;
            }

            if (UseStubCalibration)
                StaticTransform.SetReference(ReferencePositionID, Vector3.zero, Quaternion.identity);
        }

        private void SetupPosDatabase()
        {
            if(DatabaseReference != null)
            {
                DatabaseReference.BaseDistance = BaseDistance;
                DatabaseReference.BaseHeight = BaseHeight;
                DatabaseReference.DistanceTolerance = DistanceTolerance;
                DatabaseReference.ReferenceObject = ReferenceObject;
                DatabaseReference.UseClusters = UseClusters;
                DatabaseReference.ClusterSize = ClusterSize;
                DatabaseReference.UseMaxIndices = UseMaxIndices;
                DatabaseReference.MaxIndices = MaxIndices;
            }

            if(DatabaseClientComponent != null)
            {
                DatabaseClientComponent.InitOnStart = true;
                if (DatabaseReference != null) DatabaseClientComponent.PositionsDB = DatabaseReference;
                if (SarServerComponent != null) DatabaseClientComponent.Client = SarServerComponent;
                DatabaseClientComponent.UploadTime = UploadTime;
                DatabaseClientComponent.DownloadTime = DownloadTime;
                DatabaseClientComponent.MaxRetryCountConnection = MaxRetryCountConnection;
                DatabaseClientComponent.ConnectionRetryDelay = ConnectionRetryDelay;
                DatabaseClientComponent.UpdateRadius = UpdateRadius;
            }
        }

        private void SetupControlUnit()
        {
            if(ControlUnitReference != null)
            {
                if (DatabaseReference != null)
                    ControlUnitReference.DbReference = DatabaseReference;
                if (DrawerReference != null)
                    ControlUnitReference.DrawerReference = DrawerReference;
                if (StructureReference != null)
                    ControlUnitReference.StructureReference = StructureReference;
                if (VisualRoot != null)
                    ControlUnitReference.MarkersRoot = VisualRoot;

                ControlUnitReference.DrawableRadius = DrawableRadius;
                ControlUnitReference.Intensities = Intensities;
                ControlUnitReference.UserHeight = UserHeight;
                ControlUnitReference.MarkerHeightPercent = MarkerHeightPercent;
            }
        }

        private void SetupGlobalParameters()
        {
            StaticAppSettings.SetOpt("ReferencePositionID", ReferencePositionID);

            StaticAppSettings.SetObject("AppSettings", this);
            StaticAppSettings.SetObject("DebugMode", DefineGlobalDebugMode);
            StaticAppSettings.SetObject("UserHeight", UserHeight);
        }
    }
}
