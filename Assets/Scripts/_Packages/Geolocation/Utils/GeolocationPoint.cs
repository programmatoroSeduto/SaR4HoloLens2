using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Packages.DoublePrecisionGeometry.Utils;

namespace Packages.Geolocation.Utils
{
    public class GeolocationPoint
    {
        // constants
        public static readonly double EarthRadius = 6371000.0;
        public static readonly double Rad2Deg = 180.0 / System.Math.PI;
        public static readonly double Deg2Rad = System.Math.PI / 180.0;

        // Geolocation index 
        public int MasurementCounter = -1;

        // Geo (from measurements)
        public DVector3 GeoRealCoordinates = null;

        // Geo (predicted)
        public DVector3 GeoCoordinates = null;

        // World (from measurement)
        public DVector3 WorldRealPoint = null;

        // World (predicted)
        public DVector3 WorldPoint = null;

        // Unity (from measurement)
        public DVector3 UnityRealPoint = null;

        // Unity (predicted)
        public DVector3 UnityPoint = null;

        // timestamp
        public DateTime timestamp = DateTime.Now;

        public GeolocationPoint()
        {
            timestamp = DateTime.Now;
        }

        public static GeolocationPoint FromAbsoluteGeolocation(DVector3 g, DVector3 uP, Nullable<DateTime> dt = null)
        {
            GeolocationPoint res = new GeolocationPoint();

            res.GeoRealCoordinates = g;
            res.UnityRealPoint = uP;
            if (dt != null)
                res.timestamp = (DateTime)dt;
            else
                res.timestamp = DateTime.Now;

            return res;
        }

        // coordinates from polar to cartesian (angles in degrees)
        public static DVector3 PolarToCartesian(DVector3 PolarCoord, bool fromDeg = true)
        {
            double phi = PolarCoord.x;
            double lambda = PolarCoord.y;
            double H = PolarCoord.z;

            if(fromDeg)
            {
                phi = Deg2Rad * phi;
                lambda = Deg2Rad * lambda;
            }

            return new DVector3(
                H * System.Math.Cos(phi) * System.Math.Cos(lambda),
                H * System.Math.Sin(phi),
                H * System.Math.Cos(phi) * System.Math.Sin(lambda)
                );
        }

        // coordinates from cartesian to polar (no singularity)
        public static DVector3 CartesianToPolar(DVector3 pos, bool toDeg = true)
        {
            double lambda = System.Math.Atan2(pos.z, pos.x);
            double phi = System.Math.Atan2(System.Math.Cos(lambda) * pos.y, pos.x);
            double H = pos.y / System.Math.Sin(phi);

            if (toDeg)
                return new DVector3(Rad2Deg * phi, Rad2Deg * lambda, H);
            else
                return new DVector3(phi, lambda, H);
        }

        public override string ToString()
        {
            string s = "GeolocationPoint:\n";

            if (GeoRealCoordinates != null)
                s += $"GeoRealCoordinates={GeoRealCoordinates}\n";
            if (WorldRealPoint != null)
                s += $"WorldRealPoint={WorldRealPoint}\n";
            if (UnityRealPoint != null)
                s += $"UnityRealPoint={UnityRealPoint}\n";
            if (GeoCoordinates != null)
                s += $"GeoCoordinates={GeoCoordinates}\n";
            if (WorldPoint != null)
                s += $"WorldPoint={WorldPoint}\n";
            if (UnityPoint != null)
                s += $"UnityPoint={UnityPoint}\n";

            return s;
        }
    }
}
