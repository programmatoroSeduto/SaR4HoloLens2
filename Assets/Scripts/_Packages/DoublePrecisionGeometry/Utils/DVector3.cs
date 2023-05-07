using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Packages.DoublePrecisionGeometry.Utils
{
	// double-precision class for a 3D vector
    public class DVector3
    {
        public double x = 0.0;
        public double y = 0.0;
        public double z = 0.0;

        public static DVector3 zero = new DVector3();
        public static DVector3 ones = new DVector3(1, 1, 1);
        public static DVector3 onex = new DVector3(1, 0, 0);
        public static DVector3 oney = new DVector3(0, 1, 0);
        public static DVector3 onez = new DVector3(0, 0, 1);

        public DVector3(double xx=0, double yy=0, double zz=0)
        {
            x = xx;
            y = yy;
            z = zz;
        }

        public string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        public static DVector3 FromJson(string dvector3)
        {
            return JsonUtility.FromJson<DVector3>(dvector3);
            
        }
        public Vector3 ToUnity()
        {
            return new Vector3((float)x, (float)y, (float)z);
        }

        public static DVector3 FromUnity(Vector3 uv)
        {
            return new DVector3(uv.x, uv.y, uv.z);
        }

        // dot product
        public double dot(DVector3 v)
        {
            return x * v.x + y * v.y + z * v.z;
        }

        public double dot(Vector3 v)
        {
            return this.dot(DVector3.FromUnity(v));
        }

        // cross product
        public DVector3 cross(DVector3 c, bool left=true)
        {
            DVector3 v = this;

            if(left)
                return new DVector3(
                    xx: v.z * c.y - v.y * c.z,
                    yy: v.x * c.z - v.z * c.x,
                    zz: v.x * c.y - v.y * c.x
                    );
            else
                return new DVector3(
                    xx: v.z * c.y - v.y * c.z,
                    yy: v.z * c.x - v.x * c.z,
                    zz: v.x * c.y - v.y * v.x
                    );
        }

        public DVector3 normalized()
        {
            return this.scale(1.0/ this.modulus());
        }

        public DVector3 scale(double factor)
        {
            return new DVector3(factor * x, factor * y, factor * z);
        }

        public double modulus()
        {
            return System.Math.Sqrt(this.dot(this));
        }

        public DVector3 distanceVector(DVector3 a)
        {
            // (B - A)
            var b = this;
            return new DVector3(
                b.x - a.x,
                b.y - a.y,
                b.z - a.z);
        }

        public DVector3 projectedOnPlane(DVector3 v)
        {
            var r = this;
            return this.distanceVector(v.scale(v.dot(this) / v.dot(v)));
        }

        public override string ToString()
        {
            return $"(x:{x}, y:{y}, z:{z})";
        }
    }
}