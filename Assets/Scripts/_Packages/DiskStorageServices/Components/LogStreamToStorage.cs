using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Packages.DiskStorageServices.Components
{
    public class LogStreamToStorage : MonoBehaviour
    {
        // ====== GUI ===== //
        [Header("Base Settings")]
        [Tooltip("Send the common infos to the handles")]
        public bool SendInfos = false;
        [Tooltip("Send also the warnings")]
        public bool SendWarnings = false;
        [Tooltip("Send also the error messages")]
        public bool SendErrors = true;
        [Tooltip("The component to call when a new message arrives")]
        public TxtWriter StorageChannel = null;



        // ====== PRIVATE ===== //

        // if the stream is active or not
        private bool isEnabledStream = false;



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
        }

        // start reading logs when the object is enabled
        void OnEnable() => Application.logMessageReceived += LogUnityMessage;

        // stop reading events from Unity when the object is disabled
        void OnDisable() => Application.logMessageReceived -= LogUnityMessage;



        // ====== FEATURE COLLECT LOG AND WRITE ===== //

        public void LogUnityMessage(string message, string stackTrace, LogType type)
        {
            if (!isEnabledStream) return;

            if (
                ((type == LogType.Error || type == LogType.Exception || type == LogType.Assert) && SendErrors)
                || (type == LogType.Warning && SendWarnings)
                || (type == LogType.Log && SendInfos)
             )
            {
                StorageChannel.EVENT_Write(message + "\n");
            }
        }
    }
}
