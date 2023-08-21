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
        [Tooltip("IP Address of the server")]
        public string ServerIp = "127.0.0.1";
        [Tooltip("Server Port Number")]
        public int ServerPortNo = 5000;
        [Tooltip("Connect on start?")]
        public bool ConnectOnStart = false;
        [Tooltip("Connection Timeout (-1 if not used)")]
        public int ConnectionTimeout = 10;

        [Header("Login Settings")]
        [Tooltip("User ID")]
        public string UserId = "SARHL2_ID0000000000_USER";
        [Tooltip("User Approver ID")]
        public string UserApproverID = "SARHL2_ID0000000000_USER";
        [Tooltip("Device ID")]
        public string DeviceId = "SARHL2_ID0000000000_DEVC";
        [Tooltip("User Access Key")]
        public string UserAccessKey = "";



        // ===== PRIVATE ===== //

        // connection done?
        private bool connected = false;
        // connection coroutine
        private Coroutine COR_Connecion = null;
        // if the connection coroutine is running or not
        private bool runningConnection = false;
        // user only for logging
        private string classLogSource = "SarHL2Client";



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
            string sourceLog = $"{classLogSource}:OnDestroy";
            if (connected)
            {
                StaticLogger.Info(sourceLog, "Found a connection; trying to disconnect...", logLayer: 1);
                Disconnect();
            }
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

            COR_Connecion = StartCoroutine(ORCOR_connect());
        }

        public IEnumerator ORCOR_connect()
        {
            string sourceLog = $"{classLogSource}:ORCOR_connect";
            yield return null;
            runningConnection = true;
            StaticLogger.Info(sourceLog, "connection process...", logLayer: 0);

            // service check statuc
            StaticLogger.Info(sourceLog, "Getting status service ...", logLayer: 2);
            yield return SarAPI.ApiCall_ServiceStatus(timeout: 10);
            if (!SarAPI.ServiceStatus)
            {
                StaticLogger.Err(sourceLog, "SERVICE UNAVAILABLE");
                runningConnection = false;
                connected = false;
                yield break; // server is not online
            }
            StaticLogger.Info(sourceLog, "Getting status service ... OK: service available", logLayer: 2);

            // user login
            StaticLogger.Info(sourceLog, "Trying to login user ...", logLayer: 2);
            yield return SarAPI.ApiCall_UserLogin(UserId, UserApproverID, UserAccessKey, ConnectionTimeout);
            if(!SarAPI.UserLoggedIn)
            {
                StaticLogger.Err(sourceLog, "LOGIN REQUEST REFUSED");
                runningConnection = false;
                connected = false;
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
                _ = Disconnect();
                yield break; // device login fail
            }
            StaticLogger.Info(sourceLog, "Trying to get device resource ... OK: device successfully registered", logLayer: 2);

            StaticLogger.Info(sourceLog, "connection process... OK: successfully connected", logLayer: 0);
            connected = true;
            runningConnection = false;
        }



        // ===== LOGOUT PROCESS ===== //

        public bool Disconnect()
        {
            string sourceLog = $"{classLogSource}:Disconnect";
            
            if(!connected)
            {
                StaticLogger.Info(sourceLog, "No disconnection required", logLayer: 1);
            }
            
            StaticLogger.Info(sourceLog, "Disconnection process ... ");
            if (!SarAPI.ApiCall_UserLogout())
            {
                StaticLogger.Err(sourceLog, "Unable to release the connection!");
                return false;
            }

            connected = false;
            StaticLogger.Info(sourceLog, "Disconnection process ... OK: disconnected");
            return true;
        }
    }
}
