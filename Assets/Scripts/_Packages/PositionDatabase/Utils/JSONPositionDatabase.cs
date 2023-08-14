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
        public JSONWaypoint CurrentZone;
        
        public float BaseDistance;
        public float BaseHeight;
        public float DistanceTolerance;
        public bool UseClusters;
        public int ClusterSize;
        public bool UseMaxIndices;
        public JSONTupleList<int, int> AreaRenaming = new JSONTupleList<int, int>();
        public int MaxIndices;

        public List<JSONWaypoint> Waypoints = new List<JSONWaypoint>();
    }
}
