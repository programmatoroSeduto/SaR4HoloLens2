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
        [Tooltip("Pretty JSON export (more readable, but also heavier)")]
        public bool PrettyExport = false;



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
                ExportJsonFullFile(PrettyExport);
        }

        public void ExportJsonFullFile(bool pretty = false)
        {
            if (TryInit())
                StorageHub.WriteOneShot(JsonFileName, "json", ExportJsonFullCode(pretty), useTimestamp: UseFileNameTimestamp);
        }

        public string ExportJsonFullCode(bool pretty = false)
        {
            StaticLogger.Info(this, "JSON export FULL START");

            if (!TryInit())
            {
                StaticLogger.Warn(this, "Unable to export data! Can't init the ExportUtility class; returning empty JSON", logLayer: 1);
                return "{}";
            }

            if(PositionsDB.LowLevelDatabase.Count == 0)
            {
                StaticLogger.Warn(this, "Nothing to export", logLayer: 2);
                return "{}";
            }

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

            string jsonCode = JsonUtility.ToJson(dump, pretty);
            StaticLogger.Info(this, "JSON export FULL END", logLayer: 1);
            StaticLogger.Info(this, "JSON code exported:\n\t" + jsonCode + "\n\n", logLayer: 3);
            return jsonCode;
        }



        // ===== FEATURE FULL IMPORT ===== //

        public void EVENT_ImportJson(bool merge = true)
        {
            if (!TryInit() || isImporting) return;

            isImporting = true;
            StartCoroutine(merge ? BSCOR_ImportJsonMerge() : BSCOR_ImportJsonFull());
        }

        public bool ImportJsonFromCode(string jsonCode, bool merge = true)
        {
            if (!TryInit() || isImporting) return false;

            isImporting = true;
            StartCoroutine(merge ? BSCOR_ImportJsonMerge(jsonCode) : BSCOR_ImportJsonFull(jsonCode));
            return true;
        }

        public IEnumerator BSCOR_ImportJsonMerge(string jsonCode = "")
        {
            yield return null;
            StaticLogger.Info(this, "JSON import MERGE START");

            if (!TryInit() || !PositionsDB.SetStatusImporting(this, true))
                yield break;

            // lettura JSON
            if (jsonCode == "")
            {
                StaticLogger.Info(this, "JSON import from file; reading file ...");
                yield return StorageHub.ReadOneShot($"{JsonFileName}.json");
                jsonCode = StorageHub.FileContent;
            }
            StaticLogger.Info(this, "getting JSON code ...");
            JSONMaker jm = new JSONMaker();
            JSONPositionDatabase jdb = JsonUtility.FromJson<JSONPositionDatabase>(jsonCode);
            StaticLogger.Info(this, "getting JSON code ... OK");

            // check reference
            if (StaticTransform.ReferencePositionID != jdb.ReferenceID)
            {
                StaticLogger.Warn(this, $"HoloLens2 device reference position is {StaticTransform.ReferencePositionID} but got referenceID {jdb.ReferenceID} from file to import. \n\tSaR4HL2 actively prevents to import maks with different reference IDs since this could lead to data unconsistencies and visualization issues. \n\tUnable to import.", logLayer: 0);
                yield break;
            }

            // primo setup db
            StaticLogger.Info(this, "setting up DB ... ");
            jm.FromJsonClass(jdb, PositionsDB);
            StaticLogger.Info(this, "setting up DB ... OK");

            // oridinamento low level rispetto alriferimento comune
            Vector3 wpRef = JSONMaker.JSONToVector3(jdb.CurrentZone.AreaCenter);
            StaticLogger.Info(this, $"sorting DB by reference ({wpRef.x},{wpRef.y},{wpRef.z}) ... ");
            PositionsDB.LowLevelDatabase.SortReferencePosition = wpRef;
            PositionsDB.LowLevelDatabase.SortAll();
            StaticLogger.Info(this, $"sorting DB by reference ({wpRef.x},{wpRef.y},{wpRef.z}) ... OK");

            // caricamento waypoints e segna gli archi trovati
            StaticLogger.Info(this, "loading local renamings ... ");
            Dictionary<int, int> AreaRenamingLocal = JSONMaker.JSONToDict<int, int>(jdb.AreaRenaming);
            Dictionary<int, List<PositionDatabaseWaypoint>> AreaWp = new Dictionary<int, List<PositionDatabaseWaypoint>>();
            HashSet<int> UnresolvedAreaLocal = new HashSet<int>();
            foreach (var tup in AreaRenamingLocal)
            {
                AreaWp.Add(tup.Key, new List<PositionDatabaseWaypoint>());
                UnresolvedAreaLocal.Add(tup.Key);
            }
            StaticLogger.Info(this, $"loading local renamings ... OK found {AreaRenamingLocal.Count} renamings");

            HashSet<JSONPath> links = new HashSet<JSONPath>();
            Dictionary<string, PositionDatabaseWaypoint> LocalKeyToWp = new Dictionary<string, PositionDatabaseWaypoint>();
            HashSet<PositionDatabaseWaypoint> mergedLocations = new HashSet<PositionDatabaseWaypoint>();

            StaticLogger.Info(this, "Loading waypoints START");
            foreach (JSONWaypoint jwp in jdb.Waypoints)
            {
                StaticLogger.Info(this, $"(from JSON) jwp with ID:{jwp.Key}");

                // max dist dal ref
                Vector3 wpPos = JSONMaker.JSONToVector3(jwp.FirstAreaCenter);
                float maxDist = Vector3.Distance(wpPos, wpRef);
                PositionDatabaseWaypoint wp = new PositionDatabaseWaypoint();
                jm.FromJsonClass(jwp, wp);

                // in ogni caso tieni da parte gli archi
                foreach (JSONPath p in jwp.Paths)
                    links.Add(p);

                // scorri la lista, tenta di trovarne uno vicino (usa la max dist per non cercare in tutto il DB)
                for (int i = 0; i < PositionsDB.LowLevelDatabase.Count; ++i)
                {
                    PositionDatabaseWaypoint dbwp = PositionsDB.LowLevelDatabase.Database[i];
                    if (Vector3.Distance(wp.AreaCenter, dbwp.AreaCenter) <= 2.0f * PositionsDB.BaseDistance - PositionsDB.DistanceTolerance)
                    {
                        // se trovi, --> correggi la pos del marker, assegna l'area finale (segna la conversione)
                        StaticLogger.Info(this, $"jwp with ID:{jwp.Key} merge with ID{wp.PositionID}");
                        dbwp.AreaCenter = wp.AreaCenter;
                        AreaRenamingLocal[wp.AreaIndex] = PositionsDB.AreaRenamingLookup[dbwp.AreaIndex];
                        UnresolvedAreaLocal.Remove(wp.AreaIndex);
                        dbwp.Description = wp.Description;
                        LocalKeyToWp.Add(wp.Key, dbwp);
                        mergedLocations.Add(dbwp);

                        PositionsDB.LowLevelDatabase.SortAll();

                        break;
                    }
                    else if (Vector3.Distance(wpRef, dbwp.AreaCenter) >= maxDist || i == PositionsDB.LowLevelDatabase.Count - 1)
                    {
                        // altrimenti, inserisci nel DB e nel dubbio tieni da parte
                        StaticLogger.Info(this, $"(from JSON) jwp with ID:{jwp.Key} is new point of the set");
                        PositionsDB.LowLevelDatabase.Database.Add(wp);
                        wp.setPositionID(PositionsDB.LowLevelDatabase.Count);
                        AreaWp[wp.AreaIndex].Add(wp);
                        LocalKeyToWp.Add(wp.Key, wp);

                        PositionsDB.LowLevelDatabase.SortAll();

                        break;
                    }
                }
            }
            StaticLogger.Info(this, "Loading waypoints END");

            // caricamento archi
            StaticLogger.Info(this, "Loading edges START");
            foreach (JSONPath p in links)
            {
                StaticLogger.Info(this, $"(from JSON) path with ID:{p.Key}");

                PositionDatabaseWaypoint wp1 = LocalKeyToWp[p.Waypoint1];
                PositionDatabaseWaypoint wp2 = LocalKeyToWp[p.Waypoint2];

                if (!wp1.IsLinkedWith(wp2))
                {
                    StaticLogger.Info(this, $"path with ID:{p.Key} adding link");
                    wp1.AddPath(wp2);
                }

                if (!mergedLocations.Contains(wp1) && mergedLocations.Contains(wp2) && UnresolvedAreaLocal.Contains(wp1.AreaIndex))
                {
                    AreaRenamingLocal[wp1.AreaIndex] = PositionsDB.AreaRenamingLookup[wp2.AreaIndex];
                    UnresolvedAreaLocal.Remove(wp1.AreaIndex);
                }
                else if (mergedLocations.Contains(wp1) && !mergedLocations.Contains(wp2) && UnresolvedAreaLocal.Contains(wp2.AreaIndex))
                {
                    AreaRenamingLocal[wp2.AreaIndex] = PositionsDB.AreaRenamingLookup[wp1.AreaIndex];
                    UnresolvedAreaLocal.Remove(wp2.AreaIndex);
                }
            }
            StaticLogger.Info(this, "Loading edges END");

            // risoluzione zone non ancora associate
            foreach (int unresolvedArea in UnresolvedAreaLocal)
            {
                int AreaGlobal = PositionsDB.AreaRenamingLookup.Count;
                PositionsDB.AreaRenamingLookup.Add(AreaGlobal, AreaGlobal);
                AreaRenamingLocal[unresolvedArea] = AreaGlobal;
            }

            // infine aggiornamento delle aree
            StaticLogger.Info(this, "Final operations START");
            foreach (PositionDatabaseWaypoint wp in LocalKeyToWp.Values)
            {
                if (!mergedLocations.Contains(wp))
                {
                    wp.AreaIndex = AreaRenamingLocal[wp.AreaIndex];
                }
            }
            StaticLogger.Info(this, "Final operations END");

            PositionsDB.SetStatusImporting(this, false);
            isImporting = false;

            StaticLogger.Info(this, "JSON import MERGE END");
        }

        public IEnumerator BSCOR_ImportJsonFull(string jsonCode = "")
        {
            yield return null;
            StaticLogger.Info(this, "JSON import FULL START");

            if (!TryInit() || !PositionsDB.SetStatusImporting(this, true))
                yield break;

            // lettura JSON
            if (jsonCode == "")
            {
                StaticLogger.Info(this, "JSON import from file; reading file ...");
                yield return StorageHub.ReadOneShot($"{JsonFileName}.json");
                jsonCode = StorageHub.FileContent;
            }
            StaticLogger.Info(this, "getting JSON code ...");
            JSONMaker jm = new JSONMaker();
            JSONPositionDatabase jdb = JsonUtility.FromJson<JSONPositionDatabase>(jsonCode);
            StaticLogger.Info(this, "getting JSON code ... OK");

            // check reference
            if( StaticTransform.ReferencePositionID != jdb.ReferenceID )
            {
                StaticLogger.Warn(this, $"HoloLens2 device reference position is {StaticTransform.ReferencePositionID} but got referenceID {jdb.ReferenceID} from file to import. \n\tSaR4HL2 actively prevents to import maks with different reference IDs since this could lead to data unconsistencies and visualization issues. \n\tUnable to import.", logLayer: 0);
                yield break;
            }

            // clean status
            StaticLogger.Info(this, "cleaning DB status ... ");
            PositionsDB.LowLevelDatabase.Database.Clear();
            PositionsDB.AreaRenamingLookup.Clear();
            StaticLogger.Info(this, "cleaning DB status ... OK ");

            // import db settings
            StaticLogger.Info(this, "setting up DB ... ");
            jm.FromJsonClass(jdb, PositionsDB);
            PositionsDB.AreaRenamingLookup = JSONMaker.JSONToDict<int, int>(jdb.AreaRenaming);
            StaticLogger.Info(this, "setting up DB ... OK");

            HashSet<JSONPath> links = new HashSet<JSONPath>();
            Dictionary<string, PositionDatabaseWaypoint> wpLookup = new Dictionary<string, PositionDatabaseWaypoint>();

            // create waypoints
            StaticLogger.Info(this, "Loading waypoints START");
            foreach (JSONWaypoint jwp in jdb.Waypoints)
            {
                StaticLogger.Info(this, $"(from JSON) jwp with ID:{jwp.Key}");

                PositionDatabaseWaypoint wp = new PositionDatabaseWaypoint();
                jm.FromJsonClass(jwp, wp);
                wp.AreaIndex = PositionsDB.AreaRenamingLookup[wp.AreaIndex];
                PositionsDB.LowLevelDatabase.Database.Add(wp);
                wpLookup.Add(wp.Key, wp);

                foreach (JSONPath p in jwp.Paths)
                    links.Add(p);
            }
            StaticLogger.Info(this, "Loading waypoints END");

            // create paths 
            StaticLogger.Info(this, "Loading edges START");
            foreach (JSONPath p in links)
            {
                wpLookup[p.Waypoint1].AddPath(wpLookup[p.Waypoint2]);
            }
            StaticLogger.Info(this, "Loading edges END");

            // sort wrt the current reference
            PositionsDB.OnEnable(true);

            PositionsDB.SetStatusImporting(this, false);
            isImporting = false;

            StaticLogger.Info(this, "JSON import FULL END");
        }
    }
}
