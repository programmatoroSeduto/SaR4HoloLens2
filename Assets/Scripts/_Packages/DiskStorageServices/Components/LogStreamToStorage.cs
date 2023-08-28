using System.Collections.Generic;

using UnityEngine;

using Project.Scripts.Utils;
using Project.Scripts.Components;

namespace Packages.DiskStorageServices.Components
{
    public class LogStreamToStorage : ProjectMonoBehaviour
    {
        // ====== GUI ===== //
        [Header("Base Settings")]
        [Tooltip("Send the common infos to the handles")]
        public bool SendInfos = true;
        [Tooltip("Send also the warnings")]
        public bool SendWarnings = true;
        [Tooltip("Send also the error messages")]
        public bool SendErrors = true;
        [Tooltip("The component to call when a new message arrives")]
        public TxtWriter StorageChannel = null;
        [Tooltip("Suppressed warnings from static app settings")]
        public bool SuppressWarningsFromSettings = true;
        [Tooltip("Print stack traces also for warnings")]
        public bool PrintStackTraceWarnings = false;



        // ====== PRIVATE ===== //

        // if the stream is active or not
        private bool isEnabledStream = false;
        // pending messages
        private Queue<string> pendingMsgs = new Queue<string>();



        // ====== UNITY CALLBACKS ===== //

        void Start()
        {
            if (!SendInfos && !SendErrors && !SendWarnings)
            {
                Debug.LogWarning("ERROR: (Log Stream to Storage) Nothing to send!");
                return;
            }

            if (StorageChannel == null)
            {
                Debug.LogWarning("ERROR: (Log Stream to Storage) no storage channel defined!");
                return;
            }

            isEnabledStream = true;
            Ready();
        }

        private void Update()
        {
            while(pendingMsgs.Count > 0)
            {
                if (StorageChannel.EVENT_Write(pendingMsgs.Peek(), true))
                    pendingMsgs.Dequeue();
                else break;
            }
        }

        private void OnDestroy()
        {
            try
            {
                if (pendingMsgs.Count > 0)
                {
                    int retry = 10;
                    while (pendingMsgs.Count > 0 && retry > 0)
                    {
                        if (StorageChannel.EVENT_Write(pendingMsgs.Peek(), true))
                        {
                            pendingMsgs.Dequeue();
                        }
                        else
                        {
                            if(--retry <= 0)
                            {
                                retry = 10;
                                string lostMsg = pendingMsgs.Dequeue();
                                Debug.LogWarning($"LOST MESSAGE: {lostMsg}");
                            }
                        }
                    }
                }
            }
            catch(System.Exception)
            {
                Debug.LogWarning("WARNING: cannot empty le logInfo buffer; a exception occurred");
            }
        }

        // start reading logs when the object is enabled
        private void OnEnable() => Application.logMessageReceived += LogUnityMessage;

        // stop reading events from Unity when the object is disabled
        private void OnDisable() => Application.logMessageReceived -= LogUnityMessage;



        // ====== FEATURE COLLECT LOG AND WRITE ===== //

        public void LogUnityMessage(string message, string stackTrace, LogType type)
        {
            if (
                ((type == LogType.Error || type == LogType.Exception || type == LogType.Assert) && SendErrors)
                || (type == LogType.Warning && SendWarnings)
                || (type == LogType.Log && SendInfos)
             )
            {
                if (SuppressWarningsFromSettings && (type == LogType.Warning && SendWarnings) && ((ProjectAppSettings)StaticAppSettings.GetObject("AppSettings")).SuppressedLogs.Contains(message))
                    return;

                pendingMsgs.Enqueue(message);
                if (type == LogType.Error || (PrintStackTraceWarnings && type == LogType.Warning))
                    pendingMsgs.Enqueue("STACK TRACE ERROR:\n\n\t" + stackTrace + "\n\n");
            }
        }
    }
}
