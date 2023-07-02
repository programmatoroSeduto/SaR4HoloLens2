using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SaR4Hololens2.Scenes.BuildingExplorationV2.Scripts.Components;

namespace SaR4Hololens2.Scenes.BuildingExplorationV2.Scripts.Utils
{
    public class PositionDatabaseWaypoint
    {
        // ===== PUBLIC ===== //

        // references
        public PositionsDatabase DBReference = null;
        public GameObject ObjectCenterReference = null;

        // geometry infos
        public Vector3 AreaCenter
        {
            get 
            {
                if (ObjectCenterReference != null)
                    AreaCenterFirst = ObjectCenterReference.transform.position;
                return AreaCenterFirst;
            }
            set
            {
                AreaCenterFirst = AreaCenter;
            }
        } 
        public float AreaRadius = 1.0f;

        // infos about the paths starting from here
        public List<PositionDatabasePath> Paths = new List<PositionDatabasePath>();

        // creation timestamp
        public DateTime Timestamp = DateTime.Now;



        // ===== PUBLIC METHODS ===== //

        public void AddPath(PositionDatabaseWaypoint wpTo)
        {
            PositionDatabasePath path = new PositionDatabasePath(this, wpTo);
            this.Paths.Add(path);
            wpTo.Paths.Add(path);
        }



        // ===== PRIVATE ===== //

        private Vector3 AreaCenterFirst;
    }
}