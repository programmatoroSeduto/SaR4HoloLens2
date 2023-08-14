using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Packages.PositionDatabase.Utils
{
    [Serializable]
    public class JSONWaypoint
    {
        public string Key;
        public int PositionID;
        public float AreaRadius;
        public List<float> AreaCenter = new List<float> { 0.0f, 0.0f, 0.0f };
        public List<float> FirstAreaCenter = new List<float> { 0.0f, 0.0f, 0.0f };
        public List<JSONPath> Paths = new List<JSONPath>();
        public string Description = "";
        public string CreatedAt;
        public int AreaIndex;
    }
}
