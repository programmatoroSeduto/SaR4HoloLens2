using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Project.Scripts.Components;
using Project.Scripts.Utils;

using Packages.DiskStorageServices.Components;
using Packages.PositionDatabase.Utils;

namespace Packages.PositionDatabase.Components
{
    public class PositionDatabaseClientUtility : MonoBehaviour
    {
        // ===== GUI ===== //

        [Header("Basic Properties")]
        [Tooltip("Reference to the positions database")]
        public PositionsDatabase PositionsDB = null;



        // ===== PRIVATE ===== //

        // reference to the low level from the database (it is a 'init' variable as well)
        private PositionDatabaseLowLevel lowLevel = null;



        // ===== UNITY CALLBACKS AND INIT ===== //

        private void Start()
        {
            TryInit();
        }

        private bool TryInit()
        {
            if (PositionsDB != null && lowLevel != null) return true;

            // init DB
            if (PositionsDB != null)
                lowLevel = PositionsDB.LowLevelDatabase;
            else
                return false;

            return true;
        }

    }
}
