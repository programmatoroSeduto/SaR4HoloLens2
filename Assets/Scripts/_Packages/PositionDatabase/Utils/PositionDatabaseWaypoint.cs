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

        public string Key
        {
            get => positionID.ToString("0000");
        }

        public Vector3 FirstAreaCenter
        {
            get => AreaCenterFirst;
        }

        // references
        public PositionsDatabase DBReference = null;
        public GameObject ObjectCenterReference = null;
        public bool CanUpdate = false;
        public string Description = "";

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

        // area identifier
        public int AreaIndex = 0;

        // creation timestamp
        public DateTime Timestamp = DateTime.Now;



        // ===== PRIVATE ===== //

        private int positionID = -1;
        private Vector3 AreaCenterFirst;



        // ===== PUBLIC METHODS ===== //

        public void AddPath(PositionDatabaseWaypoint wpTo)
        {
            PositionDatabasePath path = new PositionDatabasePath(this, wpTo);
            this.Paths.Add(path);
            wpTo.Paths.Add(path);
        }

        public void setPositionID(int id)
        {
            if (positionID == -1)
            {
                positionID = id;
                if (Description == "")
                    Description = Key;
            }
        }

        public void setFirstAreaCenter(Vector3 ac)
        {
            AreaCenterFirst = ac;
        }

        public PositionDatabasePath GetPathTo(PositionDatabaseWaypoint wpDest)
        {
            foreach (PositionDatabasePath path in Paths)
                if (path.wp1 == wpDest || path.wp2 == wpDest)
                    return path;

            return null;
        }

        public PositionDatabasePath GetPathTo(string destKey)
        {
            if (destKey == Key)
                return null;

            foreach (PositionDatabasePath path in Paths)
                if (path.wp1.Key == destKey || path.wp2.Key == destKey)
                    return path;

            return null;
        }

        public bool IsLinkedWith(PositionDatabaseWaypoint wpDest)
        {
            return (GetPathTo(wpDest) != null);
        }

        public bool IsLinkedWith(string destKey)
        {
            return (GetPathTo(destKey) != null);
        }

        public void TurnOffVisualization()
        {
            foreach (PositionDatabasePath link in Paths)
            {
                if (link.Renderer != null)
                {
                    GameObject.Destroy(link.Renderer);
                }
                link.Renderer = null;
            }
        }



        // ===== FEATURE EXPORT DATA ===== //

        // export as JSON item
        public string ToJson()
        {
            string linksDump = "";
            for (int i = 0; i < Paths.Count; ++i)
                linksDump += Paths[i].Next(this).positionID + (i == Paths.Count - 1 ? "" : ", ");

            return "{" 
                + $"'key' : '{this.Key}'" + ","
                + $"'position_id' : '{this.PositionID}'" + ","
                + $"'area_radius' : '{this.AreaRadius}'" + ","
                + $"'area_center_x' : '{this.AreaCenter.x}'" + ","
                + $"'area_center_y' : '{this.AreaCenter.y}'" + ","
                + $"'area_center_z' : '{this.AreaCenter.z}'" + ","
                + $"'original_area_center_x' : '{this.FirstAreaCenter.x}'" + ","
                + $"'original_area_center_y' : '{this.FirstAreaCenter.y}'" + ","
                + $"'original_area_center_z' : '{this.FirstAreaCenter.z}'" + ","
                + $"'linked_to' : [ {linksDump} ]" + ","
                + $"'created_at' : '{this.Timestamp}'"
                + "}";
        }

        // export as CSV item
        public List<string> ToCsv(bool header = false)
        {
            if (header)
                return new List<string> {
                    "key", "position_id",
                    "area_radius",
                    "area_center_x", "area_center_y", "area_center_z",
                    "original_area_center_x", "original_area_center_y", "original_area_center_z",
                    "linked_to",
                    "created_at"
                };
            else
            {
                string linksDump = "";
                for(int i=0; i<Paths.Count; ++i)
                    linksDump += Paths[i].Next(this).positionID + ( i == Paths.Count-1 ? "" : ";" );

                return new List<string> {
                    this.Key, this.PositionID.ToString(),
                    this.AreaRadius.ToString(),
                    this.AreaCenter.x.ToString(), this.AreaCenter.y.ToString(), this.AreaCenter.z.ToString(),
                    this.FirstAreaCenter.x.ToString(), this.FirstAreaCenter.y.ToString(), this.FirstAreaCenter.z.ToString(),
                    linksDump,
                    this.Timestamp.ToString()
                };
            }
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




    }
}