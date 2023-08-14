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
        [Tooltip("Name of the JSON export/import file")]
        public string JsonFileName = "db_export";
        [Tooltip("Wether using the timestamp at the end of the file name or not (suggested: false)")]
        public bool UseFileNameTimestamp = false;



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
                this.StorageHub = StaticAppSettings.GetObject(GlobalStorageHubName, null) as StorageHubOneShot;
                if (StorageHub == null) return false;
            }
            
            return true;
        }



        // ===== FEATURE FULL EXPORT ===== //

        public void EVENT_ExportJsonFull()
        {
            ExportJsonFull();
        }

        public void ExportJsonFull()
        {
            if (!TryInit()) return;

            JSONMaker jm = new JSONMaker();
            JSONPositionDatabase dump = jm.ToJsonClass(PositionsDB);

            foreach (PositionDatabaseWaypoint wp in lowLevel.Database)
            {
                JSONWaypoint jsonWp = jm.ToJsonClass(wp);

                foreach (PositionDatabasePath link in wp.Paths)
                {
                    if (link.Key.StartsWith(wp.Key))
                    {
                        jsonWp.Paths.Add(jm.ToJsonClass(link));
                    }
                }
                dump.Waypoints.Add(jsonWp);
            }

            StorageHub.WriteOneShot(JsonFileName, "json", JsonUtility.ToJson(dump, true), useTimestamp: UseFileNameTimestamp);
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
