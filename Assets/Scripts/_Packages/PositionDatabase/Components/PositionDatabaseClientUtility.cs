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
    public class PositionDatabaseClientUtility : ProjectMonoBehaviour
    {
        // ===== GUI ===== //

        [Header("Basic Properties")]
        [Tooltip("Init on start?")]
        public bool InitOnStart = false;
        [Tooltip("Reference to the positions database")]
        public PositionsDatabase PositionsDB = null;
        [Tooltip("Reference to the SAR client")]
        public SarHL2Client Client = null;
        [Tooltip("Upload time (default: each 60 seconds)")]
        public float UploadTime = 60.0f;
        [Tooltip("Download time (default: each 120 seconds). Priority is given to the download")]
        public float DownloadTime = 120.0f;
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



        // ===== UNITY CALLBACKS AND INIT ===== //

        private void Start()
        {
            if(InitOnStart)
                TryInit();
        }

        public bool TryInit()
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
            Ready();
            return true;
        }

        private void CALLBACK_OnInsert()
        {
            string sourceLog = $"{classLogSource}:CALLBACK_OnInsert";

            StaticLogger.Info(sourceLog, $"receiving new position ... ", logLayer: 2);
            PositionDatabaseWaypoint wp = PositionsDB.DataZoneCreated;
            if(wp.AreaIndex == 0 && wp.PositionStableID != 0)
            {
                newPositions.Add(PositionsDB.DataZoneCreated);
                foreach (PositionDatabasePath pt in wp.Paths)
                    newPaths.Add(pt);
            }
            else if (wp.PositionStableID == 0)
            {
                StaticLogger.Info(sourceLog, $"receiving new position ... SKIP (the point is the origin of the reference frame)", logLayer: 2);
                return;
            }
            else if (wp.AreaIndex == 0)
            {
                // TODO: when the area index becomes zero from another number, it's time to upload data to the server (this scenario is currently not managed)
                StaticLogger.Info(sourceLog, $"receiving new position ... SKIP (area index is not zero)", logLayer: 2);
                return;
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
            while(StaticTransform.CalibrationComponent == null || !StaticTransform.CalibrationDone)
            {
                yield return new WaitForSecondsRealtime(1.0f);
                StaticLogger.Info(sourceLog, $"({waitCount++}) waiting ... (nullRef:{(StaticTransform.CalibrationComponent == null)})", logLayer: 3);
            }
            StaticLogger.Info(sourceLog, $"Waiting for calibration ... OK: calibration done", logLayer: 0);
    
            StaticLogger.Info(sourceLog, $"Waiting for first position from position database ...", logLayer: 0);
            waitCount = 0;
            while (PositionsDB.CurrentZone == null)
            {
                yield return new WaitForSecondsRealtime(1.0f);
                StaticLogger.Info(sourceLog, $"({waitCount++}) waiting ... )", logLayer: 3);
            }
            StaticLogger.Info(sourceLog, $"Waiting for first position from position database ... OK", logLayer: 0);

            // first download
            StaticLogger.Info(sourceLog, $"First download from server ...", logLayer: 0);
            yield return BSCOR_Download();
            if(!Client.Success)
            {
                StaticLogger.Err(sourceLog, $"Error during first download; terminating coroutine ...");
                COR_MainWorkingCycle = null;
                init = false;
                yield break;
            }
            StaticLogger.Info(sourceLog, $"First download from server ... OK: starting inner working cycle", logLayer: 0);

            yield return BSCOR_InnerWorkingCycle();
            // yield return BSCOR_Test();
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

        public IEnumerator BSCOR_Test()
        {
            string sourceLog = $"{classLogSource}:BSCOR_Test";
            yield return null;

            StaticLogger.Info(sourceLog, $"waiting at least 5 positions to send ... ", logLayer: 3);
            while (newPositions.Count < 5)
            {
                yield return new WaitForSecondsRealtime(5.0f);
                StaticLogger.Info(sourceLog, $"still waiting -- collected positions so far: {newPositions.Count} ", logLayer: 3);
            }
            StaticLogger.Info(sourceLog, $"waiting at least 5 positions to send ... OK: time to upload", logLayer: 3);

            StaticLogger.Info(sourceLog, $"UPLOAD TEST ... ", logLayer: 3);
            yield return BSCOR_Upload();
            if (!Client.Success)
            {
                StaticLogger.Err(sourceLog, $"Unable to upload!", logLayer: 1);
                yield break;
            }
            StaticLogger.Info(sourceLog, $"UPLOAD TEST ... OK", logLayer: 3);
        }


        public IEnumerator BSCOR_InnerWorkingCycle()
        {
            string sourceLog = $"{classLogSource}:ORCOR_InnerWorkingCycle";
            yield return null;

            float remainingUploadTime = UploadTime;
            float remainingDownloadTime = DownloadTime;
            float timeUnit = 5.0f;
            int retryCount = 10;

            while(retryCount > 0)
            {
                StaticLogger.Info(sourceLog, $"waiting ... ", logLayer: 3);
                yield return new WaitForSecondsRealtime(timeUnit);
                StaticLogger.Info(sourceLog, $"waiting ... OK updating remaining time", logLayer: 3);

                remainingUploadTime -= timeUnit;
                remainingDownloadTime -= timeUnit;

                if(remainingDownloadTime <= 0)
                {
                    StaticLogger.Info(sourceLog, $"time to download!", logLayer: 1);

                    // ... perform upload
                    StaticLogger.Info(sourceLog, $"uploading ... ", logLayer: 2);
                    yield return BSCOR_Upload();
                    if(!Client.Success)
                    {
                        StaticLogger.Warn(sourceLog, $"Unable to upload!", logLayer: 1);
                        --retryCount;
                        continue;
                    }
                    StaticLogger.Info(sourceLog, $"uploading ... OK", logLayer: 2);

                    // ... perform download
                    StaticLogger.Info(sourceLog, $"downloading ... ", logLayer: 2);
                    yield return BSCOR_Download();
                    if (!Client.Success)
                    {
                        StaticLogger.Warn(sourceLog, $"Unable to download!", logLayer: 1);
                        --retryCount;
                        continue;
                    }
                    StaticLogger.Info(sourceLog, $"downloading ... OK", logLayer: 2);

                    remainingUploadTime = UploadTime;
                    remainingDownloadTime = DownloadTime;
                    retryCount = 10;
                }
                else if (remainingUploadTime <= 0)
                {
                    StaticLogger.Info(sourceLog, $"time to upload!", logLayer: 1);
                    if(newPositions.Count == 0)
                    {
                        StaticLogger.Info(sourceLog, $"No new position to upload; skipping update...", logLayer: 1);
                        continue;
                    }

                    // ... perform upload
                    StaticLogger.Info(sourceLog, $"uploading ... ", logLayer: 2);
                    yield return BSCOR_Upload();
                    if (!Client.Success)
                    {
                        StaticLogger.Warn(sourceLog, $"Unable to upload!", logLayer: 1);
                        --retryCount;
                        continue;
                    }
                    StaticLogger.Info(sourceLog, $"uploading ... OK", logLayer: 2);

                    remainingUploadTime = UploadTime;
                    retryCount = 10;
                }
            }

            StaticLogger.Err(sourceLog, $"Unable to perform inner cycle (many errors occurred, exhausted attempts)");
        }



        // ===== DOWNLOAD FEATURE ===== //

        public IEnumerator BSCOR_Download()
        {
            string sourceLog = $"{classLogSource}:BSCOR_Download";
            yield return null;
            
            StaticLogger.Info(sourceLog, $"Calling API download from server ...", logLayer: 1);
            if(PositionsDB.CurrentZone.AreaCenter == null)
                StaticLogger.Warn(sourceLog, $"PositionsDB.CurrentZone.AreaCenter == null : {PositionsDB.CurrentZone.AreaCenter == null}", logLayer: 1);
            yield return Client.ORCOR_DownloadFromServer(StaticTransform.ToRefPoint(PositionsDB.CurrentZone.AreaCenter), UpdateRadius);
            if (!Client.Success)
            {
                StaticLogger.Warn(sourceLog, $"Calling download from server... ERROR: cannot download from server", logLayer: 0);
                yield break;
            }
            StaticLogger.Info(sourceLog, $"Calling download from server ... OK", logLayer: 1);

            if (Client.UpdatedEntriesWps.Count == 0)
            {
                StaticLogger.Info(sourceLog, $"Nothing to integrate; closing download", logLayer: 1);
                yield break;
            }
            else
                StaticLogger.Info(sourceLog, $"found {Client.UpdatedEntriesWps.Count} waypoints to integrate", logLayer: 3);

            StaticLogger.Info(sourceLog, $"Importing waypoints ... ", logLayer: 2);
            PositionsDB.SetStatusImporting(this, true);
            int wpCacheIndex = Client.ServerPositionIndex;
            foreach (Tuple<int, int, data_hl2_waypoint> jwp in Client.UpdatedEntriesWps)
            {
                /*
                 * è impossibile che nel download salgano dei rename. Tutte le posizioni hanno ID req->loc coincidenti
                 * */
                PositionDatabaseWaypoint wp = GetWaypointFromJsonClass(jwp.Item3);
                StaticLogger.Info(sourceLog, $"{wp}", logLayer: 3);

                if (PositionsDB.LowLevelDatabase.WpIndex.ContainsKey(jwp.Item2))
                {
                    if(PositionsDB.LowLevelDatabase.WpIndex[jwp.Item2] != null)
                    {
                        // c'è stato qualche nuovo inserimento mentre si svolgeva il dowload dei dati
                        // l'inserimento è già stato catturato dalla CALLBACK
                        // l'elemento va spostato nell'indice
                        // oppure è un elemento che avevo allocato quando il mio MaxIdx era più basso
                        // (prima del download non potevo sapere quante fossero le posizioni, così nel dubbio ho allocato al primo posto disponibile)
                        int idx = wpCacheIndex++;
                        PositionsDB.LowLevelDatabase.WpIndex[jwp.Item2].setPositionID(idx);
                        // idx potrebbe essere più alto del massimo indice a disposizione sul dizionario, attenzione
                        if (PositionsDB.LowLevelDatabase.WpIndex.ContainsKey(idx))
                            PositionsDB.LowLevelDatabase.WpIndex[idx] = PositionsDB.LowLevelDatabase.WpIndex[jwp.Item2];
                        else
                            PositionsDB.LowLevelDatabase.WpIndex.Add(idx, PositionsDB.LowLevelDatabase.WpIndex[jwp.Item2]);
                        // PositionsDB.LowLevelDatabase.WpIndex[jwp.Item2] = null; // non serve qui, tanto la alloco sicuro dopo l'IF
                    }
                    PositionsDB.LowLevelDatabase.WpIndex[jwp.Item2] = wp;
                }
                else
                    PositionsDB.LowLevelDatabase.WpIndex.Add(jwp.Item2, wp);
                PositionsDB.LowLevelDatabase.Database.Add(wp);

                yield return new WaitForEndOfFrame(); // per alleggerire il carico computazionale sul frame
            }
            StaticLogger.Info(sourceLog, $"PositionsDB.LowLevelDatabase.MaxSharedIndex:{PositionsDB.LowLevelDatabase.MaxSharedIndex} Vs. Client.ServerPositionIndex:{Client.ServerPositionIndex} with wpCacheIndex:{wpCacheIndex}", logLayer: 2);
            if (PositionsDB.LowLevelDatabase.MaxSharedIndex < wpCacheIndex)
            {
                StaticLogger.Info(sourceLog, $"max IDX moved: {PositionsDB.LowLevelDatabase.MaxSharedIndex} -> wpCacheIndex:{wpCacheIndex}", logLayer: 3);
                PositionsDB.LowLevelDatabase.MaxSharedIndex = Client.ServerPositionIndex;
            }
            else
                StaticLogger.Info(sourceLog, $"keep max IDX: {PositionsDB.LowLevelDatabase.MaxSharedIndex}", logLayer: 3);
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

                yield return new WaitForEndOfFrame(); // per alleggerire il carico computazionale sul frame
            }
            StaticLogger.Info(sourceLog, $"Importing paths ... OK", logLayer: 1);
            PositionsDB.SetStatusImporting(this, false);
        }



        // ===== UPLOAD FEATURE ===== //

        public IEnumerator BSCOR_Upload()
        {

            string sourceLog = $"{classLogSource}:BSCOR_Upload";
            yield return null;

            StaticLogger.Info(sourceLog, $"Collecting informations to upload ...", logLayer: 1);
            List<data_hl2_waypoint> wpList = collectWaypointsUpload();
            List<data_hl2_path> ptList = collectPathsUpload();
            yield return new WaitForEndOfFrame();
            if(wpList.Count == 0)
            {
                StaticLogger.Info(sourceLog, $"nohing new to upload", logLayer: 1);
                yield break;
            }
            StaticLogger.Info(sourceLog, $"Collecting informations to upload ... OK ready", logLayer: 1);

            StaticLogger.Info(sourceLog, $"Calling API upload to server ...", logLayer: 1);
            yield return Client.ORCOR_UploadToServer(wpList, ptList);
            if(!Client.Success)
            {
                StaticLogger.Err(sourceLog, $"Collecting informations to upload ... ERROR: cannot upload, API error");
                yield break;
            }
            StaticLogger.Info(sourceLog, $"Calling API upload to server ... OK", logLayer: 1);

            StaticLogger.Info(sourceLog, $"Aligning positions database ...", logLayer: 1);
            if(!PositionsDB.SetStatusImporting(this, true))
                StaticLogger.Warn(sourceLog, $"SetStatusImporting (enabling import) returned false!", logLayer: 1);
            int wpCacheIndex = Client.ServerPositionIndex;
            StaticLogger.Info(sourceLog, $"starting with wpCacheIndex:{wpCacheIndex}", logLayer: 3);
            foreach (Tuple<int, int, data_hl2_waypoint> item in Client.UpdatedEntriesWps)
            {
                // questo ciclo azzecca solo renamings (non ci sono aggiunte, ovviamente, dato che i dati li stai fornendo tu)
                int oldIdx = item.Item1;
                int newIdx = item.Item2;
                StaticLogger.Info(sourceLog, $"oldIdx:{oldIdx} to newIdx:{newIdx}", logLayer: 3);

                if (PositionsDB.LowLevelDatabase.WpIndex.ContainsKey(newIdx))
                {
                    StaticLogger.Info(sourceLog, $"ID:{oldIdx} already registered", logLayer: 3);
                    if (PositionsDB.LowLevelDatabase.WpIndex[newIdx] != null)
                    {
                        // c'è stato un inserimento mentre si faceva l'upload
                        // l'inserimento è già stato catturato dalla CALLBACK 
                        // però l'elemento va spostato, altrimenti verrà sovrascritto
                        int idx = wpCacheIndex;
                        StaticLogger.Info(sourceLog, $"ID:{oldIdx} is not null! Need to move it: {newIdx} -> {idx}", logLayer: 3);
                        PositionsDB.LowLevelDatabase.WpIndex[newIdx].setPositionID(wpCacheIndex++);
                        PositionsDB.LowLevelDatabase.WpIndex[idx] = PositionsDB.LowLevelDatabase.WpIndex[newIdx];
                        PositionsDB.LowLevelDatabase.WpIndex[newIdx] = null;
                    }
                    PositionsDB.LowLevelDatabase.WpIndex[newIdx] = PositionsDB.LowLevelDatabase.WpIndex[oldIdx];
                    PositionsDB.LowLevelDatabase.WpIndex[oldIdx] = null;
                }
                else
                {
                    // altrimenti puoi inserire liberamente l'elemento al suo posto
                    StaticLogger.Info(sourceLog, $"ID:{oldIdx} seen for the first time", logLayer: 3);
                    PositionsDB.LowLevelDatabase.WpIndex.Add(newIdx, PositionsDB.LowLevelDatabase.WpIndex[oldIdx]);
                }
                PositionsDB.LowLevelDatabase.WpIndex[oldIdx] = null;
            }
            StaticLogger.Info(sourceLog, $"PositionsDB.LowLevelDatabase.MaxSharedIndex:{PositionsDB.LowLevelDatabase.MaxSharedIndex} Vs. Client.ServerPositionIndex:{Client.ServerPositionIndex} with wpCacheIndex:{wpCacheIndex}", logLayer: 2);
            if (PositionsDB.LowLevelDatabase.MaxSharedIndex < wpCacheIndex)
            {
                StaticLogger.Info(sourceLog, $"max IDX moved: {PositionsDB.LowLevelDatabase.MaxSharedIndex} -> wpCacheIndex:{wpCacheIndex}", logLayer: 3);
                // PositionsDB.LowLevelDatabase.MaxSharedIndex = Client.ServerPositionIndex; // WRONG WRONG WRONG SUPER WRONG!
                PositionsDB.LowLevelDatabase.MaxSharedIndex = wpCacheIndex;
            }
            else
                StaticLogger.Info(sourceLog, $"keep max IDX: {PositionsDB.LowLevelDatabase.MaxSharedIndex}", logLayer: 3);
            // il renaming degli archi avviene in maniera implicita poichè per ottenere la chiave di un path si usano dei get dai wps
            if (!PositionsDB.SetStatusImporting(this, false))
                StaticLogger.Warn(sourceLog, $"SetStatusImporting (disabling import) returned false!", logLayer: 1);
            StaticLogger.Info(sourceLog, $"Aligning positions database ... OK aligned", logLayer: 1);
        }



        // ===== UTILITIES ===== //

        private PositionDatabaseWaypoint GetWaypointFromJsonClass(data_hl2_waypoint jsonWp)
        {
            PositionDatabaseWaypoint wp = new PositionDatabaseWaypoint();
            wp.setPositionID(jsonWp.pos_id); // already shared in download
            wp.AreaIndex = jsonWp.area_id;
            DateTime.TryParse(jsonWp.wp_timestamp, out wp.Timestamp);
            wp.AreaCenter = StaticTransform.ToAppPoint(new Vector3(jsonWp.v[0], jsonWp.v[1], jsonWp.v[2]));
            wp.DBReference = PositionsDB;
            wp.AreaRadius = PositionsDB.BaseDistance;

            return wp;
        }

        private data_hl2_waypoint GetJsonClassFromWaypoint(PositionDatabaseWaypoint wp)
        {
            data_hl2_waypoint jwp = new data_hl2_waypoint();
            jwp.pos_id = wp.PositionID;
            jwp.area_id = 0;
            Vector3 glbVect = StaticTransform.ToRefPoint(wp.AreaCenter);
            jwp.v = new List<float> { glbVect.x, glbVect.y, glbVect.z };
            jwp.wp_timestamp = wp.Timestamp.ToString("yyyy/MM/dd HH:mm:ss");

            return jwp;
        }

        private data_hl2_path GetJsonClassFromPath(PositionDatabasePath pt)
        {
            data_hl2_path jpt = new data_hl2_path();
            jpt.wp1 = pt.wp1.PositionID;
            jpt.wp2 = pt.wp2.PositionID;
            jpt.pt_timestamp = pt.wp1.Timestamp.ToString("yyyy/MM/dd HH:mm:ss");

            return jpt;
        }

        private List<data_hl2_waypoint> collectWaypointsUpload()
        {
            List<data_hl2_waypoint> wpList = new List<data_hl2_waypoint>();
            HashSet<int> idWpsDuplicates = new HashSet<int>();
            foreach (PositionDatabaseWaypoint wp in newPositions)
            {
                if (!idWpsDuplicates.Contains(wp.PositionID))
                {
                    wpList.Add(GetJsonClassFromWaypoint(wp));
                    idWpsDuplicates.Add(wp.PositionID);
                }
            }

            newPositions.Clear();
            return wpList;
        }

        private List<data_hl2_path> collectPathsUpload()
        {
            List<data_hl2_path> ptList = new List<data_hl2_path>();
            HashSet<string> idDuplicates = new HashSet<string>();
            foreach (PositionDatabasePath pt in newPaths)
            {
                if (!idDuplicates.Contains(pt.Key))
                {
                    ptList.Add(GetJsonClassFromPath(pt));
                    idDuplicates.Add(pt.Key);
                }
            }

            newPaths.Clear();
            return ptList;
        }

    }
}
