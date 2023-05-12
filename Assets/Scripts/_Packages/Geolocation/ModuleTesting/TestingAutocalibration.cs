using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Packages.DoublePrecisionGeometry.Utils;
using Packages.StorageUtilities.Components;
using Packages.Geolocation.Components;
using Packages.Geolocation.Types;
using Packages.Geolocation.Utils;

namespace Packages.Geolocation.ModuleTesting
{
    public class TestingAutocalibration : GeolocationPointReaderType
    {
        [Tooltip("The module to test")]
        public GeolocationAutocalibrating ModuleReference = null;

        [Tooltip("Test Period")]
        public float TestPeriod = 2.0f;

        [Tooltip("Name of the output file")]
        public string CsvFileName = "";



        private CSVFileWriter loggerComponent = null;
        private Coroutine COR_MainTestCycle = null;
        private Coroutine COR_UpdateLogger = null;
        private int lastIndex = -1;
        private GeolocationPoint absoluteMeasurement = null;





        private void Start()
        {
            if(ModuleReference == null)
            {
                Debug.LogWarning("[test] no geolocation module provided!");
                return;
            }

            loggerComponent = CreateLogger(CsvFileName, new List<string>
            {
                "FROM_RELATIVE_FL",
                "LAT", "LON", "ALT",
                "WX", "WY", "WZ",
                "UX", "UY", "UZ"
            });

            Debug.Log("[test] Starting test");
            COR_MainTestCycle = StartCoroutine(ORCOR_MainTestCycle());
        }

        public override void EVENT_ReadGeopoint(GeolocationPoint gp)
        {
            Debug.Log("[test] (from 'EVENT_ReadGeopoint') received new measurement");
            absoluteMeasurement = gp;
            Debug.Log($"{gp}");
        }








        private IEnumerator ORCOR_MainTestCycle()
        {
            yield return null;

            while(true)
            {
                yield return new WaitForSecondsRealtime(TestPeriod);
                if (absoluteMeasurement == null) continue;

                Debug.Log($"[test] check at {DateTime.Now}");
                if (absoluteMeasurement.MasurementCounter != lastIndex)
                {
                    Debug.Log("[test] New absolute measurement!");

                    absoluteMeasurement.WorldRealPoint = GeolocationPoint.PolarToCartesian(absoluteMeasurement.GeoRealCoordinates);

                    List<string> csv = new List<string>();
                    csv.Add(" "); // not relative
                    csv.Add($"{absoluteMeasurement.GeoRealCoordinates.x}"); // lat
                    csv.Add($"{absoluteMeasurement.GeoRealCoordinates.y}"); // lon
                    csv.Add($"{absoluteMeasurement.GeoRealCoordinates.z}"); // alt
                    csv.Add($"{absoluteMeasurement.WorldRealPoint.x}"); // wx
                    csv.Add($"{absoluteMeasurement.WorldRealPoint.y}"); // wy
                    csv.Add($"{absoluteMeasurement.WorldRealPoint.z}"); // wx
                    csv.Add($"{absoluteMeasurement.UnityRealPoint.x}"); // ux
                    csv.Add($"{absoluteMeasurement.UnityRealPoint.y}"); // uy
                    csv.Add($"{absoluteMeasurement.UnityRealPoint.z}"); // ux

                    yield return BSCOR_UpdateLogger(loggerComponent, csv);
                    lastIndex = absoluteMeasurement.MasurementCounter;
                }
                else if(ModuleReference.EVENT_IsCalibrationStructReady())
                {
                    Debug.Log("[test] New relative position");
                    GeolocationTransform cal = ModuleReference.EVENT_GetCalibrationResults();
                    
                    GeolocationPoint rgp = new GeolocationPoint();
                    rgp.UnityRealPoint = DVector3.FromUnity(Camera.main.transform.position);
                    rgp.WorldPoint = cal.UnityToWorld(rgp.UnityRealPoint);
                    rgp.GeoCoordinates = cal.UnityToPolar(rgp.UnityRealPoint);

                    List<string> csv = new List<string>();
                    csv.Add("X"); //  is relative
                    csv.Add($"{rgp.GeoCoordinates.x}"); // lat
                    csv.Add($"{rgp.GeoCoordinates.y}"); // lon
                    csv.Add($"{rgp.GeoCoordinates.z}"); // alt
                    csv.Add($"{rgp.WorldPoint.x}"); // wx
                    csv.Add($"{rgp.WorldPoint.y}"); // wy
                    csv.Add($"{rgp.WorldPoint.z}"); // wx
                    csv.Add($"{rgp.UnityRealPoint.x}"); // ux
                    csv.Add($"{rgp.UnityRealPoint.y}"); // uy
                    csv.Add($"{rgp.UnityRealPoint.z}"); // ux

                    yield return BSCOR_UpdateLogger(loggerComponent, csv);
                }
            }
        }

        private CSVFileWriter CreateLogger(string fileName, List<string> fields)
        {
            Debug.Log($"Creating logger to file '{fileName}' ... ");
            CSVFileWriter logger = gameObject.AddComponent<CSVFileWriter>();

            logger.FileName = fileName;
            logger.CSVFields = fields;
            logger.UseTimestamp = true;
            logger.ApplyTimestampColumn = true;
            logger.ApplyDurationColumn = true;
            logger.ApplyCounter = true;

            logger.EVENT_CreateFile();

            Debug.Log($"Creating logger to file '{fileName}' ... OK");
            return logger;
        }

        private IEnumerator BSCOR_UpdateLogger(CSVFileWriter logger, List<string> fields)
        {
            yield return null;

            if (logger.EVENT_IsEnabled())
                logger.EVENT_WriteCsv(fields);
        }
    }
}
