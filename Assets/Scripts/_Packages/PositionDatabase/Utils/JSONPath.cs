using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Packages.PositionDatabase.Utils
{
    [Serializable]
    public class JSONPath
    {
        public string Key;
        public int Waypoint1;
        public int Waypoint2;
        private PositionDatabasePath link;
    }
}
