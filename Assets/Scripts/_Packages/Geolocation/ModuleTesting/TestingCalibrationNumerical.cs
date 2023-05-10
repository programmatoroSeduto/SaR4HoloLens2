using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Packages.DoublePrecisionGeometry.Utils;
using Packages.Geolocation.Utils;


namespace Packages.Geolocation.ModuleTesting
{
    public class TestingCalibrationNumerical : MonoBehaviour
    {
        private bool done = false;

        void Update()
        {
            if(!done)
            {
                Debug.Log(" === Calibration step === ");
                GeolocationTransform gt = new GeolocationTransform();
                gt.g1deg = new DVector3(
                    44.405650,
                    8.946256,
                    GeolocationPoint.EarthRadius
                    );
                gt.g2deg = new DVector3(
                    44.405651,
                    8.946257,
                    GeolocationPoint.EarthRadius
                    );
                gt.uP1 = new DVector3(1,0,1);
                gt.CalibrationStep();
                Debug.Log(gt);

                Debug.Log(" === Testing step === ");
                DVector3 g = new DVector3(
                    44.405650,
                    8.946256,
                    GeolocationPoint.EarthRadius
                    );
                Debug.Log($"(same as g1deg) g = {g}");
                Debug.Log($"(expected g1) uP = {gt.PolarToUnity(g)}");
                g = new DVector3(
                    44.405651,
                    8.946256,
                    GeolocationPoint.EarthRadius
                    );
                Debug.Log($"g = {g}");
                Debug.Log($"uP = {gt.PolarToUnity(g)}");
                g = new DVector3(
                    44.405652,
                    8.946256,
                    GeolocationPoint.EarthRadius
                    );
                Debug.Log($"g = {g}");
                Debug.Log($"uP = {gt.PolarToUnity(g)}");
                g = new DVector3(
                    44.405653,
                    8.946256,
                    GeolocationPoint.EarthRadius
                    );
                Debug.Log($"g = {g}");
                Debug.Log($"uP = {gt.PolarToUnity(g)}");
                g = new DVector3(
                    44.405654,
                    8.946256,
                    GeolocationPoint.EarthRadius
                    );
                Debug.Log($"g = {g}");
                Debug.Log($"uP = {gt.PolarToUnity(g)}");
                g = new DVector3(
                    44.405655,
                    8.946256,
                    GeolocationPoint.EarthRadius
                    );
                Debug.Log($"g = {g}");
                Debug.Log($"uP = {gt.PolarToUnity(g)}");

                done = true;
            }
        }
    }

}