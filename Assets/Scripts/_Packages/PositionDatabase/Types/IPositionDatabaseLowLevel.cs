using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;

using Packages.PositionDatabase.Utils;

namespace Packages.PositionDatabase.Types
{
    public interface IPositionDatabaseLowLevel
    {
        // ===== PUBLIC GETTERS ===== //

        public Vector3 SortReferencePosition { get; set; }
        public List<PositionDatabaseWaypoint> Database { get; set; }
        public PositionDatabaseWaypoint CurrentZone { get; }
        public int Count { get; }



        // ===== UTILITY FUNCTIONS ===== //

        public void Reset();



        // ===== SINGLE STEP SORT ===== //

        public void SortStep();



        // ===== ONESHOT SORT ===== //

        public void SortAll();



        // ===== INSERT AND UPDATE ===== //

        public void Insert(PositionDatabaseWaypoint wp);
    }
}
