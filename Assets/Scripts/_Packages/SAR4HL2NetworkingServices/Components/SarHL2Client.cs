using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Networking;

using Project.Scripts.Components;
using Project.Scripts.Utils;
using Packages.SAR4HL2NetworkingSettings.Utils;

namespace Packages.SAR4HL2NetworkingSettings.Components
{
    public class SarHL2Client : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            StaticLogger.CurrentLogLayer = 99999;
            StartCoroutine(SarAPI.ServiceStatus());
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
