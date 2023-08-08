using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Packages.PositionDatabase.Utils
{
    [Serializable]
    public class JSONPath : MonoBehaviour
    {
        public string Key;
        public int Waypoint1;
        public int Waypoint2;
        private PositionDatabasePath link;



        // ===== BUILDERS ===== //

        public JSONPath( )
        {
            
        }

        public JSONPath(PositionDatabasePath link)
        {
            this.Key = link.Key;
            this.Waypoint1 = link.wp1.PositionID;
            this.Waypoint2 = link.wp2.PositionID;
        }
    }
}
