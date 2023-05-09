using System;
using System.Collections;
using System.Collections.Generic;

using System.Threading.Tasks;

using UnityEngine;

//#define WINDOWS_UWP



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

        private bool authorized = false;
        private bool runningGeolocalization = false;

        private Coroutine COR_AccessFeature = null;
        private Coroutine COR_Geolocalization = null;

        // Start is called before the first frame update
        void Start()
        {
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

            //UWP
            Task<GeolocationAccessStatus> accessRequestTask = Geolocator.RequestAccessAsync().AsTask();
            while (!accessRequestTask.IsCompleted)
                yield return new WaitForEndOfFrame();
            //UWP

            Debug.Log("Getting request status...");

            //UWP
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
            //UWP

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

            //UWP
            Geolocator geolocator = new Geolocator
            {
                DesiredAccuracy = ( HighAccuracy ? PositionAccuracy.High : PositionAccuracy.Default )
            };
            PositionStatus currentStatus = PositionStatus.Ready;
            geolocator.StatusChanged += (Geolocator g, StatusChangedEventArgs e) => {
                currentStatus = e.Status;
            };
            //UWP

            Debug.Log("Sending request...");

            //UWP
            Task<Geoposition> geolocalizationTask = geolocator.GetGeopositionAsync().AsTask();
            while (!geolocalizationTask.IsCompleted)
            {
                yield return new WaitForSecondsRealtime(1.0f);
                Debug.Log($"Status: {currentStatus.ToString()}");
            }
            //UWP

            Debug.Log("Received new position");

            //UWP
            Geoposition gpos = geolocalizationTask.Result;
            //gpos.Coordinate.Point.AltitudeReferenceSystem = AltitudeReferenceSystem.Geoid;
            Debug.Log($"Lat: {gpos.Coordinate.Point.Position.Latitude} -- Lon: {gpos.Coordinate.Point.Position.Longitude} -- Alt: {gpos.Coordinate.Point.Position.Altitude}");
            //UWP

            runningGeolocalization = false;
#endif
        }
    }
}
