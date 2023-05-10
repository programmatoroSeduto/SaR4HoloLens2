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
        public DVector3 g1deg = null; 

        // first world coordinates (obtained)
        public DVector3 wP1 = null;

        // first Unity coordinates (measured)
        public DVector3 uP1 = null;

        // second geo measurement (measured)
        public DVector3 g2deg = null;

        // second world coordinates (obtained)
        public DVector3 wP2 = null;

        // second Unity coordinates (measured)
        public DVector3 uP2 = null;

        // Geolocation world origin (obtained)
        public DVector3 wOu = null;

        // Geolocation direct transform (obtained)
        public RotationMatrix wRu = null;

        // Geolocation inverse transform (obtained)
        public RotationMatrix uRw = null;

        public void CalibrationStep()
        {
            if (g1deg == null || g2deg == null ||
                wP1 == null || wP2 == null ||
                uP1 == null || uP2 == null)
                return;

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

        public bool CheckCalibrationAbsDistanceError(double tollerance = 0.5)
        {
            if (g1deg == null || g2deg == null ||
                wP1 == null || wP2 == null ||
                uP1 == null || uP2 == null)
                return false;

            return 
                System.Math.Abs(DVector3.Diff(wP1, wP2).modulus() - DVector3.Diff(uP1, uP2).modulus()) <= tollerance;
        }

        public DVector3 WorldToUnity(DVector3 wP)
        {
            if (uRw == null || uP1 == null || wP1 == null) return null;

            return DVector3.Sum(uP1, uRw.rotate(DVector3.Diff(wP1, wP)));
        }

        public DVector3 UnityToWorld(DVector3 uP)
        {
            if (wRu == null || uP1 == null || wP1 == null) return null;

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

            s += $"g1deg = {IsNull(g1deg, "NULL")}" + "\n";
            s += $"g2deg = {IsNull(g2deg, "NULL")}" + "\n";
            s += $"wP1 = {IsNull(wP1, "NULL")}" + "\n";
            s += $"wP2 = {IsNull(wP2, "NULL")}" + "\n";
            s += $"uP1 = {IsNull(uP1, "NULL")}" + "\n";
            s += $"uP2 = {IsNull(uP2, "NULL")}" + "\n";
            s += $"uRw = {IsNull(uRw, "NULL")}" + "\n";
            s += $"wRu = {IsNull(wRu, "NULL")}";

            return s;
        }

        private string IsNull<T>(T s, string replaceWith = "")
        {
            return (s != null ? s.ToString() : replaceWith);
        }
    }
}