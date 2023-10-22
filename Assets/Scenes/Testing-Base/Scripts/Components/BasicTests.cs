using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Project.Scripts.Components;
using Project.Scripts.Utils;
using Packages.DiskStorageServices.Components;
using Packages.PositionDatabase.Components;
using Packages.PositionDatabase.Utils;

namespace Project.Scenes.TestingBase.Components
{
    public class BasicTests : ProjectMonoBehaviour
    {
        // ===== GUI ===== //

        [Header("General References")]
        
        [Tooltip("PosDB Reference")]
        public PositionsDatabase PosDbReference = null;


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


        [Header("Test: Chasing Speed")]

        [Tooltip("Enable chasing speed metric")]
        public bool EnableChasingSpeedMetric = false;
        [Tooltip("How much frames to wait after the next metric update")]
        public int MetricFrameResolution = 60;
        [Tooltip("Either to compute the sorting euristic or not")]
        public bool EvaluateDbSort = false;
        [Tooltip("Enable or disable CSV export for chasing speed metric")]
        public bool EnableCsvExportChasingSpeedMetric = false;
        [Tooltip("Print lines in output")]
        public bool ChasingSpeedMetricPrintCsvLines = false;
        [Tooltip("CSV export for chasing speed")]
        public CsvWriter CsvExportChasingSpeedMetric = null;


        [Header("Test: Interal Odometry Check")]

        [Tooltip("Enable internal odometry check")]
        public bool EnableOdometryCheck = false;
        [Tooltip("How much frames to wait after the next metric update")]
        public int OdometryFrameResolution = 60;
        [Tooltip("Print lines in output")]
        public bool EnableOdometryCsvExport = false;
        [Tooltip("CSV export for chasing speed")]
        public CsvWriter CsvExportOdometry = null;



        // ===== PRIVATE ===== //

        // GENERIC
        // ...

        // FRAME RATE METRIC
        // last frame rate measurement
        private DateTime UnityFrameRate_LastFrameMeasurement = DateTime.Now;
        // framerate count
        private int UnityFrameRate_Count = 0;
        // the frame rate
        private double UnityFrameRate_FrameRate = 0.0;

        // CHASING SPEED COUNT
        // overall frames
        private int ChasingSpeedMetric_FrameCountAll = 0;
        // frame counter
        private int ChasingSpeedMetric_FrameCount = 0;
        // either the DB is chasing position or not
        private bool ChasingSpeedMetric_ChasingPhase = false;

        // INTERNAL ODOMETRY CHECK
        // last frame rate measurement
        private DateTime Odometry_LastFrameMeasurement = DateTime.Now;
        // last position detected
        private Vector3 Odometry_LastPositionDeteced = Vector3.zero;
        // last orientation (Euler) detected
        private Vector3 Odometry_LastOrientationDeteced = Vector3.zero;
        // framerate count
        private double Odometry_FrameCount = 0;
        // the frame rate
        private double Odometry_FrameRate = 0.0;




        // ===== UNITY CALLBACKS ===== //

        // Start is called before the first frame update
        void Start()
        {
            if (EnableUnityTestFrame)
            {
                INIT_UnityFrameRate();
            }
            if (EnableChasingSpeedMetric)
            {
                INIT_ChasingSpeedMetric();
            }
            if (EnableOdometryCheck)
            {
                INIT_OdometryCheck();
            }
        }

        // Update is called once per frame
        void Update()
        {
            if(EnableUnityTestFrame)
            {
                UPDATE_UnityFrameRate();
            }
            if(EnableChasingSpeedMetric)
            {
                UPDATE_ChasingSpeedMetric();
            }
            if (EnableOdometryCheck)
            {
                UPDATE_OdometryCheck();
            }

            Ready();
        }



        // ===== TEST FRAME RATE ===== //

        public void EVENT_UnityFrameRate(bool opt)
        {
            EnableUnityTestFrame = opt;
        }

        private void INIT_UnityFrameRate()
        {
            if (!EnableUnityTestFrame) return;
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



        // ===== TEST CHASING SPEED METRIC ===== //

        public void EVENT_ChasingSpeedMetric(bool opt)
        {
            EnableUnityTestFrame = opt;
        }

        private void INIT_ChasingSpeedMetric()
        {
            if (!EnableChasingSpeedMetric) return;

            if (PosDbReference == null)
            {
                StaticLogger.Err(this, "unable to compute ChasingSpeedMetric; posDB was empty");
                EnableChasingSpeedMetric = false;
                return;
            }
        }

        private void UPDATE_ChasingSpeedMetric()
        {
            if (!EnableChasingSpeedMetric || PosDbReference.CurrentZone == null) return;

            if (ChasingSpeedMetric_FrameCount < MetricFrameResolution)
                ++ChasingSpeedMetric_FrameCount;
            else
            {
                ChasingSpeedMetric_FrameCountAll += MetricFrameResolution;
                ChasingSpeedMetric_FrameCount = 0;

                //Vector3 odom = Camera.main.transform.position;
                Vector3 odom = PosDbReference.LowLevelDatabase.SortReferencePosition;
                Vector3 posdb = PosDbReference.CurrentZone.AreaCenter;
                float dist = Vector3.Distance(odom, posdb);
                ChasingSpeedMetric_ChasingPhase = dist > PosDbReference.BaseDistance;

                string frame_no = $"{ChasingSpeedMetric_FrameCountAll}";
                string odom_x = odom.x.ToString(".00");
                string odom_y = odom.y.ToString(".00");
                string odom_z = odom.z.ToString(".00");
                string posdb_zone_idx = $"{PosDbReference.CurrentZone.PositionStableID}";
                string posdb_x = posdb.x.ToString(".00");
                string posdb_y = posdb.y.ToString(".00");
                string posdb_z = posdb.z.ToString(".00");
                string dist_odom_posdb = dist.ToString(".00");
                string in_range_vl = $"{!ChasingSpeedMetric_ChasingPhase}";
                string sort_quality = (EvaluateDbSort ? computeOrderQuality(odom).ToString(".00") : "");
                string posdb_load = $"{PosDbReference.LowLevelDatabase.Count}";
                string posdb_active_clusters = $"{PosDbReference.LowLevelDatabase.WorkingClusters}";
                string posdb_max_cluster = $"{PosDbReference.ClusterSize}";
                string posdb_max_idx = $"{PosDbReference.ClusterSize}";
                string busy_avg = PosDbReference.LowLevelDatabase.AverageBusyTime.ToString(".000");
                string busy_max = PosDbReference.LowLevelDatabase.MaxBusyTime.ToString(".000");
                string swap_avg = PosDbReference.LowLevelDatabase.AverageSwapPerCall.ToString(".0");
                string percent_hit = (PosDbReference.HitPercent * 100.0).ToString(".00");
                string percent_miss = (PosDbReference.MissPercent * 100.0).ToString(".00");

                CsvExportChasingSpeedMetric.EVENT_WriteCsv(new List<string> {
                    frame_no,
                    odom_x, odom_y, odom_z,
                    posdb_zone_idx,
                    posdb_x, posdb_y, posdb_z,
                    dist_odom_posdb, in_range_vl,
                    sort_quality,
                    posdb_load, posdb_active_clusters,
                    posdb_max_cluster, posdb_max_idx,
                    busy_avg, busy_max, 
                    swap_avg,
                    percent_hit, percent_miss
                }, ChasingSpeedMetricPrintCsvLines);
            }
        }



        // ===== TEST INTERNAL ODOMETRY CHECK ===== //

        public void EVENT_OdometryCheck(bool opt)
        {
            EnableOdometryCheck = opt;
        }

        private void INIT_OdometryCheck()
        {
            if (!EnableOdometryCheck) return;

            Odometry_LastPositionDeteced = Camera.main.transform.position;
            Odometry_LastOrientationDeteced = Camera.main.transform.rotation.eulerAngles;
        }

        private void UPDATE_OdometryCheck()
        {
            if (!EnableOdometryCheck) return;

            if (Odometry_FrameCount < OdometryFrameResolution)
                ++Odometry_FrameCount;
            else
            {
                double prevFrameRate = Odometry_FrameRate;
                Odometry_FrameRate = ((double)Odometry_FrameCount) / (DateTime.Now - Odometry_LastFrameMeasurement).TotalSeconds;
                Vector3 curPos = Camera.main.transform.position;
                Vector3 curRot = Camera.main.transform.rotation.eulerAngles;
                bool lockedPos = (curPos == Odometry_LastPositionDeteced);
                bool lockedRot = (curPos == Odometry_LastOrientationDeteced);

                if (EnableOdometryCsvExport)
                    CsvExportOdometry.EVENT_WriteCsv(new List<string> { 
                        // frame_rate_prev
                        prevFrameRate.ToString(".0000"),
                        // frame_rate_cur
                        Odometry_FrameRate.ToString(".0000"),
                        // delta_frame_rate
                        (Odometry_FrameRate - prevFrameRate).ToString(".0000"),
                        // pos_x_cur
                        $"{curPos.x}",
                        // pos_y_cur
                        $"{curPos.y}",
                        // pos_z_cur
                        $"{curPos.z}",
                        // rot_x_cur
                        $"{curRot.x}",
                        // rot_y_cur
                        $"{curRot.y}",
                        // rot_z_cur
                        $"{curRot.z}",
                        // pos_x_prev
                        $"{Odometry_LastPositionDeteced.x}",
                        // pos_y_prev
                        $"{Odometry_LastPositionDeteced.y}",
                        // pos_z_prev
                        $"{Odometry_LastPositionDeteced.z}",
                        // rot_x_prev
                        $"{Odometry_LastOrientationDeteced.x}",
                        // rot_y_prev
                        $"{Odometry_LastOrientationDeteced.y}",
                        // rot_z_prev
                        $"{Odometry_LastOrientationDeteced.z}",
                        // is_locked_position
                        $"{lockedPos}",
                        // is_locked_rotation
                        $"{lockedRot}"
                    });;

                Odometry_FrameCount = 0;
                Odometry_LastFrameMeasurement = DateTime.Now;
                Odometry_LastPositionDeteced = curPos;
                Odometry_LastOrientationDeteced = curRot;
            }
        }



        // ===== POSDB ORDER EVALUATION ===== //

        private double computeOrderQuality(Vector3 curPos)
        {
            double res = 0.0;
            for (int i = 0; i < PosDbReference.LowLevelDatabase.Count; ++i)
                res += ((double)i) * ((double)Vector3.Distance(curPos, PosDbReference.LowLevelDatabase.Database[i].AreaCenter));

            return res;
        }
    }
}
