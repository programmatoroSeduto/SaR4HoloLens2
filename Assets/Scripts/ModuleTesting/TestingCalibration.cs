using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Project.Scripts.Components;
using Project.Scripts.Utils;

namespace Project.Scripts.ModuleTesting
{
    public class TestingCalibration : ProjectMonoBehaviour
    {
        public int count = 1500;
        public int firstCount = 1500;

        // Start is called before the first frame update
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {
            if (!StaticTransform.CalibrationDone) return;
            if (--count > 0) return;
            else count = firstCount;

            Vector3 app = StaticTransform.AppPosition;
            Vector3 tr = StaticTransform.TransformPosition;
            StaticLogger.Info(this, $"\n\tapp: ({app.x}, {app.y}, {app.z})\n\ttr:  ({tr.x}, {tr.y}, {tr.z})");
        }
    }
}
