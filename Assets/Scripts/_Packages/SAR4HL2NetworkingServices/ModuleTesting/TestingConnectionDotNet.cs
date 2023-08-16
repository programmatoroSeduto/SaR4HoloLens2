using System;
using System.Net.Http;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Packages.SAR4HL2NetworkingSettings.ModuleTesting
{
    public class TestingConnectionDotNet : MonoBehaviour
    {
        private static HttpClient cl = new HttpClient();

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

            var req = cl.GetAsync($"http://{IpAddress}:{portno}");
            while(!req.IsCompleted)
            {
                Debug.Log("Waiting ...");
                yield return new WaitForSecondsRealtime(CycleTimeout);
            }
            Debug.Log("end of cycle!");
            req.GetAwaiter().GetResult();

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
