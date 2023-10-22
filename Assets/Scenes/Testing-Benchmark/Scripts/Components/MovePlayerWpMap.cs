using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Project.Scripts.Components;
using Project.Scripts.Utils;
using Project.Scenes.TestingBenchmark.Scripts.Utils;

namespace Project.Scenes.TestingBenchmark.Scripts.Components
{
    public class MovePlayerWpMap : ProjectMonoBehaviour
    {
        // ===== GUI ===== //

        [Header("Test Movement Settings")]
        [Tooltip("either the object beholding the component or another one")]
        public GameObject PlayerObject = null;
        [Min(0.0f)]
        public float BaseSpeed = 0.0f;
        [Tooltip("The list of waypoints")]
        public List<PlayerWaypoint> TestMap = new List<PlayerWaypoint>();
        [Tooltip("The first point is particular: it needs the near points to be specified")]
        public List<string> RootNearWpNames = new List<string>();
        public string RootWpName = "root";

        [Header("Test Behaviour Static Settings")]
        public bool UseMovementStops = false;
        [Min(0.0f)]
        public float MaxStopTime = 0.0f;
        public bool UseRandomSpeedFromBase = false;
        [Min(0.0f)]
        public float MaxSpeedVariance = 0.0f;
        [Tooltip("Using this setting, the player can move to any of the defined positions")]
        public bool IgnoreNearPoints = false;

        [Header("Test Behaviour Dynamic Settings")]
        [Tooltip("Stop!")]
        public bool Stop = false;
        [Tooltip("One-shot check: select to update the text area in the output zone. The check is reset after update")]
        public bool TestOutputOnTextArea = false;

        [Header("Output only")]
        public string PlayerCurrentPosition = "";
        public string PlayerTargetPosition = "";
        public float PlayerSpeed = 0.0f;
        public float PlayerDistFromTarget = 0.0f;
        [TextArea(5, 10)]
        public string TestOutput = "";



        // ===== PRIAVTE ===== //

        private int frameCount = 0;
        private GameObject goPlayer = null;
        private Dictionary<string, PlayerWaypoint> map = new Dictionary<string, PlayerWaypoint>();
        private enum PlayerStatus
        {
            planning,
            moving,
            staying
        };
        private PlayerStatus playerStatus = PlayerStatus.planning;
        private float delay = -1.0f;
        private float currentSpeed = 0.0f;
        private List<string> testStepOutput = new List<string>();  // status, nextStatus, timestamp, tx, ty, tz, curx, cury, curz, dist, speed, delay
        private List<List<string>> testOutputStaging = new List<List<string>>();
        private PlayerWaypoint curPos = null;
        private PlayerWaypoint trgPos = null;
        private Vector3 targetVector = Vector3.zero;



        // ===== UNITY CALLBACKS ===== //

        // Start is called before the first frame update
        void Start()
        {
            if(TestMap.Count == 0)
            {
                StaticLogger.Err(this, "Map is empty; closing");
                return;
            }
            else if (RootNearWpNames.Count == 0 && !IgnoreNearPoints)
            {
                StaticLogger.Err(this, "RootNearWpNames cannot be empty when IgnoreNearPoints is not checked; closing");
                return;
            }
            if (PlayerObject == null)
            {
                goPlayer = gameObject;
                PlayerObject = goPlayer;
            }
            else
                goPlayer = PlayerObject;

            // load map
            map.Add(RootWpName, new PlayerWaypoint()
            {
                WpName = RootWpName,
                TargetGo = null,
                TargetVector = Vector3.zero,
                NearWpNames = RootNearWpNames
            });
            int wpNo = 0;
            foreach(PlayerWaypoint wp in TestMap)
            {
                ++wpNo;
                if(wp.WpName == RootWpName)
                {
                    StaticLogger.Err(this, $"point number {wpNo} cannot have the name of the root; got '{RootWpName}', closing");
                    return;
                }
                map.Add(wp.WpName, wp);
            }

            curPos = null;
            trgPos = map[RootWpName];
            PlayerCurrentPosition = null;
            PlayerTargetPosition = RootWpName;

        }

        // Update is called once per frame
        void Update()
        {
            ++frameCount;

            if(!Stop)
                switch (playerStatus)
                {
                    case PlayerStatus.planning:
                        Update_Planning();
                        break;
                    case PlayerStatus.moving:
                        Update_Moving();
                        break;
                    case PlayerStatus.staying:
                        Update_Staying();
                        break;
                }

            if (TestOutputOnTextArea)
            {
                UpdateTestOutput();
                TestOutputOnTextArea = false;
            }
        }

        private void Update_Planning()
        {
            curPos = trgPos;
            if (IgnoreNearPoints)
            {
                trgPos = map[chooseRandomPos()];
            }
            else
            {
                string nextName = chooseRandomPos(curPos.NearWpNames);
                if(nextName == "")
                    trgPos = map[chooseRandomPos()];
                else
                    trgPos = map[nextName];
            }
            targetVector = trgPos.Target;
            PlayerDistFromTarget = Vector3.Distance(goPlayer.transform.position, targetVector);

            currentSpeed = positiveOrZero(BaseSpeed + (UseRandomSpeedFromBase ? UnityEngine.Random.value * MaxSpeedVariance : 0.0f));
            PlayerSpeed = currentSpeed;

            playerStatus = PlayerStatus.moving;
            updateTestStep("planning", "moving");
        }

        private void Update_Moving()
        {
            Vector3 direction = (targetVector - goPlayer.transform.position).normalized;
            PlayerDistFromTarget = Vector3.Distance(goPlayer.transform.position, targetVector);

            currentSpeed = positiveOrZero(BaseSpeed + (UseRandomSpeedFromBase ? UnityEngine.Random.value * MaxSpeedVariance : 0.0f));
            PlayerSpeed = currentSpeed;

            if (PlayerDistFromTarget < 0.075f)
            {
                playerStatus = PlayerStatus.planning;
                updateTestStep("moving", "planning");
            }
            else if (UseMovementStops && (UnityEngine.Random.value >= 0.75f))
            {
                playerStatus = PlayerStatus.staying;
                updateTestStep("moving", "staying");
            }
            else
            {
                goPlayer.transform.position += (currentSpeed * Time.fixedDeltaTime) * direction;
            }
        }

        private void Update_Staying()
        {
            if (delay < 0)
            {
                delay = UnityEngine.Random.value * MaxStopTime;
                currentSpeed = 0.0f;
                updateTestStep("staying", "staying");
                return;
            }

            delay -= Time.fixedDeltaTime;

            if (delay < 0.0f)
            {
                delay = -1;
                playerStatus = PlayerStatus.moving;
                updateTestStep("staying", "moving");
                return;
            }
        }

        private void updateTestStep(string status, string nextStatus)
        {
            // frameCountstatus, nextStatus, timestamp, tx, ty, tz, curx, cury, curz, dist, speed, delay
            if (testStepOutput.Count > 0)
            {
                testOutputStaging.Add(testStepOutput);
                testStepOutput = new List<string>();
            }
            Vector3 target = targetVector;
            Vector3 curp = goPlayer.transform.position;

            testStepOutput.Add(frameCount.ToString());
            testStepOutput.Add(status);
            testStepOutput.Add(nextStatus);
            testStepOutput.Add(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));
            testStepOutput.Add(target.x.ToString("0.000"));
            testStepOutput.Add(target.y.ToString("0.000"));
            testStepOutput.Add(target.z.ToString("0.000"));
            testStepOutput.Add(curp.x.ToString("0.000"));
            testStepOutput.Add(curp.y.ToString("0.000"));
            testStepOutput.Add(curp.z.ToString("0.000"));
            testStepOutput.Add(PlayerDistFromTarget.ToString("0.000"));
            testStepOutput.Add(currentSpeed.ToString("0.000"));
            testStepOutput.Add(delay.ToString("0.000"));
        }

        private void UpdateTestOutput()
        {
            string header = "frame,status,nextStatus,timestamp,tx,ty,tz,curx,cury,curz,dist,speed,delay\n";
            string ss = "";
            foreach (List<string> ls in testOutputStaging)
                ss += String.Join(",", mapQuoted(ls)) + "\n";

            TestOutput = header + ss;
        }

        private List<string> mapQuoted(List<string> ls)
        {
            List<string> res = new List<string>();
            foreach (string s in ls)
                res.Add("\"" + s + "\"");
            return res;
        }

        private string chooseRandomPos(List<string> inside = null)
        {
            List<string> ls = null;
            
            if (inside == null)
            {
                ls = new List<string>(map.Keys);
                ls.Remove(curPos.WpName);
            }
            else
            {
                ls = inside;
                /*
                string ss = "chosing inside: ";
                foreach (string s in ls)
                    ss += $"'{s}' ";
                StaticLogger.Info(this, ss);
                */
            }

            if (ls.Count == 0)
            {
                // StaticLogger.Info(this, $"random res: returning empty");
                return "";
            }
            else
            {
                string res = ls[UnityEngine.Random.Range(0, ls.Count)];
                // StaticLogger.Info(this, $"random res: '{res}'");
                return res;
            }
        }

        private float positiveOrZero(float val)
        {
            return (val >= 0.0f ? val : 0.0f);
        }


    }
}
