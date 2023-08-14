using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Packages.PositionDatabase.Utils;

namespace Packages.PositionDatabase.Types
{
    public interface IPositionDatabaseLowLevel
    {
        public Vector3 SortReferencePosition { get; set; }
        public List<PositionDatabaseWaypoint> Database { get; set; }
        public void Reset();
        public void SortStep();
    }
}
