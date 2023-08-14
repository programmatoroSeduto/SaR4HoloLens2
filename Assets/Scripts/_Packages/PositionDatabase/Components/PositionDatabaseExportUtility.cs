using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Project.Scripts.Components;
using Project.Scripts.Utils;

using Packages.DiskStorageServices.Components;
using Packages.PositionDatabase.Utils;

namespace Packages.PositionDatabase.Components
{
    public class PositionDatabaseExportUtility : MonoBehaviour
    {
        // ===== GUI ===== //

        [Header("Basic Properties")]
        [Tooltip("Reference to the positions database")]
        public PositionsDatabase PositionsDB = null;

        [Header("Storage Properties")]
        [Tooltip("The component which can save the file on disk")]
        public StorageHubOneShot StorageHub = null;
        [Tooltip("Load the storage Hub from the global project settings")]
        public bool LoadGlobalStorageHub = false;
        [Tooltip("(OPTIONAL, only with 'LoadGlobalStorageHub' enabled) name of the global property containing the global storage")]
        public string GlobalStorageHubName = "StorageHub";



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
            if (PositionsDB != null && lowLevel != null && StorageHub != null) return true;

            // init DB
            if (PositionsDB != null)
                lowLevel = PositionsDB.LowLevelDatabase;
            else
                return false;

            // init storage hub
            if (StorageHub == null)
            {
                this.StorageHub = StaticAppSettings.GetObject("StorageHub", null) as StorageHubOneShot;
                if (StorageHub == null) return false;
            }
            
            return true;
        }



        // ===== FEATURE FULL EXPORT ===== //

        public void EVENT_ExportJsonFull()
        {
            if (!TryInit()) return;

            // ...
        }



        // ===== FEATURE FULL IMPORT ===== //

        public void EVENT_ImportJsonFull()
        {
            if (!TryInit()) return;

            StartCoroutine(BSCOR_ImportJsonFull());
        }

        public IEnumerator BSCOR_ImportJsonFull(bool fullRefresh = false)
        {
            yield return null;

            if (!TryInit() || !PositionsDB.SetStatusImporting(this, true))
                yield break;

            // ... import JSON

            PositionsDB.SetStatusImporting(this, false);
        }

    }
}
