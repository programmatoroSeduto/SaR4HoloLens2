using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;


using Packages.VisualItems.Types;

namespace Packages.UnityBasics.Components
{
    public class LogDispatcher : MonoBehaviour
    {
        [Header("General Settings")]
        [Tooltip("Send the common infos to the handles")]
        public bool SendInfos = false;

        [Tooltip("Send also the warnings")]
        public bool SendWarnings = false;

        [Tooltip("Send also the error messages")]
        public bool SendErrors = true;

        [Header("Handles Settings")]
        [Tooltip("Handle functions to call when one message arrives")]
        //public List<UnityEvent<string>> Callbacks = new List<UnityEvent<string>>();

        public LogWindowBaseType logger = null;

        // Start is called before the first frame update
        void Start()
        {
            if (!SendInfos && !SendErrors && !SendWarnings)
            {
                Debug.LogWarning("(LogDispatcher) Nothing to send!");
                return;
            }

            /*
            if (Callbacks.Count == 0)
            {
                Debug.LogWarning("(LogDispatcher) No callback has been set!");
                return;
            }
            */
        }

        // start reading logs when the object is enabled
        void OnEnable() => Application.logMessageReceived += LogUnityMessage;

        // stop reading events from Unity when the object is disabled
        void OnDisable() => Application.logMessageReceived -= LogUnityMessage;

        // message from Unity log to the other handles
        public void LogUnityMessage(string message, string stackTrace, LogType type)
        {
            if (
                ((type == LogType.Error || type == LogType.Exception || type == LogType.Assert) && SendErrors)
                || (type == LogType.Warning && SendWarnings)
                || (type == LogType.Log && SendInfos)
             )
            {
                /*
                foreach (UnityEvent<string> cbk in Callbacks)
                    cbk.Invoke(message);
                */
                if (logger != null)
                    logger.EVENT_LogContent(message);
            }
        }

        // Update is called once per frame
        void Update()
        {
            // ...
        }
    }
}