//define WINDOWS_UWP

using System;
using System.Collections;
using System.Collections.Generic;

using System.Threading.Tasks;

using UnityEngine;

using Packages.StorageUtilities.Types;



#if WINDOWS_UWP
using Windows;
using Windows.Devices;
using Windows.Devices.Geolocation;
#endif




namespace Packages.Geolocation.ModuleTesting
{
    public class GeolocalizationFeatureTesting : MonoBehaviour
    {
        public bool HighAccuracy = false;
        public CSVLineReaderType logger = null;

        private bool authorized = false;
        private bool runningGeolocalization = false;

        private Coroutine COR_AccessFeature = null;
        private Coroutine COR_Geolocalization = null;



        // Start is called before the first frame update
        void Start()
        {
            if(logger == null)
            {
                Debug.LogWarning("WARNING: no logger provided");
            }

            COR_AccessFeature = StartCoroutine(ORCOR_GeolocationAccess());
        }

        // Update is called once per frame
        void Update()
        {
            if(authorized && !runningGeolocalization)
            {
                COR_Geolocalization = StartCoroutine(ORCOR_Geolocalization());
            }
        }






        
        private IEnumerator ORCOR_GeolocationAccess()
        {
            yield return null;

#if WINDOWS_UWP
            Debug.Log("Requesting access to Geolocator...");

            Task<GeolocationAccessStatus> accessRequestTask = Geolocator.RequestAccessAsync().AsTask();
            while (!accessRequestTask.IsCompleted)
                yield return new WaitForEndOfFrame();

            Debug.Log("Getting request status...");

            GeolocationAccessStatus status = accessRequestTask.Result;
            if(status != GeolocationAccessStatus.Allowed)
            {
                Debug.LogWarning("ERROR: unauthorized!");
                yield break;
            }
            else
            {
                Debug.Log("Success! Authorized");
            }

            yield return new WaitForSecondsRealtime(1.0f);
            authorized = true;
#endif
        }

        private IEnumerator ORCOR_Geolocalization()
        {
            yield return null;
#if WINDOWS_UWP
            runningGeolocalization = true;  

            Debug.Log("Asking for new geolocation...");

            Geolocator geolocator = new Geolocator
            {
                DesiredAccuracy = ( HighAccuracy ? PositionAccuracy.High : PositionAccuracy.Default )
            };
            PositionStatus currentStatus = PositionStatus.Ready;
            geolocator.StatusChanged += (Geolocator g, StatusChangedEventArgs e) => {
                currentStatus = e.Status;
            };

            Debug.Log("Sending request...");

            Task<Geoposition> geolocalizationTask = geolocator.GetGeopositionAsync().AsTask();
            while (!geolocalizationTask.IsCompleted)
            {
                yield return new WaitForSecondsRealtime(1.0f);
                Debug.Log($"Status: {currentStatus.ToString()}");
            }

            Debug.Log("Received new position");

            Geoposition gpos = geolocalizationTask.Result;
            // gpos.Coordinate.Point.AltitudeReferenceSystem.ToString
            Debug.Log($"Lat: {gpos.Coordinate.Point.Position.Latitude} -- Lon: {gpos.Coordinate.Point.Position.Longitude} -- Alt: {gpos.Coordinate.Point.Position.Altitude}");

            if(logger != null)
            {
                logger.EVENT_ReadCSVRow(new List<string> {
                    gpos.Coordinate.Point.Position.Latitude.ToString(),
                    gpos.Coordinate.Point.Position.Longitude.ToString(),
                    gpos.Coordinate.Point.Position.Altitude.ToString(),
                    gpos.Coordinate.Point.AltitudeReferenceSystem.ToString(),
                    Camera.main.transform.position.x.ToString(),
                    Camera.main.transform.position.y.ToString(),
                    Camera.main.transform.position.z.ToString()
                });
            }

            runningGeolocalization = false;
#endif
        }
    }
}
