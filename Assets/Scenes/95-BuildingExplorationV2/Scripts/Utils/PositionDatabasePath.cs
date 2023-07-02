using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SaR4Hololens2.Scenes.BuildingExplorationV2.Scripts.Utils
{
    public class PositionDatabasePath
    {
        // ===== PUBLIC ===== //

        public PositionDatabaseWaypoint wp1 = null;
        public PositionDatabaseWaypoint wp2 = null;

        public float Distance { 
            get {
                Vector3 pos1 = wp1.AreaCenter;
                Vector3 pos2 = wp2.AreaCenter;
                return Vector3.Distance(pos1, pos2);
            } 
        }



        // ===== PUBLIC METHODS ===== //

        public PositionDatabasePath(PositionDatabaseWaypoint wpFrom = null, PositionDatabaseWaypoint wpTo = null)
        {
            wp1 = wpFrom;
            wp2 = wpTo;
        }

        public PositionDatabaseWaypoint Next(PositionDatabaseWaypoint wpFrom)
        {
            if (wpFrom == null) return null;
            
            return (wp1 == wpFrom ? wp2 : wp1);
        }
    }
}
