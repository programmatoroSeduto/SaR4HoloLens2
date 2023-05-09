using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Packages.DoublePrecisionGeometry.Utils;
using Packages.Geolocation.Utils;

public class TestingCoordinatesTransformations : MonoBehaviour
{
    private bool done = false;

    // Update is called once per frame
    void Update()
    {
        if(!done)
        {
            Debug.Log("Initial Geo coordinates");
            DVector3 polarDeg = new DVector3(
                44.405650,
                8.946256,
                GeolocationPoint.EarthRadius
                );
            Debug.Log(polarDeg);

            Debug.Log("Geo Coordinates in Radiants");
            DVector3 polarRad = new DVector3(
                polarDeg.x * GeolocationPoint.Deg2Rad,
                polarDeg.y * GeolocationPoint.Deg2Rad,
                polarDeg.z
                );
            Debug.Log(polarRad);

            Debug.Log("Coordinates in world frame, cartesian");
            DVector3 cartesian = GeolocationPoint.PolarToCartesian(polarDeg, fromDeg: true);
            Debug.Log(cartesian);

            Debug.Log("Inverse transform from cartesian to polar");
            DVector3 polarInverseDeg = GeolocationPoint.CartesianToPolar(cartesian, toDeg: true);
            Debug.Log(polarInverseDeg);

            done = true;
        }
    }
}
