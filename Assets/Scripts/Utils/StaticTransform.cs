using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Project.Scripts.Components;

namespace Project.Scripts.Utils
{
    public static class StaticTransform
    {
        // ===== PUBLIC ===== //

        /// <summary> either a calibration utility is linked to the class or not </summary>
        public static CalibrationUtility CalibrationComponent = null;

        /// <summary> reference poition name (from global app settings) </summary>
        public static string ReferencePositionNameGlobal
        {
            get
            {
                posID = StaticAppSettings.GetOpt("CalibrationPositionID", "UNKNOWN");
                if (posID == "")
                    StaticLogger.Warn("GET ReferencePositionName", "WARNING: unknown reference position for calibration");
                return posID;
            }
        }

        /// <summary> set current position and get current app position </summary>
        public static Vector3 AppPosition
        {
            get
            {
                return appPos;
            }
            set
            {
                appPos = value;
            }
        }

        /// <summary> get transformed position (equals to the AppPosition if no calibation is performed) </summary>
        public static Vector3 TransformPosition
        {
            get 
            {
                if (calibrationDone || CalibrationComponent == null)
                {
                    Vector3 v = Quaternion.Inverse(refRot) * appPos - refPos;
                    StaticLogger.Info(SourceLog, $"GET TRANSFORMED POSITION : appPos({appPos}) -> transformPos({v})", logLayer: 1);
                    StaticLogger.Info(SourceLog, $"GET TRANSFORMED POSITION : refPos({refPos}) refRot({refRot.eulerAngles})", logLayer: 2);
                    return v;
                }
                else
                {
                    StaticLogger.Warn(SourceLog, "Trying to get the transformed position without calibration; returning AppPos", logLayer: 1);
                    return appPos;
                }
            }
        }



        // ===== PRIVATE ===== //

        private static Vector3 appPos = Vector3.zero;
        // the reference position (from global options)
        private static string posID = "";
        // reference position coordinates
        private static Vector3 refPos = Vector3.zero;
        // reference position rotation
        private static Quaternion refRot = Quaternion.identity;
        // only for logging
        private static string SourceLog = "StaticTransform";
        // if the calibration has been done or not (first coordinates set)
        private static bool calibrationDone = false;



        // ===== REFERENCE POSITION ===== //

        // Set the reference position with transform
        public static bool SetReference(string refPointName, Vector3 refPointPosition, Quaternion refPointRotation)
        {
            if(refPointName == "")
            {
                StaticLogger.Warn(SourceLog, "Trying to set a empty reference point name!");
                return false;
            }

            posID = StaticAppSettings.SetOpt("CalibrationPositionID", refPointName);
            refPos = refPointPosition;
            refRot = refPointRotation;

            calibrationDone = true;
            return true;
        }
    }
}
