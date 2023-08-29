using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Project.Scripts.Utils;
using Packages.SAR4HL2NetworkingServices.Utils;

namespace Packages.SAR4HL2NetworkingServices.Components
{
    public class SarHL2Client : ProjectMonoBehaviour
    {
        // ===== GUI ===== //

        [Header("Connection Settings")]
        [Tooltip("Web Address of the server")]
        public string ServerURL = "";
        [Tooltip("Connect on start?")]
        public bool ConnectOnStart = false;
        [Tooltip("Connection Timeout (-1 if not used)")]
        public int ConnectionTimeout = 10;

        [Header("Login Settings")]
        [Tooltip("User ID")]
        public string UserId = "";
        [Tooltip("User Approver ID")]
        public string UserApproverID = "";
        [Tooltip("Device ID")]
        public string DeviceId = "";
        [Tooltip("Reference position")]
        public string ReferencePositionId = "";
        [Tooltip("User Access Key")]
        public string UserAccessKey = "";



        // ===== PUBLIC ===== //

        public bool IsBusy
        {
            get
            {
                return runningConnection || isWorkingDownload || isWorkingUpload;
            }
        }

        public bool Success
        {
            get => success;
        }

        public bool Online
        {
            get => connected;
        }

        [HideInInspector]
        // visible to pos db -- waypoints (local, renamed, wpdata)
        public List<Tuple<int, int, data_hl2_waypoint>> UpdatedEntriesWps = new List<Tuple<int, int, data_hl2_waypoint>>();
        [HideInInspector]
        // visible to pos db -- paths
        public List<Tuple<Tuple<int, int>, Tuple<int, int>, data_hl2_path>> UpdatedEntriesPaths = new List<Tuple<Tuple<int, int>, Tuple<int, int>, data_hl2_path>>();



        // ===== PRIVATE ===== //

        // connection done?
        private bool connected = false;
        // connection coroutine
        private Coroutine COR_Connection = null;
        // if the connection coroutine is running or not
        private bool runningConnection = false;
        // user only for logging
        private string classLogSource = "SarHL2Client";
        // download in progress
        private bool isWorkingDownload = false;
        // upload in progress
        private bool isWorkingUpload = false;
        // success or not for the previously executed operation
        private bool success = false;
        // is this the first download?
        private bool isFirstDownload = true;

        // the positons archive (local ID -> tuple( local ID aligned with server, waypoint data ))
        // se le posizioni sono in questa tabella, significa che sono state certificate e allineate col server
        // non è possibile che il server mi ritorni una posizione che già esiste, quindi che non è nulla (non viene fatto renaming sulle posizioni note)
        // se li ho ricevuti dal server, allora sono sacrosanti e condivisi con tutti gli opeatori che fanno uso della stessa sessione
        // e la classe NON aggiunge qui dati che non siano stati validati e allineati col server
        // se capita un caso di 'renaming' significa che la base dati non è allineata! Manca l'upload prima del download
        private Dictionary<int, data_hl2_waypoint> archiveWaypoints = new Dictionary<int, data_hl2_waypoint>();
        // la seguente è da usare per i renamings
        // private Dictionary<int, Tuple<int, data_hl2_waypoint>> cachedRenamedWaypoints = new Dictionary<int, Tuple<int, data_hl2_waypoint>>();
        // the paths archive
        private Dictionary<Tuple<int, int>, data_hl2_path> archivePaths = new Dictionary<Tuple<int, int>, data_hl2_path>();




        // ===== UNITY CALLBACKS ===== //

        private void Start()
        {
            if (ConnectOnStart)
                InitConnection();
        }

        private void Update()
        {

        }

        private void OnDestroy()
        {
            Disconnect();
        }



        // ===== LOGIN PROCESS ===== //

        public void InitConnection()
        {
            string sourceLog = $"{classLogSource}:InitConnection";
            if (connected)
            {
                StaticLogger.Info(sourceLog, "Already connected");
                return;
            }

            COR_Connection = StartCoroutine(ORCOR_connect());
        }

        public IEnumerator ORCOR_connect()
        {
            string sourceLog = $"{classLogSource}:ORCOR_connect";
            yield return null;
            success = true;
            runningConnection = true;
            StaticLogger.Info(sourceLog, "connection process...", logLayer: 0);

            // service check status
            StaticLogger.Info(sourceLog, "Getting status service ...", logLayer: 2);
            SarAPI.ApiURL = ServerURL;
            yield return SarAPI.ApiCall_ServiceStatus(timeout: 10);
            if (!SarAPI.ServiceStatus)
            {
                StaticLogger.Err(sourceLog, "SERVICE UNAVAILABLE");
                runningConnection = false;
                connected = false;
                success = false;
                yield break; // server is not online
            }
            StaticLogger.Info(sourceLog, "Getting status service ... OK: service available", logLayer: 2);

            // user login
            StaticLogger.Info(sourceLog, "Trying to login user ...", logLayer: 2);
            yield return SarAPI.ApiCall_UserLogin(UserId, UserApproverID, UserAccessKey, ConnectionTimeout);
            if (!SarAPI.UserLoggedIn)
            {
                StaticLogger.Err(sourceLog, "LOGIN REQUEST REFUSED");
                runningConnection = false;
                connected = false;
                success = false;
                yield break; // user login fail
            }
            StaticLogger.Info(sourceLog, "Trying to login user ... OK: successfully logged in", logLayer: 2);
            connected = true;

            // device login
            StaticLogger.Info(sourceLog, "Trying to get device resource ...", logLayer: 2);
            yield return SarAPI.ApiCall_DeviceLogin(DeviceId, ConnectionTimeout);
            if (!SarAPI.DeviceLoggedIn)
            {
                StaticLogger.Err(sourceLog, "DEVICE REGISTRATION REQUEST REFUSED");
                runningConnection = false;
                success = false;
                _ = Disconnect();
                yield break; // device login fail
            }
            StaticLogger.Info(sourceLog, "Trying to get device resource ... OK: device successfully registered", logLayer: 2);

            StaticLogger.Info(sourceLog, "connection process... OK: successfully connected", logLayer: 0);
            connected = true;
            runningConnection = false;
            Ready();
        }



        // ===== LOGOUT PROCESS ===== //

        ~SarHL2Client()
        {
            // DisconnectOnDestroyClass();
        }

        public bool Disconnect()
        {
            string sourceLog = $"{classLogSource}:Disconnect";
            success = false;

            if (!connected)
            {
                StaticLogger.Info(sourceLog, "No disconnection required", logLayer: 1);
                success = true;
                return true;
            }

            StaticLogger.Info(sourceLog, "Disconnection process ... ");
            if (!SarAPI.ApiCall_UserLogout())
            {
                StaticLogger.Err(sourceLog, "Unable to release the connection!");
                return false;
            }

            connected = false;
            success = true;
            StaticLogger.Info(sourceLog, "Disconnection process ... OK: disconnected");
            return true;
        }

        private void DisconnectOnDestroyClass()
        {
            _ = SarAPI.ApiCall_UserLogout();
        }



        // ===== DOWNLOAD PROCESS ===== //

        public IEnumerator ORCOR_DownloadFromServer(Vector3? pos = null, float? radius = null)
        {
            string sourceLog = $"{classLogSource}:ORCOR_DownloadFromServer";
            yield return null;
            success = false;
            StaticLogger.Info(sourceLog, "download process...", logLayer: 0);

            if (IsBusy)
            {
                StaticLogger.Err(sourceLog, "Busy: cannot download");
                yield break;
            }
            else if (!connected)
            {
                StaticLogger.Err(sourceLog, "Not connected: cannot download");
                yield break;
            }

            isWorkingDownload = true;
            if (isFirstDownload && archiveWaypoints.Count == 0)
            {
                // the origin is automatically created by the server: it is fixed to 0 by convention
                data_hl2_waypoint wpOrigin = new data_hl2_waypoint();
                wpOrigin.pos_id = 0;
                wpOrigin.area_id = 0;
                wpOrigin.v = new List<float> { 0.0f, 0.0f, 0.0f };
                wpOrigin.wp_timestamp = DateTime.Now.ToString("%yyyy/%MM/%d %HH:%mm:%ss");
                archiveWaypoints.Add(0, wpOrigin);
            }

            StaticLogger.Info(sourceLog, "Performing download request ... ", logLayer: 2);
            if (isFirstDownload)
                yield return SarAPI.ApiCall_Hl2Download(ReferencePositionId, Vector3.zero, radius ?? 250.0f, calibrating:true, timeout:ConnectionTimeout);
            else
                yield return SarAPI.ApiCall_Hl2Download(ReferencePositionId, pos ?? Vector3.zero, radius ?? 250.0f, calibrating: false, timeout: ConnectionTimeout);
            // StaticLogger.Info(sourceLog, "Performing download request ... OK", logLayer: 2);
            if(!SarAPI.DownloadSuccess)
            {
                StaticLogger.Err(sourceLog, "UNABLE TO DOWNLOAD");
                isWorkingDownload = false;
                yield break;
            }
            else
                isFirstDownload = false;
            StaticLogger.Info(sourceLog, "download process... OK: success", logLayer: 0);

            StaticLogger.Info(sourceLog, "Collecting data ...", logLayer: 2);
            api_hl2_download_response data = SarAPI.Hl2DownloadResponse;

            UpdatedEntriesWps.Clear();
            foreach (data_hl2_waypoint wp in data.waypoints)
            {
                if(!archiveWaypoints.ContainsKey(wp.pos_id) || archiveWaypoints[wp.pos_id] == null)
                {
                    UpdatedEntriesWps.Add(new Tuple<int, int, data_hl2_waypoint>(wp.pos_id, wp.pos_id, wp));
                    if (!archiveWaypoints.ContainsKey(wp.pos_id))
                        archiveWaypoints.Add(wp.pos_id, wp);
                    else
                        archiveWaypoints[wp.pos_id] = wp;
                }
                else
                {
                    StaticLogger.Warn(sourceLog, $"Redundant position found from the server (skipping element)\n\t{JsonUtility.ToJson(wp)}\n\tHINT: is the server aligned with the client and viceversa?", logLayer: 1);
                    continue;
                }
            }

            UpdatedEntriesPaths.Clear();
            foreach (data_hl2_path path in data.paths)
            {
                Tuple<int, int> k12 = new Tuple<int, int>(path.wp1, path.wp2);
                bool k12Exists = archivePaths.ContainsKey(k12);
                bool k12HasValue = k12Exists && archivePaths[k12] != null;
                Tuple<int, int> k21 = new Tuple<int, int>(path.wp2, path.wp1);
                bool k21Exists = archivePaths.ContainsKey(k12);
                bool k21HasValue = k21Exists && archivePaths[k21] != null;

                if(k12HasValue || k12HasValue)
                {
                    StaticLogger.Warn(sourceLog, $"Redundant path found from the server (skipping element)\n\t{JsonUtility.ToJson(path)}\n\tHINT: is the server aligned with the client and viceversa?", logLayer: 1);
                    continue;
                }

                // keep the double reference: doing so, recalling the result becomes simpler
                if(!k12Exists)
                    archivePaths.Add(k12, path);
                else if (!k12HasValue)
                    archivePaths[k12] = path;
                if (!k21Exists)
                    archivePaths.Add(k21, path);
                else if (!k21HasValue)
                    archivePaths[k21] = path;

                UpdatedEntriesPaths.Add(new Tuple<Tuple<int, int>, Tuple<int, int>, data_hl2_path>(k12, k12, path));
            }
            StaticLogger.Info(sourceLog, "Collecting data ... OK: ready", logLayer: 2);

            success = true;
            isWorkingDownload = false;
        }





        // ===== UPLOAD PROCESS ===== //

        public int ServerPositionIndex => SarAPI.ServerPositionIndex;

        /*
         * ANNOTAZIONI
         * - ogni volta che il servizio deve fare upload, fa upload di tutti i WPs presenti al di sopra del
         *      precedente max index. Lato DB posizioni funziona, perchè se il server ti dice che il tuo 
         *      max idx è quello, significa che ogni posizione che abbia un ID <= di quello che ti ha passato
         *      è sicuramente già stata considerata. 
         * - il max si chiama ... ServerPositionIndex (sipario)
         * - il check di ridondanza non serve alla fin fine, perchè HL2 ttramite posDB tenta di inviare sempre
         *      le posizioni nuove usando il max idx, mentre lato server se qualche ridondanza scappa viene
         *      comunque gestita dalle query che si occupano dell'upload. 
         * */
        public IEnumerator ORCOR_UploadToServer(List<data_hl2_waypoint> wpList, List<data_hl2_path> ptList)
        {
            string sourceLog = $"{classLogSource}:ORCOR_UploadToServer";
            yield return null;
            success = false;
            StaticLogger.Info(sourceLog, "upload process...", logLayer: 0);

            if(wpList == null)
            {
                StaticLogger.Err(sourceLog, "parameter 'wpList' is null");
                yield break;
            }
            else if (ptList == null)
            {
                StaticLogger.Err(sourceLog, "parameter 'ptList' is null");
                yield break;
            }

            if (IsBusy)
            {
                StaticLogger.Err(sourceLog, "Busy: cannot upload");
                yield break;
            }
            else if (!connected)
            {
                StaticLogger.Err(sourceLog, "Not connected: cannot upload");
                yield break;
            }
            else if (isFirstDownload)
            {
                StaticLogger.Info(sourceLog, "HINT: (Calling Upload before Download is not allowed) Calling 'upload' before 'download' could result in a disalignment between client IDs and server IDs, and the last ones are shared across different devices", logLayer: 1);
                StaticLogger.Err(sourceLog, "Calling Upload before Download is not allowed");
                yield break;
            }
            else if (wpList.Count == 0 && ptList.Count == 0)
            {
                StaticLogger.Warn(sourceLog, "Calling Upload with no items", logLayer: 2);
                success = true;
                yield break;
            }
            // ... and what about calling function with no waypoints only???
            // ... and what about calling function with no paths???

            isWorkingUpload = true;

            StaticLogger.Info(sourceLog, "Performing upload request ... ", logLayer: 2);
            yield return SarAPI.ApiCall_Hl2Upload(wpList, ptList, timeout: ConnectionTimeout);
            if(!SarAPI.UploadSuccess)
            {
                StaticLogger.Err(sourceLog, "UNABLE TO UPLOAD");
                isWorkingUpload = false;
                yield break;
            }
            StaticLogger.Info(sourceLog, "Performing upload request ... OK: upload completed", logLayer: 2);

            StaticLogger.Info(sourceLog, "Performing alignment with the server ... ", logLayer: 2);
            api_hl2_upload_response data = SarAPI.hl2UploadResponse;
            if (data.wp_alignment.Count == 0)
            {
                StaticLogger.Info(sourceLog, "Alignment set is empty from server", logLayer: 2);
            }
            else
            {
                /*
                 * l'allineamento fa riferimento ai soli punti che ho già inviato, e che ancora non fanno
                 * parte del mio set di punti perchè non so come collocarli nel mio insieme. Io propongo
                 * un ID che vado a negoziare col server tramite richiesta API. Il server mi ritorna gli 
                 * ID finali della richiesta. 
                 * 
                 * l'allineamento può dare diversi risultati:
                 * - faceva già parte delle mie keys? -> allora era un punto già noto, allneato a BE
                 * - non fa parte delle mie keys? 
                 * */
                UpdatedEntriesWps.Clear();
                foreach (data_hl2_waypoint wp in wpList)
                {
                    int finalPosId = (SarAPI.AlignmentLookup.ContainsKey(wp.pos_id) ? SarAPI.AlignmentLookup[wp.pos_id] : wp.pos_id);
                    bool isKnownPoint = archiveWaypoints.ContainsKey(finalPosId);
                    bool isRenamedPoint = (wp.pos_id != finalPosId);

                    // isKnownPoint and isRenamedPoint : 
                    //     segna come modificato e vai avanti
                    // isKnownPoint and not isRenamedPoint : 
                    //     ridondante, skippa
                    // not isKnownPoint and isRenamedPoint : 
                    //     è completamente nuovo, ma ha un ID diverso da quello proposto
                    //     segna modifica, aggiungi il punto e passa oltre
                    // not isKnownPoint and not isRenamedPoint :
                    //     è completamente nuovo, e l'ID è quello che gli ho dato io
                    //     aggiungi il punto e passa oltre

                    if(isRenamedPoint)
                        UpdatedEntriesWps.Add(new Tuple<int, int, data_hl2_waypoint>(wp.pos_id, finalPosId, wp));
                    if (!isKnownPoint)
                        archiveWaypoints.Add(finalPosId, wp);
                }
                UpdatedEntriesPaths.Clear();

                /* (molto probabilmente non serve per l'allineamento del Positions DB)
                foreach(data_hl2_path pt in ptList)
                {
                    int wp1 = (SarAPI.AlignmentLookup.ContainsKey(pt.wp1) ? SarAPI.AlignmentLookup[pt.wp1] : pt.wp1);
                    int wp2 = (SarAPI.AlignmentLookup.ContainsKey(pt.wp2) ? SarAPI.AlignmentLookup[pt.wp2] : pt.wp2);
                    Tuple<int, int> finalK12 = new Tuple<int, int>(wp1, wp2);
                    Tuple<int, int> finalK21 = new Tuple<int, int>(wp2, wp1);
                    bool isKnownPath12 = archivePaths.ContainsKey(finalK12);
                    bool isKnownPath21 = archivePaths.ContainsKey(finalK21);
                    bool isRenamedPath = (pt.wp1 != wp1) || (pt.wp2 != wp2);

                    if (isRenamedPath)
                        UpdatedEntriesPaths.Add(new Tuple<Tuple<int, int>, Tuple<int, int>, data_hl2_path>(
                            new Tuple<int, int>(pt.wp1, pt.wp2), finalK12, pt));
                    if (!isKnownPath12)
                        archivePaths.Add(finalK12, pt);
                    if (isKnownPath21)
                        archivePaths.Remove(finalK21);
                }
                */
            }
            StaticLogger.Info(sourceLog, "Performing alignment with the server ... OK", logLayer: 2);

            success = true;
            isWorkingUpload = false;
            StaticLogger.Info(sourceLog, "upload process... OK: success", logLayer: 0);
        }
    }
}
