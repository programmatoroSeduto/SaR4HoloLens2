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
        public GeolocationAutocalibrating ModuleReference;

        [Tooltip("Name of the output file")]
        public string CsvFileName = "";



        private CSVFileWriter loggerComponent = null;
        private Coroutine COR_MainTestCycle = null;
        private Coroutine COR_UpdateLogger = null;
        private int lastIndex = -1;
        private GeolocationPoint absoluteMeasurement = null;





        private void Start()
        {
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
            absoluteMeasurement = gp;
        }








        private IEnumerator ORCOR_MainTestCycle()
        {
            yield return null;

            while(true)
            {
                yield return new WaitForSecondsRealtime(1.0f);
                if (absoluteMeasurement == null) continue;
                
                if (absoluteMeasurement.MasurementCounter != lastIndex)
                {
                    Debug.Log("[test] New absolute measurement!");

                    absoluteMeasurement.WorldPoint = GeolocationPoint.PolarToCartesian(absoluteMeasurement.GeoRealCoordinates);

                    List<string> csv = new List<string>();
                    csv.Add(" "); // not relative
                    csv.Add($"{absoluteMeasurement.GeoRealCoordinates.x}"); // lat
                    csv.Add($"{absoluteMeasurement.GeoRealCoordinates.y}"); // lon
                    csv.Add($"{absoluteMeasurement.GeoRealCoordinates.z}"); // alt
                    csv.Add($"{absoluteMeasurement.WorldPoint.x}"); // wx
                    csv.Add($"{absoluteMeasurement.WorldPoint.y}"); // wy
                    csv.Add($"{absoluteMeasurement.WorldPoint.z}"); // wx
                    csv.Add($"{absoluteMeasurement.UnityRealPoint.x}"); // ux
                    csv.Add($"{absoluteMeasurement.UnityRealPoint.y}"); // uy
                    csv.Add($"{absoluteMeasurement.UnityRealPoint.z}"); // ux

                    yield return BSCOR_UpdateLogger(loggerComponent, csv);
                    lastIndex = (int) absoluteMeasurement.MasurementCounter;
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
                    csv.Add("X"); // not relative
                    csv.Add($"{absoluteMeasurement.GeoCoordinates.x}"); // lat
                    csv.Add($"{absoluteMeasurement.GeoCoordinates.y}"); // lon
                    csv.Add($"{absoluteMeasurement.GeoCoordinates.z}"); // alt
                    csv.Add($"{absoluteMeasurement.WorldPoint.x}"); // wx
                    csv.Add($"{absoluteMeasurement.WorldPoint.y}"); // wy
                    csv.Add($"{absoluteMeasurement.WorldPoint.z}"); // wx
                    csv.Add($"{absoluteMeasurement.UnityRealPoint.x}"); // ux
                    csv.Add($"{absoluteMeasurement.UnityRealPoint.y}"); // uy
                    csv.Add($"{absoluteMeasurement.UnityRealPoint.z}"); // ux

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

            if(logger.EVENT_IsEnabled())
            {
                while (!logger.EVENT_IsReadyForOutput())
                    yield return new WaitForEndOfFrame();
                logger.EVENT_ReadCSVRow(fields);
            }
        }
    }
}
