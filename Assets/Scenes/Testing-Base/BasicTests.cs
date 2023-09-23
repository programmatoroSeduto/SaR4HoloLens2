using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Project.Scripts.Components;
using Project.Scripts.Utils;
using Packages.DiskStorageServices.Components;

namespace Project.Scenes.TestingBase.Components
{
    public class BasicTests : ProjectMonoBehaviour
    {
        // ===== GUI ===== //

        [Header("Test: Unity Frame Rate")]

        [Tooltip("Enable test Unity FrameRate")]
        public bool EnableUnityTestFrame = false;
        [Tooltip("New record each tot frames")]
        public int FrameCount = 60;
        [Tooltip("print framerate to screen")]
        public bool PrintFrameRate = false;
        [Tooltip("Enable or disable CSV export")]
        public bool EnableFrameRateCSVExport = false;
        [Tooltip("CSV export for frame rate")]
        public CsvWriter CsvExportFrameRate = null;



        // ===== PRIVATE ===== //

        // last frame rate measurement
        private DateTime UnityFrameRate_LastFrameMeasurement = DateTime.Now;
        // framerate count
        private int UnityFrameRate_Count = 0;
        // the frame rate
        private double UnityFrameRate_FrameRate = 0.0;



        // ===== UNITY CALLBACKS ===== //

        // Start is called before the first frame update
        void Start()
        {
            if (EnableUnityTestFrame)
            {
                INIT_UnityFrameRate();
            }
        }

        // Update is called once per frame
        void Update()
        {
            if(EnableUnityTestFrame)
            {
                UPDATE_UnityFrameRate();
            }
        }



        // ===== TEST FRAME RATE ===== //

        public void EVENT_UnityFrameRate(bool opt)
        {
            EnableUnityTestFrame = opt;
        }

        private void INIT_UnityFrameRate()
        {
            if(EnableFrameRateCSVExport && CsvExportFrameRate != null)
            {
                // ... ?
            }
        }

        private void UPDATE_UnityFrameRate()
        {
            if (!EnableUnityTestFrame) return;

            if (UnityFrameRate_Count < FrameCount)
                ++UnityFrameRate_Count;
            else 
            {
                // get frme rate
                UnityFrameRate_FrameRate = ((double)FrameCount) / (DateTime.Now - UnityFrameRate_LastFrameMeasurement).TotalSeconds;

                // export frame rate
                if (PrintFrameRate)
                    StaticLogger.Info(this, $"FrameRate: {UnityFrameRate_FrameRate.ToString(".0000")}");
                if (EnableFrameRateCSVExport)
                    CsvExportFrameRate.EVENT_WriteCsv(new List<string> { UnityFrameRate_FrameRate.ToString(".0000") });

                // reset frame rate count
                UnityFrameRate_LastFrameMeasurement = DateTime.Now;
                UnityFrameRate_Count = 0;
            }
        }
    }
}
