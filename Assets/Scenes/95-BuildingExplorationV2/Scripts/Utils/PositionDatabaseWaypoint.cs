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

        public int PositionID
        {
            get => positionID;
        }

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
                AreaCenterFirst = value;
            }
        } 
        public float AreaRadius = 1.0f;

        // infos about the paths starting from here
        public List<PositionDatabasePath> Paths = new List<PositionDatabasePath>();

        // creation timestamp
        public DateTime Timestamp = DateTime.Now;



        // ===== PRIVATE ===== //

        private int positionID = -1;



        // ===== PUBLIC METHODS ===== //

        public void AddPath(PositionDatabaseWaypoint wpTo)
        {
            PositionDatabasePath path = new PositionDatabasePath(this, wpTo);
            this.Paths.Add(path);
            wpTo.Paths.Add(path);
        }

        public void setPositionID(int id)
        {
            if (positionID == -1) positionID = id;
        }

        public override string ToString()
        {
            string ss = "";

            Vector3 c = this.AreaCenter;

            ss += $"Area center: ({c.x}, {c.y}, {c.z})" + " || ";
            ss += $"DB Reference: {(ObjectCenterReference == null ? "NULL" : ObjectCenterReference.name)}" + " || ";
            ss += $"DB Reference: {(DBReference == null ? "NULL" : "set")}" + " || ";
            ss += $"Area radius: {AreaRadius}" + " || ";
            ss += $"With paths: {Paths.Count}" + " || ";
            ss += $"Created at: {Timestamp}";

            return ss;
        }



        // ===== PRIVATE ===== //

        private Vector3 AreaCenterFirst;
    }
}