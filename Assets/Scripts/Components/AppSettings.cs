using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace SaR4Hololens2.Scripts.Components
{
    public class AppSettings : MonoBehaviour
    {
        // User Settings
        public float UserHeight = 1.85f;

        // server connection settings
        public string ServerIpAddress = "127.0.0.1";
        public string ServerPortNo = "5000";

        private void Start()
        {
            StaticAppSettings.SetOpt("UserHeight", UserHeight.ToString());

            StaticAppSettings.SetOpt("ServerIpAddress", ServerIpAddress);
            StaticAppSettings.SetOpt("ServerPortNo", ServerPortNo);
        }

    }
}
