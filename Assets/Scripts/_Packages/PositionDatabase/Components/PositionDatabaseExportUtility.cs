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
        // wether the class is importing or not
        private bool isImporting = false;



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
            if (TryInit())
                ExportJsonFullFile();
        }

        public void ExportJsonFullFile()
        {
            if (TryInit())
                StorageHub.WriteOneShot(JsonFileName, "json", ExportJsonFullCode(), useTimestamp: UseFileNameTimestamp);
        }

        public string ExportJsonFullCode()
        {
            if (!TryInit()) return "{}";

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

            return JsonUtility.ToJson(dump, true);
        }



        // ===== FEATURE FULL IMPORT ===== //

        public void EVENT_ImportJson()
        {
            if (!TryInit() || isImporting) return;

            isImporting = true;
            StartCoroutine(BSCOR_ImportJson());
        }

        public bool ImportJsonFromCode(string jsonCode)
        {
            if (!TryInit() || isImporting) return false;

            isImporting = true;
            StartCoroutine(BSCOR_ImportJson(jsonCode));
            return true;
        }

        public IEnumerator BSCOR_ImportJson(string jsonCode = "")
        {
            yield return null;

            if (!TryInit() || !PositionsDB.SetStatusImporting(this, true))
                yield break;

            // lettura JSON
            if(jsonCode == "")
            {
                yield return StorageHub.ReadOneShot($"{JsonFileName}.json");
                jsonCode = StorageHub.FileContent;
            }
            JSONMaker jm = new JSONMaker();
            JSONPositionDatabase jdb = JsonUtility.FromJson<JSONPositionDatabase>(jsonCode);

            // primo setup db
            jm.FromJsonClass(jdb, PositionsDB);

            // oridinamento low level rispetto alriferimento comune
            PositionsDB.LowLevelDatabase.SortReferencePosition = JSONMaker.JSONToVector3(jdb.CurrentZone.AreaCenter);
            PositionsDB.LowLevelDatabase.SortAll();

            // caricamento waypoints e segna gli archi trovati
            Dictionary<int, int> AreaRenamingLocal = JSONMaker.JSONToDict<int, int>(jdb.AreaRenaming);
            Dictionary<int, List<PositionDatabaseWaypoint>> AreaWp = new Dictionary<int, List<PositionDatabaseWaypoint>>();
            foreach (var tup in AreaRenamingLocal)
                AreaWp.Add(tup.Key, new List<PositionDatabaseWaypoint>());

            // applicaione delle zone fin da subito
            // se trovo un punto vicino di una certa zona, propago la zona mia ai nuovi inserimenti
            Dictionary<PositionDatabaseWaypoint, JSONPath> links = new Dictionary<PositionDatabaseWaypoint, JSONPath>();

            // ----- LA RISPOSTA ----- //
            Dictionary<string, PositionDatabaseWaypoint> LocalKeyToWp = new Dictionary<string, PositionDatabaseWaypoint>();
            // ----- LA RISPOSTA ----- //

            Vector3 wpRef = PositionsDB.LowLevelDatabase.SortReferencePosition;
            foreach (JSONWaypoint jwp in jdb.Waypoints)
            {
                // max dist dal ref
                Vector3 wpPos = JSONMaker.JSONToVector3(jwp.FirstAreaCenter);
                float maxDist = Vector3.Distance(wpPos, wpRef);
                PositionDatabaseWaypoint wp = new PositionDatabaseWaypoint(); 
                jm.FromJsonClass(jwp, wp);

                // in ogni caso tieni da parte gli archi
                foreach (JSONPath p in jwp.Paths)
                    links.Add(wp, p);

                // scorri la lista, tenta di trovarne uno vicino (usa la max dist per non cercare in tutto il DB)
                for (int i=0; i<PositionsDB.LowLevelDatabase.Count; ++i)
                {
                    PositionDatabaseWaypoint dbwp = PositionsDB.LowLevelDatabase.Database[i];
                    if( Vector3.Distance(wp.AreaCenter, dbwp.AreaCenter) <= PositionsDB.BaseDistance)
                    {
                        // se trovi, --> correggi la pos del marker, assegna l'area finale (segna la conversione)
                        dbwp.AreaCenter = wp.AreaCenter;
                        AreaRenamingLocal[wp.AreaIndex] = PositionsDB.AreaRenamingLookup[dbwp.AreaIndex];

                        break;
                    }
                    else if (Vector3.Distance(wpRef, dbwp.AreaCenter) >= maxDist || i == PositionsDB.LowLevelDatabase.Count - 1)
                    {
                        // altrimenti, inserisci nel DB e nel dubbio tieni da parte
                        PositionsDB.LowLevelDatabase.Database.Add(wp);
                        AreaWp[wp.AreaIndex].Add(wp);

                        break;
                    }
                }
            }

            // caricamento archi
            foreach(KeyValuePair<PositionDatabaseWaypoint, JSONPath> tup in links)
            {
                // se il wp è già connesso al waypoint 2 ... ???
                // allora continua
            }

            // infine aggiornamento finale delle aree

            PositionsDB.SetStatusImporting(this, false);
            isImporting = false;
        }

    }
}
