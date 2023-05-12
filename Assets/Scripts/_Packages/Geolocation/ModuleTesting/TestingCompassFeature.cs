//define WINDOWS_UWP

using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Packages.StorageUtilities.Components;
using System;

#if WINDOWS_UWP
using Windows;
using Windows.Foundation;
using Windows.Devices;
using Windows.Devices.Sensors;
#endif

/*
 * NOT WORKING
 * HoloLens2 does not support such a feature...
 * ... even if it has a magnetometer ...
 * */

namespace Packages.Geolocation.ModuleTesting
{
    public class TestingCompassFeature : MonoBehaviour
    {
        public string FileName = "compass_output";
        public bool UseUWP = false;

        private CSVFileWriter outputLogger = null;
        private Coroutine COR_Compass = null;
        private Vector3 uP = Vector3.zero;
        private object MUTEX_uP = new object();

#if WINDOWS_UWP
        Windows.Devices.Sensors.Compass compass;
#endif


        void Start()
        {
            if (!UseUWP)
            {
                outputLogger = CreateLogger(FileName, new List<string>
                {
                    "UX", "UY", "UX", 
                    "TRUE_HEADING", "MAGNETIC_HEADING", "HEADING_ACCURACY", "RAW_X", "RAW_Y", "RAW_Z"
                });
                Input.location.Start();

                COR_Compass = StartCoroutine(BSCOR_CompassUnity());
            }
            else
            {
                outputLogger = CreateLogger(FileName, new List<string>
                {
                    "UX", "UY", "UX",
                    "MAGNETIC_NORTH_HEADING", "TRUE_NORTH_HEADING"
                });

                UWP_InitCompass();
            }
        }

        private void Update()
        {
            lock (MUTEX_uP) uP = Camera.main.transform.position;
        }

        private IEnumerator BSCOR_CompassUnity()
        {
            while(true)
            {
                yield return new WaitForSecondsRealtime(2.0f);

                Vector3 uP = Camera.main.transform.position;
                float trueHeading = Input.compass.trueHeading;
                float magHeading = Input.compass.magneticHeading;
                float accuracy = Input.compass.headingAccuracy;
                Vector3 rawHeading = Input.compass.rawVector;

                List<string> fields = new List<string>
                {
                    uP.x.ToString(), uP.y.ToString(), uP.z.ToString(),
                    trueHeading.ToString(),
                    magHeading.ToString(),
                    accuracy.ToString(),
                    rawHeading.x.ToString(), rawHeading.y.ToString(), rawHeading.z.ToString()
                };

                
                outputLogger.EVENT_WriteCsv(fields, print:true);
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


        private void UWP_InitCompass()
        {
#if WINDOWS_UWP
            compass = Windows.Devices.Sensors.Compass.GetDefault();

            uint minReportInterval = compass.MinimumReportInterval;
            uint reportInterval = minReportInterval > 16 ? minReportInterval : 16;
            compass.ReportInterval = reportInterval;

            compass.ReadingChanged += new TypedEventHandler<Windows.Devices.Sensors.Compass, CompassReadingChangedEventArgs>(ReadingChanged);
#endif
        }

#if WINDOWS_UWP
        private void ReadingChanged(object sender, CompassReadingChangedEventArgs e)
        {
            CompassReading reading = e.Reading;
            string magNorth = "";
            string trueNorth = "";
            magNorth = String.Format("{0,5:0.00}", reading.HeadingMagneticNorth);
            if (reading.HeadingTrueNorth.HasValue)
                trueNorth = String.Format("{0,5:0.00}", reading.HeadingTrueNorth);
            else
                trueNorth = "";
            
            List<string> fields = null;
            lock(MUTEX_uP)
                fields = new List<string>
                {
                    uP.x.ToString(), uP.y.ToString(), uP.z.ToString(),
                    trueNorth.ToString(),
                    magNorth.ToString()
                };

            outputLogger.EVENT_WriteCsv(fields, print: true);
        }
#endif
    }
}
