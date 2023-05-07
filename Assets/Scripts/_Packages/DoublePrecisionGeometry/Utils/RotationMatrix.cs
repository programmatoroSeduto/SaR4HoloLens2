using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Packages.DoublePrecisionGeometry.Utils
{
	// Rotation Matrix with double precision
    public class RotationMatrix
    {
        public double[,] R = new double[3, 3]
        {
            { 1, 0, 0 },
            { 0, 1, 0 },
            { 0, 0, 1 }
        };

        public RotationMatrix( )
        {
            // ...
        }

        public void SetFromUpLook( DVector3 up, DVector3 look )
        {
            // vx : left
            // vy : up
            // vz : look

            // vedo up e look dal mio frame
            // e formulo una rotazione che proietta le coordinate del loro frame nel mio
            // e1 : il mio frame
            // e2 : il frame definito da look e up

            DVector3 e2y = up.normalized();
            DVector3 e2z = look.normalized();
            DVector3 e2x = e2y.cross(e2z, left: true);

            DVector3 e1x = DVector3.onex;
            DVector3 e1y = DVector3.oney;
            DVector3 e1z = DVector3.onez;

            // e2x = (e2x*e1x)e1x + (e2x*e1y)e1y + (e2x*e1z)e1z
            // e2y = (e2y*e1x)e1x + (e2y*e1y)e1y + (e2y*e1z)e1z
            // e2z = (e2z*e1x)e1x + (e2z*e1y)e1y + (e2z*e1z)e1z
            // (e2x*e1x) -> e1x.dot(e2x)

            R[0, 0] = e1x.dot(e2x);
            R[0, 1] = e1y.dot(e2x);
            R[0, 2] = e1z.dot(e2x);
            R[1, 0] = e1x.dot(e2y);
            R[1, 1] = e1y.dot(e2y);
            R[1, 2] = e1z.dot(e2y);
            R[2, 0] = e1x.dot(e2z);
            R[2, 1] = e1y.dot(e2z);
            R[2, 2] = e1z.dot(e2z);
        }

        public RotationMatrix GetInverseRotation()
        {
            RotationMatrix rm = new RotationMatrix();
            
            rm.R[0, 0] = R[0, 0];
            rm.R[0, 1] = R[1, 0];
            rm.R[0, 2] = R[2, 0];
            rm.R[1, 0] = R[0, 1];
            rm.R[1, 1] = R[1, 1];
            rm.R[1, 2] = R[2, 1];
            rm.R[2, 0] = R[0, 2];
            rm.R[2, 1] = R[2, 1];
            rm.R[2, 2] = R[2, 2];

            return rm;
        }

        public DVector3 rotate(DVector3 v2)
        {
            return new DVector3(
                xx: R[0, 0] * v2.x + R[0, 1] * v2.y + R[0, 2] * v2.z,
                yy: R[1, 0] * v2.x + R[1, 1] * v2.y + R[1, 2] * v2.z,
                zz: R[2, 0] * v2.x + R[2, 1] * v2.y + R[2, 2] * v2.z
                );
        }

        public override string ToString()
        {
            string ss = "R vals: ";

            foreach( var row in R )
            {
                ss = ss + row + ",";
            }

            return ss;
        }
    }
}
















