using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;

using Project.Scripts.Utils;
using Project.Scripts.Components;
using Packages.SAR4HL2NetworkingSettings.Utils;

namespace Packages.SAR4HL2NetworkingSettings.Components
{
    public class SarHL2Client : MonoBehaviour
    {
        // ===== GUI ===== //

        [Header("Connection Settings")]
        [HideInInspector]
        [Tooltip("Web Address of the server")]
        public string ServerURL = "http://127.0.0.1:80/sar/";
        [HideInInspector]
        [Tooltip("Connect on start?")]
        public bool ConnectOnStart = false;
        [HideInInspector]
        [Tooltip("Connection Timeout (-1 if not used)")]
        public int ConnectionTimeout = 10;

        [Header("Login Settings")]
        [HideInInspector]
        [Tooltip("User ID")]
        public string UserId = "SARHL2_ID0000000000_USER";
        [HideInInspector]
        [Tooltip("User Approver ID")]
        public string UserApproverID = "SARHL2_ID0000000000_USER";
        [HideInInspector]
        [Tooltip("Device ID")]
        public string DeviceId = "SARHL2_ID0000000000_DEVC";
        [HideInInspector]
        [Tooltip("Reference position")]
        public string ReferencePositionId = "SARHL2_ID0000000000_REFP";
        [HideInInspector]
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

        public bool NewDataFromServer
        {
            get
            {
                if(!isDirty)
                {
                    // TODO : also check the data archive (condition 1. missing)

                    isDirty = (renamedWaypoints.Count > 0) 
                        || (newWaypoints.Count > 0) 
                        || (renamedPaths.Count > 0) 
                        || (newPaths.Count > 0);
                }
                return isDirty;
            }
        }



        // ===== PRIVATE ===== //

        // connection done?
        private bool connected = false;
        // connection coroutine
        private Coroutine COR_Connecion = null;
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

        // wether the archive is dirty or not
        /*
         * NOT DIRTY WHEN:
         * 1. all the IDS in archiveWaypoints coincide
         * 2. renamedWaypoints is a empty set (there are no waypoint violating the 1.)
         * 3. newWaypoints is a empty set (all the new waypoints have been discovered by the positions database)
         * 4. same conditions (2. and 3.) hold for paths as well
         * */
        private bool isDirty = false;
        // the positons archive (local ID -> tuple( local ID aligned with server, waypoint data ))
        private Dictionary<int, Tuple<int, data_hl2_waypoint>> archiveWaypoints = new Dictionary<int, Tuple<int, data_hl2_waypoint>>();
        // ...
        private HashSet<int> renamedWaypoints = new HashSet<int>();
        // ...
        private HashSet<int> newWaypoints = new HashSet<int>();
        // the paths archive
        private Dictionary<Tuple<int, int>, data_hl2_path> archivePaths = new Dictionary<Tuple<int, int>, data_hl2_path>();
        // ...
        private HashSet<Tuple<int, int>> renamedPaths = new HashSet<Tuple<int, int>>();
        // ...
        private HashSet<Tuple<int, int>> newPaths = new HashSet<Tuple<int, int>>();




        // ===== UNITY CALLBACKS ===== //

        private void Start()
        {
            StaticLogger.CurrentLogLayer = 9999;
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

            SarAPI.ApiURL = ServerURL;
            COR_Connecion = StartCoroutine(ORCOR_connect());
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
        }



        // ===== LOGOUT PROCESS ===== //

        ~SarHL2Client()
        {
            DisconnectOnDestroyClass();
        }

        public bool Disconnect()
        {
            string sourceLog = $"{classLogSource}:Disconnect";
            success = true;

            if (!connected)
            {
                StaticLogger.Info(sourceLog, "No disconnection required", logLayer: 1);
            }

            StaticLogger.Info(sourceLog, "Disconnection process ... ");
            if (!SarAPI.ApiCall_UserLogout())
            {
                StaticLogger.Err(sourceLog, "Unable to release the connection!");
                success = false;
                return false;
            }

            connected = false;
            StaticLogger.Info(sourceLog, "Disconnection process ... OK: disconnected");
            return true;
        }

        private void DisconnectOnDestroyClass()
        {
            _ = SarAPI.ApiCall_UserLogout();
        }



        // ===== DOWNLOAD PROCESS ===== //

        public IEnumerator ORCOR_DownloadFromServer(Vector3? pos = null, float? radius = null, bool calibrating = false)
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
            else if (isDirty)
            {
                StaticLogger.Info(sourceLog, "(isDirty = True) the Issue is probably due to the fact that the position database still have not read the positions after download or upload.", logLayer: 1);
                StaticLogger.Err(sourceLog, "Data archive is still dirty! Cannot download");
                yield break;
            }

            isWorkingDownload = true;
            if (calibrating && archiveWaypoints.Count == 0)
            {
                // the origin is automatically created by the server: it is fixed to 0 by convention
                data_hl2_waypoint wpOrigin = new data_hl2_waypoint();
                wpOrigin.pos_id = 0;
                wpOrigin.area_id = 0;
                wpOrigin.v = new List<float> { 0.0f, 0.0f, 0.0f };
                wpOrigin.wp_timestamp = DateTime.Now.ToString("%yyyy/%MM/%d %HH:%mm:%ss");
                archiveWaypoints.Add(0, new Tuple<int, data_hl2_waypoint>(0, wpOrigin));
            }

            StaticLogger.Info(sourceLog, "Performing download request ... ", logLayer: 2);
            if (calibrating)
                yield return SarAPI.ApiCall_Hl2Download(ReferencePositionId, Vector3.zero, 250.0f, calibrating:true, timeout:ConnectionTimeout);
            else
                yield return SarAPI.ApiCall_Hl2Download(ReferencePositionId, pos ?? Vector3.zero, radius ?? 250.0f, calibrating: true, timeout: ConnectionTimeout);
            StaticLogger.Info(sourceLog, "Performing download request ... OK", logLayer: 2);
            if(!SarAPI.DownloadSuccess)
            {
                StaticLogger.Err(sourceLog, "UNABLE TO DOWNLOAD");
                isWorkingDownload = false;
                yield break;
            }
            StaticLogger.Info(sourceLog, "download process... OK: success", logLayer: 0);

            StaticLogger.Info(sourceLog, "Collecting data ...", logLayer: 2);
            api_hl2_download_response data = SarAPI.Hl2DownloadResponse;
            /*
             * POSITION IS NEW (local ID is not in keys of 'archiveWaypoints')
             * -> set 'newWaypoints' ADD local ID
             * -> 'archiveWaypoints' ADD simple wrt the local ID
             * -> dirty = true
             * 
             * POSITION IS ALREADY THERE (aligned ID is already present into the keys of 'archiveWaypoints')
             * it's not possible! (put a error if it happens, but it is not possible with the algorihm running on the server side)
             * */
            foreach(data_hl2_waypoint wp in data.waypoints)
            {
                if(!archiveWaypoints.ContainsKey(wp.pos_id) || archiveWaypoints[wp.pos_id] == null)
                {
                    newWaypoints.Add(wp.pos_id);
                    archiveWaypoints.Add(wp.pos_id, new Tuple<int, data_hl2_waypoint>(wp.pos_id, wp));
                    isDirty = true;
                }
                else
                {
                    StaticLogger.Warn(sourceLog, $"Redundant position found from the server\n\t{JsonUtility.ToJson(wp)}", logLayer: 1);
                    continue;
                }
            }

            /*
             * PATH IS NEW (wp1->wp2 is in the set OR wp2->wp1 is in the set)
             * -> 'archivePaths' ADD simple (key, value)
             * -> set 'newPaths' ADD tuple (wp1->wp2)
             * -> dirty = true
             * */
            foreach(data_hl2_path path in data.paths)
            {
                Tuple<int, int> k12 = new Tuple<int, int>(path.wp1, path.wp2);
                Tuple<int, int> k21 = new Tuple<int, int>(path.wp2, path.wp1);

                // TODO: from here
            }

            StaticLogger.Info(sourceLog, "Collecting data ... OK: ready", logLayer: 2);

            success = true;
            isWorkingDownload = false;
        }



        // ===== UPLOAD PROCESS ===== //

        public IEnumerator ORCOR_UploadFromServer()
        {
            string sourceLog = $"{classLogSource}:ORCOR_UploadFromServer";
            yield return null;
            success = true;
            StaticLogger.Info(sourceLog, "upload process...", logLayer: 0);

            if (IsBusy)
            {
                StaticLogger.Err(sourceLog, "Busy: cannot upload");
                success = false;
                yield break;
            }
            else if (!connected)
            {
                StaticLogger.Err(sourceLog, "Not connected: cannot upload");
                success = false;
                yield break;
            }
            else if (isDirty)
            {
                StaticLogger.Info(sourceLog, "(isDirty = True) the Issue is probably due to the fact that the position database still have not read the positions after download or upload.", logLayer: 1);
                StaticLogger.Err(sourceLog, "Data archive is still dirty! Cannot upload");
                success = false;
                yield break;
            }

            StaticLogger.Info(sourceLog, "upload process... OK: success", logLayer: 0);
        }
    }
}
