using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Project.Scripts.Components;
using Project.Scripts.Utils;
using Packages.PositionDatabase.Components;
using Packages.PositionDatabase.Utils;

namespace Project.Scenes.SARHL2.Components
{
    public class SceneStartup : MonoBehaviour
    {
        // ===== PUBLIC ===== //

        [Header("Basic Settings")]
        [Tooltip("Scene Position Database")]
        public PositionsDatabase DatabaseReference = null;



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
            StaticTransform.SetReference(referenceName, Vector3.zero, Quaternion.identity);
        }

        private void StartupCalibration()
        {
            StaticLogger.Info(SourceLog, $"Found startup mode: {appStartupMode} USING CALIBRATION", logLayer: 2);
            DatabaseReference.enabled = false;

            StaticLogger.Info(SourceLog, $"waiting for calibration command", logLayer: 0);
        }

        private void StartupProd()
        {
            StaticLogger.Info(SourceLog, $"Found startup mode: {appStartupMode} USING PROD PROFILE", logLayer: 2);
            DatabaseReference.enabled = false;

            StaticLogger.Info(SourceLog, $"waiting for calibration command", logLayer: 0);
        }



        // ===== CALIBRATION PROCESS SUPPORT ===== //

        public void VOICE_Calibration(bool retry = false)
        {
            if (!isWaitingCalibration)
            {
                StaticLogger.Info(SourceLog, $"calibration asked, but the class is not waiting for it", logLayer: 2);
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