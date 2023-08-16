using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using UnityEngine.Networking;

namespace Packages.SAR4HL2NetworkingSettings.ModuleTesting
{
    public class TestingConnectionUnity : MonoBehaviour
    {
        public string IpAddress = "127.0.0.1";
        public int portno = 5000;
        public int CycleTimeout = 1;

        // Start is called before the first frame update
        void Start()
        {
            StartCoroutine(BSCOR_TestConnection());
        }

        public IEnumerator BSCOR_TestConnection()
        {
            yield return null;

            Debug.Log("Address:" + $"http://{IpAddress}:{portno}");
            UnityWebRequest www = UnityWebRequest.Get($"http://{IpAddress}:{portno}");
            yield return www.SendWebRequest();

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}

