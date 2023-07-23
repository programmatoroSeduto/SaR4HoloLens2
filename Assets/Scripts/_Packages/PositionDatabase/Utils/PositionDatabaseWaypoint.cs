using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Packages.PositionDatabase.Components;

namespace Packages.PositionDatabase.Utils
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
        public bool CanUpdate = false;

        // geometry infos
        public Vector3 AreaCenter
        {
            get
            {
                if (CanUpdate && ObjectCenterReference != null)
                {
                    AreaCenterFirst = ObjectCenterReference.transform.position;
                }
                return AreaCenterFirst;
            }
            set
            {
                AreaCenterFirst = value;
                ObjectCenterReference = null;
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

        public PositionDatabasePath GetPathTo(PositionDatabaseWaypoint wpDest)
        {
            foreach (PositionDatabasePath path in Paths)
                if (path.wp1 == wpDest || path.wp2 == wpDest)
                    return path;

            return null;
        }

        public bool IsLinkedWith(PositionDatabaseWaypoint wpDest)
        {
            return (GetPathTo(wpDest) != null);
        }

        public void TurnOffVisualization()
        {
            foreach (PositionDatabasePath link in Paths)
            {
                if (link.Renderer != null)
                {
                    GameObject.Destroy(link.Renderer);
                    link.Renderer = null;
                }
            }
        }



        // ===== PRIVATE ===== //

        private Vector3 AreaCenterFirst;
    }
}