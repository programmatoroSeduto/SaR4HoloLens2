using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Packages.DoublePrecisionGeometry.Utils
{
	// double-precision class for a 2D vector
    public class DVector2
    {
        public double x = 0.0;
        public double y = 0.0;

        public DVector2( double xx, double yy )
        {
            x = xx;
            y = yy;
        }

        public string ToJson( )
        {
            return JsonUtility.ToJson(this);
        }

        public static DVector2 FromJson(string dvector2)
        {
            return JsonUtility.FromJson<DVector2>(dvector2);
        }

        public Vector2 ToUnity()
        {
            return new Vector2((float) x, (float) y);
        }

        public static DVector2 FromUnity(Vector2 uv)
        {
            return new DVector2(uv.x, uv.y);
        }

        public override string ToString()
        {
            return $"(x:{x}, y:{y})";
        }
    }
}