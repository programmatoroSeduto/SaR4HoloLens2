using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Packages.DoublePrecisionGeometry.Utils;
using Packages.Geolocation.Utils;

namespace Packages.Geolocation.Utils
{
    public class GeolocationTransform
    {
        //first geo measurement (measured)
        public DVector3 g1deg; 

        // first world coordinates (obtained)
        public DVector3 wP1; 

        // first Unity coordinates (measured)
        public DVector3 uP1;

        // second geo measurement (measured)
        public DVector3 g2deg;

        // second world coordinates (obtained)
        public DVector3 wP2;

        // second Unity coordinates (measured)
        public DVector3 uP2;

        // Geolocation world origin (obtained)
        public DVector3 wOu;

        // Geolocation direct transform (obtained)
        public RotationMatrix wRu;

        // Geolocation inverse transform (obtained)
        public RotationMatrix uRw;

        public void CalibrationStep()
        {
            // get wP1 from g1rad from g1deg (UP)
            wP1 = GeolocationPoint.PolarToCartesian(g1deg, fromDeg: true);

            // get wP2 from g2rad from g2deg 
            wP2 = GeolocationPoint.PolarToCartesian(g2deg, fromDeg: true);

            // get (wp2 - wp1) projected on wp1 plane (LOOK)
            DVector3 wr = wP2.distanceVector(wP1).projectedOnPlane(wP1);

            // inverse transformation
            uRw = new RotationMatrix();
            uRw.SetFromUpLook(wP1, wr);

            // direct transformation
            wRu = uRw.GetInverseRotation();

            // set world origin
            wOu = wP1;
        }

        public DVector3 WorldToUnity(DVector3 wP)
        {
            return DVector3.Sum(uP1, uRw.rotate(DVector3.Diff(wP1, wP)));
        }

        public DVector3 UnityToWorld(DVector3 uP)
        {
            return DVector3.Sum(wP1, wRu.rotate(DVector3.Diff(uP1, uP)));
        }

        public DVector3 PolarToUnity(DVector3 polar)
        {
            // polar -> world -> unity
            DVector3 wP = GeolocationPoint.PolarToCartesian(polar);
            return this.WorldToUnity(wP);
        }

        public DVector3 UnityToPolar(DVector3 uP)
        {
            // unity -> world -> polar
            DVector3 wP = this.UnityToWorld(uP);
            return GeolocationPoint.CartesianToPolar(wP);
        }

        public override string ToString()
        {
            string s = "";

            s += $"g1deg = {g1deg}" + "\n";
            s += $"g2deg = {g2deg}" + "\n";
            s += $"wP1 = {wP1}" + "\n";
            s += $"wP2 = {wP2}" + "\n";
            s += $"uP1 = {uP1}" + "\n";
            s += $"uP2 = {uP2}" + "\n";
            s += $"uRw = {uRw}" + "\n";
            s += $"wRu = {wRu}";

            return s;
        }
    }
}