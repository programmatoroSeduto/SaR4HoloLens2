using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Packages.Geolocation.Utils;


namespace Packages.Geolocation.Types
{
    public class GeolocationPointReaderType : MonoBehaviour
    {
        public virtual void EVENT_ReadGeopoint(GeolocationPoint gp)
        {
            Debug.LogError("IMPLEMENT THE METHOD! EVENT_ReadGeopoint()");
            return;
        }
    }
}