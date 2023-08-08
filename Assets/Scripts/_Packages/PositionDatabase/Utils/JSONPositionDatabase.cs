using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Packages.PositionDatabase.Components;

namespace Packages.PositionDatabase.Utils
{
    [Serializable]
    public class JSONPositionDatabase
    {
        public float BaseDistance;
        public float BaseHeight;
        public float DistanceTolerance;
        public bool UseClusters;
        public int ClusterSize;
        public bool UseMaxIndices;
        public int MaxIndices;

        public List<JSONWaypoint> Waypoints = new List<JSONWaypoint>();



        // ===== BUILDERS ===== //

        public JSONPositionDatabase()
        {

        }

        public JSONPositionDatabase(PositionsDatabase jdb)
        {
            FromDatabase(jdb);
        }



        // ===== PUBLIC ===== //

        public void SetDatabase(PositionsDatabase db)
        {
            ToDatabase(db);
        }



        // ===== PRIVATE ===== //

        private void FromDatabase(PositionsDatabase db)
        {
            this.BaseDistance = db.BaseDistance;
            this.BaseHeight = db.BaseHeight;
            this.DistanceTolerance = db.DistanceTolerance;
            this.UseClusters = db.UseClusters;
            this.UseMaxIndices = db.UseMaxIndices;
            this.MaxIndices = db.MaxIndices;
        }

        private void ToDatabase(PositionsDatabase db)
        {
            db.BaseDistance = this.BaseDistance;
            db.BaseHeight = this.BaseHeight;
            db.DistanceTolerance = this.DistanceTolerance;
            db.UseClusters = this.UseClusters;
            db.UseMaxIndices = this.UseMaxIndices;
            db.MaxIndices = this.MaxIndices;
        }
    }
}
