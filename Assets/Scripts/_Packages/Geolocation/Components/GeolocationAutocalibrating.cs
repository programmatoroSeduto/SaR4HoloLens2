//define WINDOWS_UWP

using System;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Packages.StorageUtilities.Types;
using Packages.StorageUtilities.Components;
using Packages.DoublePrecisionGeometry.Utils;
using Packages.Geolocation.Utils;
using Packages.Geolocation.Types;

#if WINDOWS_UWP
using Windows;
using Windows.Devices;
using Windows.Devices.Geolocation;
#endif

namespace Packages.Geolocation.Components
{

    public class GeolocationAutocalibrating : MonoBehaviour
    {
        [Header("Geolocation Base Settings")]
        
        [Tooltip("If to use or not High Accuracy in Geolocation module")]
        public bool HighAccuracy = false;



        [Header("Continuous Geolocation Settings")]
        
        [Tooltip("Enable continuous geolocation; it disables the event GeolocationSpinOnce")]
        public bool UseContinuousGeolocation = false;

        [Tooltip("Minimum time interval between one geolocation and another one")]
        public float MinimumTimeToNextMeasurement = 5.0f;




        [Header("Calibration Settings")]

        [Tooltip("Recompute Calibration if abs distance error is too much between Unity path and World path")]
        public bool UseRecomputeCalibration = false;

        [Tooltip("Maximum error, in absolute value, on the difference between the lenght of the reference path in the world frame and the one in the Unity frame")]
        public double CalibrationTollerance = 0.5f;




        [Header("Output Settings")]

        [Tooltip("Objects to call when a new position is successfully obtained from continuous geolocation")]
        public List<GeolocationPointReaderType> SendToObjects = new List<GeolocationPointReaderType>();







        private bool authorized = false;
        private bool runningGeolocalization = false;
        private bool runningContinuousGeolocalization = false;
        private Coroutine COR_ContinuousGeolocalizationTask = null;
        private Coroutine COR_GeolocalizationTask = null;
        private GeolocationPoint lastMeasurement = null;
        private bool calibrationRunning = false;
        private GeolocationTransform calibrationResult = null;
        private int measurementCounter = 0;

#if WINDOWS_UWP
        private PositionStatus geolocalizationFeatureStatus;
        private Geoposition prevPosition = null;
        private Geoposition currPosition = null;
#endif




        private void Start()
        {
            authorized = UWP_CheckAuthorization();
            if (!authorized)
            {
                Debug.LogWarning("[GeolocationBase] ERROR: Unauthorized");
                return;
            }
        }

        private void Update()
        {
            if (UseContinuousGeolocation && authorized && !runningContinuousGeolocalization)
            {
                COR_ContinuousGeolocalizationTask = StartCoroutine(ORCOR_ContinuousGeolocation());
            }
        }

        public bool EVENT_RequireAuthotization()
        {
            authorized = UWP_CheckAuthorization();
            if (!authorized)
            {
                Debug.LogWarning("[GeolocationBase] ERROR: Unauthorized");
                return false;
            }
            else
                return true;
        }

        public bool EVENT_GeolocationSpinOnce()
        {
            if (UseContinuousGeolocation) return false;

            if(!authorized)
            {
                authorized = UWP_CheckAuthorization();
                if (!authorized)
                {
                    Debug.LogWarning("[GeolocationBase] ERROR: Unauthorized");
                    return false;
                }
            }
            if (authorized && !runningGeolocalization)
            {
                COR_GeolocalizationTask = StartCoroutine(BSCOR_Geolocalization());
                return true;
            }
            else
                return false;
        }

        public bool EVENT_IsRunningGeolocation()
        {
            return runningGeolocalization;
        }

        public bool EVENT_IsAvailableGeolocation()
        {
            return authorized;
        }

        public GeolocationPoint EVENT_GetLastResults()
        {
            return lastMeasurement;
        }

        public bool EVENT_IsCalibrationStructReady()
        {
            return (calibrationRunning == false && calibrationResult != null);
        }

        public GeolocationTransform EVENT_GetCalibrationResults()
        {
            if (calibrationRunning == false && calibrationResult != null)
                return calibrationResult;
            else
                return null;
        }

        public void EVENT_CalibrationRedo()
        {
            calibrationResult = null;
            calibrationRunning = false;
        }


















        private IEnumerator ORCOR_ContinuousGeolocation()
        {
            yield return null;

#if WINDOWS_UWP
            Debug.Log($"Continuous Geolocation Starting at {DateTime.Now}");
            runningContinuousGeolocalization = true;
            while (authorized && UseContinuousGeolocation)
            {
                yield return BSCOR_Geolocalization();
                yield return BSCOR_UpdateGeopositionReaders();
                yield return new WaitForSecondsRealtime(MinimumTimeToNextMeasurement);
            }
            runningContinuousGeolocalization = false;
            Debug.Log($"Continuous Geolocation Stop at {DateTime.Now}");
#endif
        }

        private IEnumerator BSCOR_Geolocalization()
        {
            yield return null;

#if WINDOWS_UWP
            runningGeolocalization = true;

            do
            {
                Geolocator geolocator = new Geolocator
                {
                    DesiredAccuracy = (HighAccuracy ? PositionAccuracy.High : PositionAccuracy.Default)
                };
                geolocator.StatusChanged += (Geolocator g, StatusChangedEventArgs e) => {
                    geolocalizationFeatureStatus = e.Status;
                };

                Task<Geoposition> geolocalizationTask = geolocator.GetGeopositionAsync().AsTask();
                while (!geolocalizationTask.IsCompleted)
                    yield return new WaitForEndOfFrame();
                
                currPosition = geolocalizationTask.Result;
                yield return new WaitForEndOfFrame();
            }
            while(!UWP_ValidatePosition());
            Debug.Log($"Acquired position no. {measurementCounter}");

            lastMeasurement = GeolocationPoint.FromAbsoluteGeolocation(
                new DVector3(
                    currPosition.Coordinate.Point.Position.Latitude,
                    currPosition.Coordinate.Point.Position.Longitude,
                    GeolocationPoint.EarthRadius + currPosition.Coordinate.Point.Position.Altitude
                    ),
                DVector3.FromUnity(Camera.main.transform.position)
                );
            lastMeasurement.MasurementCounter = measurementCounter;
            measurementCounter++;

            CalibrationStep();

            runningGeolocalization = false;
#endif
        }

        private IEnumerator BSCOR_UpdateGeopositionReaders()
        { 
            if(SendToObjects.Count > 0)
                foreach (GeolocationPointReaderType reader in SendToObjects)
                {
                    reader.EVENT_ReadGeopoint(lastMeasurement);
                    yield return new WaitForEndOfFrame();
                }
        }







        private bool UWP_ValidatePosition()
        {
#if WINDOWS_UWP
            if (prevPosition == null)
            {
                prevPosition = currPosition;
                return true;
            }

            if (currPosition.Coordinate.Point.Position.Latitude == prevPosition.Coordinate.Point.Position.Latitude
                && currPosition.Coordinate.Point.Position.Longitude == prevPosition.Coordinate.Point.Position.Longitude
                && currPosition.Coordinate.Point.Position.Altitude == prevPosition.Coordinate.Point.Position.Altitude)
                return false;
            else
            {
                prevPosition = currPosition;
                return true;
            }
#else
            return true;
#endif
        }

        private bool UWP_CheckAuthorization()
        {
#if WINDOWS_UWP
            GeolocationAccessStatus access = Geolocator.RequestAccessAsync().AsTask().GetAwaiter().GetResult();
            return (access == GeolocationAccessStatus.Allowed);
#else
            return false;
#endif
        }

        private void CalibrationStep()
        {
            if(!calibrationRunning && calibrationResult == null)
            {
                calibrationRunning = true;

                calibrationResult = new GeolocationTransform();
                calibrationResult.g1deg = lastMeasurement.GeoRealCoordinates;
                calibrationResult.wP1 = GeolocationPoint.PolarToCartesian(calibrationResult.g1deg);
                calibrationResult.uP1 = lastMeasurement.UnityRealPoint;

                // Debug.Log($"Calibration process STEP 1 started at {DateTime.Now}\n{calibrationResult}");
            }
            else if(calibrationRunning && calibrationResult != null)
            {
                calibrationResult.g2deg = lastMeasurement.GeoRealCoordinates;
                calibrationResult.wP2 = GeolocationPoint.PolarToCartesian(calibrationResult.g2deg);
                calibrationResult.uP2 = lastMeasurement.UnityRealPoint;

                // Debug.Log($"STEP 2 : {calibrationResult}");
                calibrationResult.CalibrationStep();
                
                Debug.Log($"Calibration process STEP 2 started at {DateTime.Now}\n{calibrationResult}");

                if(UseRecomputeCalibration && !calibrationResult.CheckCalibrationAbsDistanceError(CalibrationTollerance))
                {
                    Debug.LogWarning("WARNING: Calibration distance check failed; repeating calibration...");
                    EVENT_CalibrationRedo();
                    return;
                }

                calibrationRunning = false;
            }
        }
    }
}
