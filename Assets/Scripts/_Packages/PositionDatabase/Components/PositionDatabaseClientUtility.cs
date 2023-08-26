using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

using Project.Scripts.Components;
using Project.Scripts.Utils;

using Packages.DiskStorageServices.Components;
using Packages.PositionDatabase.Utils;
using Packages.SAR4HL2NetworkingServices.Components;
using Packages.SAR4HL2NetworkingServices.Utils;


namespace Packages.PositionDatabase.Components
{
    public class PositionDatabaseClientUtility : MonoBehaviour
    {
        // ===== GUI ===== //

        [Header("Basic Properties")]
        [Tooltip("Init on start?")]
        public bool InitOnStart = false;
        [Tooltip("Reference to the positions database")]
        public PositionsDatabase PositionsDB = null;
        [Tooltip("Reference to the SAR client")]
        public SarHL2Client Client = null;
        [Tooltip("Use timing update")]
        public bool UseTimedUpdate = true;
        [Tooltip("Update time (default: each 60 seconds)")]
        public float TimedUpdatePeriodSecs = 60.0f;
        [Tooltip("Use minimum number of new points inside the DB")]
        public bool UseMinNumberOfPoints = true;
        [Tooltip("Check for a minimum number of new points inside the database position (set to -1 if unused)")]
        public int MinNumberOfPointsForUpdate = 5;
        [Tooltip("Max retry count for connection")]
        public int MaxRetryCountConnection = 10;
        [Tooltip("Connection Retry Delay")]
        public float ConnectionRetryDelay = 30.0f;
        [Tooltip("Update Radius")]
        public float UpdateRadius = 30.0f;



        // ===== PRIVATE ===== //

        // init done
        private bool init = false;
        // reference to the low level from the database (it is a 'init' variable as well)
        private PositionDatabaseLowLevel lowLevel = null;
        // ...
        private Coroutine COR_MainWorkingCycle = null;
        // ...
        private UnityEvent onInsertEventHandle = null;
        // ...
        private List<PositionDatabaseWaypoint> newPositions = new List<PositionDatabaseWaypoint>();
        // ...
        private List<PositionDatabasePath> newPaths = new List<PositionDatabasePath>();
        // user only for logging
        private string classLogSource = "PositionDatabaseClientUtility";
        // miss count for download
        private int downloadMiss = 0;
        // hit count for download
        private int downloadHit = 0;



        // ===== UNITY CALLBACKS AND INIT ===== //

        private void Start()
        {
            TryInit();
        }

        private bool TryInit()
        {
            string sourceLog = $"{classLogSource}:TryInit";
            StaticLogger.Info(sourceLog, "init ... ", logLayer: 2);

            if (PositionsDB != null && lowLevel != null && Client != null && this.COR_MainWorkingCycle != null && onInsertEventHandle != null) return true;

            // init DB
            if (PositionsDB != null)
                lowLevel = PositionsDB.LowLevelDatabase;
            else
            {
                StaticLogger.Warn(sourceLog, "Position Database reference is NULL", logLayer: 1);
                return false;
            }

            if (Client == null)
            {
                StaticLogger.Warn(sourceLog, "Client SarHL2Client reference is NULL", logLayer: 1);
                return false;
            }

            init = true;

            onInsertEventHandle = new UnityEvent();
            onInsertEventHandle.AddListener(CALLBACK_OnInsert);
            PositionsDB.CallOnZoneCreated.Add(onInsertEventHandle);
            
            COR_MainWorkingCycle = StartCoroutine(ORCOR_MainWorkingCycle());

            StaticLogger.Info(sourceLog, "init ... OK", logLayer: 2);
            return true;
        }

        private void CALLBACK_OnInsert()
        {
            string sourceLog = $"{classLogSource}:CALLBACK_OnInsert";

            StaticLogger.Info(sourceLog, $"receiving new position ... ", logLayer: 2);
            PositionDatabaseWaypoint wp = PositionsDB.DataZoneCreated;
            if(wp.AreaIndex == 0)
            {
                newPositions.Add(PositionsDB.DataZoneCreated);
                foreach (PositionDatabasePath pt in wp.Paths)
                    newPaths.Add(pt);
            }
            StaticLogger.Info(sourceLog, $"receiving new position ... OK with new Count:{newPositions.Count}", logLayer: 2);
        }



        // ===== MAIN WORKING CYCLE ===== //

        public IEnumerator ORCOR_MainWorkingCycle()
        {
            string sourceLog = $"{classLogSource}:ORCOR_MainWorkingCycle";
            yield return null;
            StaticLogger.Info(sourceLog, $"BEGIN COROUTINE ORCOR_MainWorkingCycle", logLayer: 2);

            // check the connection first
            if (!Client.Online)
            {
                StaticLogger.Info(sourceLog, $"System seems not connect; trying to connect", logLayer: 2);
                yield return BSCOR_ConnectClient();
                if (!Client.Online)
                {
                    StaticLogger.Err(sourceLog, $"Unable to connect (retry failed); terminating coroutine ...");
                    COR_MainWorkingCycle = null;
                    init = false;
                    yield break;
                }
                StaticLogger.Info(sourceLog, $"Connection: OK", logLayer: 2);
            }

            // waiting for calibration
            StaticLogger.Info(sourceLog, $"Waiting for calibration ...", logLayer: 0);
            int waitCount = 0;
            while(StaticTransform.CalibrationComponent == null || !StaticTransform.CalibrationComponent.CalibrationDone)
            {
                yield return new WaitForSecondsRealtime(1.0f);
                StaticLogger.Warn(sourceLog, $"({waitCount++}) waiting ... ", logLayer: 3);
            }
            StaticLogger.Info(sourceLog, $"Waiting for calibration ... OK: calibration done", logLayer: 0);

            // first download
            StaticLogger.Info(sourceLog, $"First download from server ...", logLayer: 0);
            yield return BSCOR_Download();
            StaticLogger.Info(sourceLog, $"First download from server ... OK: dataset aligned", logLayer: 0);
        }

        public IEnumerator BSCOR_ConnectClient()
        {
            string sourceLog = $"{classLogSource}:BSCOR_ConnectClient";
            yield return null;
            StaticLogger.Info(sourceLog, $"trying to connect ...", logLayer: 2);

            int remainingAttempts = MaxRetryCountConnection;
            while(--remainingAttempts >= 0)
            {
                StaticLogger.Info(sourceLog, $"Remaining attempts:{remainingAttempts}", logLayer: 3);
                yield return Client.ORCOR_connect();
                if(!Client.Online)
                {
                    StaticLogger.Warn(sourceLog, $"Attempt failed!", logLayer: 3);
                    continue;
                }
                else
                {
                    StaticLogger.Info(sourceLog, $"Connected", logLayer: 3);
                    yield break;
                }
            }
        }



        // ===== DOWNLOAD FEATURE ===== //

        public IEnumerator BSCOR_Download()
        {
            string sourceLog = $"{classLogSource}:BSCOR_Download";
            yield return null;
            
            StaticLogger.Info(sourceLog, $"Calling download from server ...", logLayer: 1);
            yield return Client.ORCOR_DownloadFromServer(PositionsDB.CurrentZone.AreaCenter, UpdateRadius);
            if (!Client.Success)
            {
                StaticLogger.Warn(sourceLog, $"Calling download from server... ERROR: cannot download from server (miss:{++downloadMiss})", logLayer: 0);
                yield break;
            }
            else ++downloadHit;
            StaticLogger.Info(sourceLog, $"Calling download from server ... OK (hit:{downloadHit})", logLayer: 1);

            if (Client.UpdatedEntriesWps.Count == 0)
            {
                StaticLogger.Info(sourceLog, $"Nothing to integrate; closing download", logLayer: 1);
                yield break;
            }

            StaticLogger.Info(sourceLog, $"Importing waypoints ... ", logLayer: 2);
            PositionsDB.SetStatusImporting(this, true);
            int wpCacheIndex = Client.ServerPositionIndex;
            foreach (Tuple<int, int, data_hl2_waypoint> jwp in Client.UpdatedEntriesWps)
            {
                /*
                 * è impossibile che nel download salgano dei rename. Tutte le posizioni hanno ID nuovi
                 * */
                PositionDatabaseWaypoint wp = GetWaypointFromJsonClass(jwp.Item3);

                if (PositionsDB.LowLevelDatabase.WpIndex.ContainsKey(jwp.Item2))
                {
                    if(PositionsDB.LowLevelDatabase.WpIndex[jwp.Item2] != null)
                    {
                        // c'è stato qualche nuovo inserimento mentre si svolgeva il dowload dei dati

                        PositionDatabaseWaypoint wpNew = PositionsDB.LowLevelDatabase.WpIndex[jwp.Item2];
                        wpNew.setPositionID(wpCacheIndex++);
                        newPositions.Add(wpNew);
                        foreach (PositionDatabasePath pt in wp.Paths)
                            newPaths.Add(pt);
                    }
                    PositionsDB.LowLevelDatabase.WpIndex[jwp.Item2] = wp;
                    yield return new WaitForEndOfFrame();
                }
                else
                    PositionsDB.LowLevelDatabase.WpIndex.Add(jwp.Item2, wp);
                PositionsDB.LowLevelDatabase.Database.Add(wp);
            }
            PositionsDB.LowLevelDatabase.MaxSharedIndex = Client.ServerPositionIndex;
            StaticLogger.Info(sourceLog, $"Importing waypoints ... OK", logLayer: 1);

            StaticLogger.Info(sourceLog, $"Importing paths ... ", logLayer: 2);
            foreach(var jpt in Client.UpdatedEntriesPaths)
            {
                int wp1Idx = jpt.Item2.Item1;
                int wp2Idx = jpt.Item2.Item2;
                if(!PositionsDB.LowLevelDatabase.WpIndex.ContainsKey(wp1Idx))
                {
                    StaticLogger.Warn(sourceLog, $"Importing paths ... ERROR: waypoint with code wp1Idx:{wp1Idx} does not exist)", logLayer: 0);
                    continue;
                }
                else if (!PositionsDB.LowLevelDatabase.WpIndex.ContainsKey(wp2Idx))
                {
                    StaticLogger.Warn(sourceLog, $"Importing paths ... ERROR: waypoint with code wp2Idx:{wp2Idx} does not exist)", logLayer: 0);
                    continue;
                }

                PositionsDB.LowLevelDatabase.WpIndex[wp1Idx].AddPath(PositionsDB.LowLevelDatabase.WpIndex[wp2Idx]);
            }
            StaticLogger.Info(sourceLog, $"Importing paths ... OK", logLayer: 1);
            PositionsDB.SetStatusImporting(this, false);
        }



        // ===== UTILITIES ===== //

        private PositionDatabaseWaypoint GetWaypointFromJsonClass(data_hl2_waypoint jsonWp)
        {
            PositionDatabaseWaypoint wp = new PositionDatabaseWaypoint();
            wp.setPositionID(jsonWp.pos_id); // already shared in download
            wp.AreaIndex = jsonWp.area_id;
            DateTime.TryParse(jsonWp.wp_timestamp, out wp.Timestamp);
            wp.AreaCenter = new Vector3(jsonWp.v[0], jsonWp.v[1], jsonWp.v[2]);
            wp.DBReference = PositionsDB;
            wp.AreaRadius = PositionsDB.BaseDistance;

            return wp;
        }

    }
}
