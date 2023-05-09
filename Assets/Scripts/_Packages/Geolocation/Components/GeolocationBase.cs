//define WINDOWS_UWP

using System;
using System.Collections;
using System.Collections.Generic;

using System.Threading.Tasks;

using UnityEngine;

using Packages.StorageUtilities.Types;
using Packages.StorageUtilities.Components;
using Packages.DoublePrecisionGeometry.Utils;

#if WINDOWS_UWP
using Windows;
using Windows.Devices;
using Windows.Devices.Geolocation;
#endif

namespace Packages.Geolocation.Components
{
    public class GeolocationBase : MonoBehaviour
    {
        [Tooltip("If to use or not High Accuracy in Geolocation module")]
        public bool HighAccuracy = false;

        [Tooltip("Export data into CSV file; the component is automatically created by the module")]
        public bool UseCsvDataExport = false;

        [Tooltip("Use a strict check on the data: check if the received data from the module are equals to the previously obtained one, and in case, don't return them")]
        public bool CheckEqualityWithPreviousMeasurements = false;

        [Tooltip("CSV raw file name")]
        public string CsvFileName = "geolocation_raw_obj";

        [Tooltip("CSV data file name")]
        public string CsvDataFileName = "geolocation_data";



        private bool authorized = false;
        private bool runningGeolocalization = false;
        private DVector3 currentUnityPos = null;
        private DateTime tstamp;
        private CSVFileWriter loggerRaw = null;
        private CSVFileWriter loggerData = null;
        private Coroutine COR_Geolocalization = null;

#if WINDOWS_UWP
    private Geoposition gpPrev = null;
    private Geoposition gp;
#endif




        private void Start()
        {
            // check authorization for the capability
            authorized = UWP_CheckAuthorization();
            if (!authorized)
            {
                Debug.LogWarning("[GeolocationBase] ERROR: Unauthorized");
                return;
            }

            // create the log file
            if (UseCsvDataExport)
            {
                loggerRaw = CreateLogger(CsvFileName, new List<string>
            {
                "gp.Coordinate.Accuracy",
                "gp.Coordinate.Heading",
                "gp.Coordinate.PositionSource",
                "gp.Coordinate.PositionSourceTimestamp",
                "gp.Coordinate.Speed",
                "gp.Coordinate.Point.AltitudeReferenceSystem",
                "gp.Coordinate.Point.Position.Latitude",
                "gp.Coordinate.Point.Position.Longitude",
                "gp.Coordinate.Point.Position.Altitude"
            });

                loggerData = CreateLogger(CsvDataFileName, new List<string>
            {
                "LATITUDE", "LONGITUDE", "ALTITUDE",
                "UNITY_X", "UNITY_Y", "UNITY_Z"
            });
            }

            Debug.Log("Starting geolocation...");
            //COR_Geolocalization = StartCoroutine(ORCOR_Geolocalization());
        }

        // Update is called once per frame
        void Update()
        {
            if (authorized && !runningGeolocalization)
            {
                COR_Geolocalization = StartCoroutine(ORCOR_Geolocalization());
            }
        }







        private IEnumerator ORCOR_Geolocalization()
        {
            yield return null;
#if WINDOWS_UWP
        runningGeolocalization = true;

        Geolocator geolocator = new Geolocator
        {
            DesiredAccuracy = (HighAccuracy ? PositionAccuracy.High : PositionAccuracy.Default)
        };
        PositionStatus currentStatus = PositionStatus.Ready;
        geolocator.StatusChanged += (Geolocator g, StatusChangedEventArgs e) => {
            currentStatus = e.Status;
        };

        Task<Geoposition> geolocalizationTask = geolocator.GetGeopositionAsync().AsTask();
        while (!geolocalizationTask.IsCompleted)
            yield return new WaitForEndOfFrame();
        tstamp = DateTime.Now;
        currentUnityPos = DVector3.FromUnity(Camera.main.transform.position);
        gp = geolocalizationTask.Result;

        if (!UWP_ValidatePosition())
        {
            Debug.LogWarning("WARNING: unvalid position from module");
            runningGeolocalization = false;
            yield break;
        }
        else
            Debug.LogWarning("Found valid position from module");

        if (UseCsvDataExport)
            yield return BSCOR_UpdateLoggers();

        runningGeolocalization = false;
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



        private bool UWP_ValidatePosition()
        {
            if (!CheckEqualityWithPreviousMeasurements)
                return true;

#if WINDOWS_UWP
        if(gpPrev == null)
        {
            gpPrev = gp;
            return true;
        }

        if (gp.Coordinate.Point.Position.Latitude == gpPrev.Coordinate.Point.Position.Latitude
            && gp.Coordinate.Point.Position.Longitude == gpPrev.Coordinate.Point.Position.Longitude
            && gp.Coordinate.Point.Position.Altitude == gpPrev.Coordinate.Point.Position.Altitude)
            return false;
        else
        {
            gpPrev = gp;
            return true;
        }
#else
            return true;
#endif
        }


        private IEnumerator BSCOR_UpdateLoggers()
        {
            yield return null;
#if WINDOWS_UWP
        Debug.Log("Updating logs...");

        while (!loggerRaw.EVENT_IsReadyForOutput())
            yield return new WaitForEndOfFrame();
        loggerRaw.EVENT_ReadCSVRow(new List<string>
        {
            gp.Coordinate.Accuracy.ToString(),
            ( gp.Coordinate.Heading != null ? gp.Coordinate.Heading.ToString() : "" ),
            gp.Coordinate.PositionSource.ToString(),
            ( gp.Coordinate.PositionSourceTimestamp != null ? gp.Coordinate.PositionSourceTimestamp.ToString() : ""),
            ( gp.Coordinate.Speed != null ? gp.Coordinate.Speed.ToString() : ""),
            gp.Coordinate.Point.AltitudeReferenceSystem.ToString(),
            gp.Coordinate.Point.Position.Latitude.ToString(),
            gp.Coordinate.Point.Position.Longitude.ToString(),
            gp.Coordinate.Point.Position.Altitude.ToString()
        });
        while (!loggerRaw.EVENT_IsReadyForOutput())
            yield return new WaitForEndOfFrame();


        while(!loggerData.EVENT_IsReadyForOutput())
            yield return new WaitForEndOfFrame();
        loggerData.EVENT_ReadCSVRow(new List<string>
        {
            gp.Coordinate.Point.Position.Latitude.ToString(),
            gp.Coordinate.Point.Position.Longitude.ToString(),
            gp.Coordinate.Point.Position.Altitude.ToString(),
            currentUnityPos.x.ToString(),
            currentUnityPos.y.ToString(),
            currentUnityPos.z.ToString()
        });
        while (!loggerData.EVENT_IsReadyForOutput())
            yield return new WaitForEndOfFrame();
#endif
        }

        private CSVFileWriter CreateLogger(string fileName, List<string> fields)
        {
            Debug.Log($"Creating logger to file '{fileName}' ... ");
            CSVFileWriter logger = gameObject.AddComponent<CSVFileWriter>();

            logger.FileName = fileName;
            logger.CSVFields = fields;
            logger.ApplyTimestampColumn = true;
            logger.ApplyDurationColumn = true;
            logger.ApplyCounter = true;

            logger.EVENT_CreateFile();

            Debug.Log($"Creating logger to file '{fileName}' ... OK");
            return logger;
        }
    }

}