using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Project.Scripts.Components;
using Project.Scripts.Utils;

namespace Project.Scenes.TestingBenchmark.Scripts.Utils
{
    [Serializable]
    public class PlayerWaypoint
    {
        public string WpName = "wp";
        public List<string> NearWpNames = new List<string>();
        public Vector3 TargetVector = Vector3.zero;
        public GameObject TargetGo = null;

        public Vector3 Target
        {
            get { return TargetGo != null ? TargetGo.transform.position : TargetVector; }
        }
    }
}

