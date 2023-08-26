using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;

namespace Packages.SAR4HL2NetworkingServices.ModuleTesting
{
    public class TestingConnectionUnity : MonoBehaviour
    {
        public string IpAddress = "127.0.0.1";
        public int portno = 5000;
        public int CycleTimeout = 1;

        // Start is called before the first frame update
        void Start()
        {
            // StartCoroutine(BSCOR_TestConnection());
            Debug.Log(UnityWebRequest.kHttpVerbGET);
        }

        public IEnumerator BSCOR_TestConnection()
        {
            yield return null;

            Debug.Log("Address:" + $"http://{IpAddress}:{portno}");
            UnityWebRequest www = UnityWebRequest.Get($"http://{IpAddress}:{portno}"); // URL mandatory
            // www.url = $"http://{IpAddress}:{portno}"; // { get; set; }
            www.method = "GET"; // { get; set; }
            www.timeout = 60; // seconds
            
            yield return www.SendWebRequest();
            // check the progress with 
            // www.uploadProgress // percent, 0.0f to 1.0f

            // check request error (simple)
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                Debug.Log("request succeeded");
            }

            // check request error (complex)
            switch (www.result)
            {
                case UnityWebRequest.Result.ConnectionError:
                case UnityWebRequest.Result.DataProcessingError:
                    Debug.LogError("Error: " + www.error);
                    break;
                case UnityWebRequest.Result.ProtocolError:
                    Debug.LogError(" HTTP Error: " + www.error);
                    break;
                case UnityWebRequest.Result.Success:
                    Debug.Log("OK");
                    break;
            }

            Debug.Log($"SERVER RETURNED HTTP CODE: {www.responseCode}");
            // Debug.Log($"With result: {www.result}");
            // www.result.Success
            // www.result.InProgress
            

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}

