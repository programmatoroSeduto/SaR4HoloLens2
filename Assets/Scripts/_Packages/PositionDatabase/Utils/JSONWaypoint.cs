using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Packages.PositionDatabase.Utils
{
    [Serializable]
    public class JSONWaypoint : MonoBehaviour
    {
        public string Key;
        public int PositionID;
        public float AreaRadius;
        public List<float> AreaCenter = new List<float> { 0.0f, 0.0f, 0.0f };
        public List<float> FirstAreaCenter = new List<float> { 0.0f, 0.0f, 0.0f };
        public List<JSONPath> Paths = new List<JSONPath>();
        public string CreatedAt;



        // ===== BUILDERS ===== //

        public JSONWaypoint()
        {

        }

        public JSONWaypoint(PositionDatabaseWaypoint wp)
        {
            this.Key = wp.Key;
            this.PositionID = wp.PositionID;
            this.AreaRadius = wp.AreaRadius;
            this.AreaCenter = new List<float> { wp.AreaCenter.x, wp.AreaCenter.y, wp.AreaCenter.z };
            this.FirstAreaCenter = new List<float> { wp.FirstAreaCenter.x, wp.FirstAreaCenter.y, wp.FirstAreaCenter.z };
            this.CreatedAt = wp.Timestamp.ToString();
        }



        // ===== PUBLIC ===== //

        public PositionDatabaseWaypoint FromJsonWaypoint()
        {
            PositionDatabaseWaypoint dbwp = new PositionDatabaseWaypoint();

            dbwp.setPositionID(this.PositionID);
            dbwp.AreaRadius = this.AreaRadius;
            dbwp.AreaCenter = new Vector3(this.AreaCenter[0], this.AreaCenter[1], this.AreaCenter[2]);
            dbwp.setFirstAreaCenter(new Vector3(this.FirstAreaCenter[0], this.FirstAreaCenter[1], this.FirstAreaCenter[2]));
            DateTime.TryParse(this.CreatedAt, out dbwp.Timestamp);

            return dbwp;
        }
    }
}
