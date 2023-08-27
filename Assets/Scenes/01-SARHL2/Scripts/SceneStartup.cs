using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Project.Scripts.Components;
using Project.Scripts.Utils;
using Packages.PositionDatabase.Components;
using Packages.PositionDatabase.Utils;
using Packages.SAR4HL2NetworkingServices.Components;
using Packages.SAR4HL2NetworkingServices.Utils;

namespace Project.Scenes.SARHL2.Components
{
    public class SceneStartup : MonoBehaviour
    {
        // ===== PUBLIC ===== //

        [Header("Basic Settings")]
        [Tooltip("Scene Position Database")]
        public PositionsDatabase DatabaseReference = null;
        [Tooltip("Reference to the client")]
        public PositionDatabaseClientUtility PosdbClientReference = null;



        // ===== PRIVATE ===== //

        // the startup mode from AppSettings
        private StartupModes appStartupMode = StartupModes.PcDevelopment;
        // used for logging
        private string SourceLog = "";
        // calibration unit from the global settings
        private CalibrationUtility calibrationUnit = null;
        // reference system name
        private string referenceName = "";
        // if the class is waiting calibration or not
        private bool isWaitingCalibration = true;



        // ===== UNITY CALLBACKS ===== //

        // Start is called before the first frame update
        void Start()
        {
            if(DatabaseReference == null)
            {
                StaticLogger.Err(SourceLog, "no database reference provided!  leaving scene unset");
                return;
            }

            appStartupMode = (StartupModes) StaticAppSettings.GetObject("StartupMode", StartupModes.Undefined);
            SourceLog = $"SceneStartup: {gameObject.scene.name}";
            StaticLogger.Info(SourceLog, $"Found startup mode: {appStartupMode}", logLayer: 1);

            calibrationUnit = (CalibrationUtility) StaticAppSettings.GetObject("CalibrationUnit", null);
            if (calibrationUnit == null)
            {
                StaticLogger.Err(SourceLog, "no calibration unit defined! Unable to start scene; leaving scene unset");
                return;
            }

            if(PosdbClientReference != null && !PosdbClientReference.InitOnStart)
            {
                SarHL2Client clientComponent = (SarHL2Client)StaticAppSettings.GetObject("SarServerComponent", null);
                if(clientComponent == null)
                {
                    StaticLogger.Err(SourceLog, "SarHL2Client reference not found in the main settings");
                    Debug.LogWarning("SarHL2Client reference is null");
                }
                else
                {
                    StaticLogger.Info(SourceLog, "SarHL2Client reference is not null");
                    Debug.Log("SarHL2Client reference is not null");
                }
                PosdbClientReference.Client = clientComponent;
                if(!PosdbClientReference.TryInit())
                {
                    StaticLogger.Warn(SourceLog, "SarHL2Client: unable to init the component");
                }
            }
            

            referenceName = StaticAppSettings.GetOpt("CalibrationPositionID", "UNKNOWN");
            if(referenceName == "UNKNOWN")
            {
                StaticLogger.Warn(SourceLog, "no reference system name set!", logLayer: 1);
            }

            StaticAppSettings.AppSettings.PositionsDatabase = DatabaseReference;
            switch (appStartupMode)
            {
                case StartupModes.Undefined:
                    StartupDefault();
                    break;
                case StartupModes.PcDevelopment:
                    StartupDefault();
                    break;
                case StartupModes.DeviceDevelopment:
                    StartupCalibration();
                    break;
                case StartupModes.PcDevelopmentWithCalibration:
                    StartupCalibration();
                    break;
                case StartupModes.DeviceDevelopmentNoCalibration:
                    StartupDefault();
                    break;
                case StartupModes.PcProduction:
                    StartupProd();
                    break;
                case StartupModes.DeviceProduction:
                    StartupProd();
                    break;
            }
        }



        // ===== CUSTOM SCENE STARTUP ===== //

        private void StartupDefault()
        {
            StaticLogger.Info(SourceLog, $"Found startup mode: {appStartupMode} USING DEFAULT", logLayer: 2);
            DatabaseReference.enabled = true;
            isWaitingCalibration = false;

            StaticLogger.Info(SourceLog, $"setting stub calibration infos", logLayer: 0);
            if(!StaticTransform.SetReference(referenceName, Vector3.zero, Quaternion.identity))
            {
                StaticLogger.Warn(SourceLog, "Cannot set calibration!");
                return;
            }
        }

        private void StartupCalibration()
        {
            StaticLogger.Info(SourceLog, $"Found startup mode: {appStartupMode} USING CALIBRATION", logLayer: 2);
            DatabaseReference.enabled = false;
            isWaitingCalibration = true;

            StaticLogger.Info(SourceLog, $"waiting for calibration command", logLayer: 0);
        }

        private void StartupProd()
        {
            StaticLogger.Info(SourceLog, $"Found startup mode: {appStartupMode} USING PROD PROFILE", logLayer: 2);
            DatabaseReference.enabled = false;
            isWaitingCalibration = true;

            StaticLogger.Info(SourceLog, $"waiting for calibration command", logLayer: 0);
        }



        // ===== CALIBRATION PROCESS SUPPORT ===== //

        public void VOICE_Calibration(bool retry = false)
        {
            if (!isWaitingCalibration && !retry)
            {
                StaticLogger.Warn(SourceLog, $"calibration asked, but the class is not waiting for it", logLayer: 2);
                return;
            }

            if(calibrationUnit == null)
            {
                StaticLogger.Err(SourceLog, "no calibration unit defined! Unable to start calibration from VOICE_Calibration()");
                return;
            }

            calibrationUnit.EVENT_Calibrate(retry);
        }
    }
}