using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Project.Scripts.Components;
using Project.Scripts.Utils;

namespace Project.Scenes.TestingBenchmark.Scripts.Components
{
    public class MovePlayerBasicLine : ProjectMonoBehaviour
    {
        // ===== GUI ===== //

        [Header("Test Movement Settings")]
        [Tooltip("either the object beholding the component or another one")]
        public GameObject PlayerObject = null;
        [Min(0.0f)]
        public float BaseSpeed = 0.0f;
        public Vector3 PathDirection = Vector3.forward;
        [Min(0.0f)]
        public float PathDistance = 5.0f;

        [Header("Test Behaviour Static Settings")]
        public bool UseDelayedStart = false;
        [Min(0.0f)]
        public float StartDelay = 0.0f;
        public bool UseMovementStops = false;
        [Min(0.0f)]
        public float MaxStopTime = 0.0f;
        public bool UseRandomSpeedFromBase = false;
        [Min(0.0f)]
        public float MaxSpeedVariance = 0.0f;

        [Header("Test Behaviour Dynamic Settings")]
        public bool TargetIsEndPoint = false;
        [Tooltip("One-shot check: select to update the text area in the output zone. The check is reset after update")]
        public bool TestOutputOnTextArea = false;

        [Header("Output only")]
        public Vector3 PlayerStartPoint = Vector3.zero;
        public Vector3 PlayerEndPoint = Vector3.zero;
        public float PlayerSpeed = 0.0f;
        public float PlayerDistFromTarget = 0.0f;
        [TextArea(5, 10)]
        public string TestOutput = "";



        // ===== PRIAVTE ===== //

        private int frameCount = 0;
        private GameObject goPlayer = null;
        private Vector3 startPoint = Vector3.zero;
        private Vector3 endPoint = Vector3.zero;
        private enum PlayerStatus
        {
            startDelay,
            planning,
            moving,
            staying
        };
        private PlayerStatus playerStatus = PlayerStatus.startDelay;
        private float delay = -1.0f;
        private float currentSpeed = 0.0f;
        private List<string> testStepOutput = new List<string>();  // status, nextStatus, timestamp, tx, ty, tz, curx, cury, curz, dist, speed, delay
        private List<List<string>> testOutputStaging = new List<List<string>>();


        
        // ===== UNITY CALLBACKS ===== //

        // Start is called before the first frame update
        void Start()
        {
            if (PlayerObject == null)
            {
                goPlayer = gameObject;
                PlayerObject = goPlayer;
            }
            else
                goPlayer = PlayerObject;

            // start and end points of the line
            startPoint = PlayerObject.transform.position;
            PlayerStartPoint = startPoint;
            
            endPoint = startPoint + PathDistance * PathDirection;
            PlayerEndPoint = endPoint;

            // movement
            TargetIsEndPoint = false;

            // fist status
            if (!UseDelayedStart)
                playerStatus = PlayerStatus.planning;
        }

        // Update is called once per frame
        void Update()
        {
            ++frameCount;

            switch (playerStatus)
            {
                case PlayerStatus.startDelay:
                    Update_StartDelay();
                    break;
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

            if(TestOutputOnTextArea)
            {
                UpdateTestOutput();
                TestOutputOnTextArea = false;
            }
        }

        private void Update_StartDelay()
        {
            if(delay < 0)
            {
                delay = StartDelay;
                currentSpeed = 0.0f;
                updateTestStep("startDelay", "startDelay");
                return;
            }

            delay -= Time.fixedDeltaTime;

            if (delay < 0.0f)
            {
                delay = -1;
                playerStatus = PlayerStatus.planning;
                updateTestStep("startDelay", "planning");
                return;
            }
        }

        private void Update_Planning()
        {
            TargetIsEndPoint = !TargetIsEndPoint;
            PlayerDistFromTarget = Vector3.Distance(goPlayer.transform.position, (TargetIsEndPoint ? endPoint : startPoint));

            currentSpeed = positiveOrZero(BaseSpeed + (UseRandomSpeedFromBase ? UnityEngine.Random.value * MaxSpeedVariance : 0.0f));
            PlayerSpeed = currentSpeed;

            playerStatus = PlayerStatus.moving;
            updateTestStep("planning", "moving");
        }

        private void Update_Moving()
        {
            Vector3 target = (TargetIsEndPoint ? endPoint : startPoint);
            Vector3 direction = (target - goPlayer.transform.position).normalized;
            PlayerDistFromTarget = Vector3.Distance(goPlayer.transform.position, (TargetIsEndPoint ? endPoint : startPoint));

            currentSpeed = positiveOrZero(BaseSpeed + (UseRandomSpeedFromBase ? UnityEngine.Random.value * MaxSpeedVariance : 0.0f));
            PlayerSpeed = currentSpeed;

            if(PlayerDistFromTarget < 0.075f)
            {
                playerStatus = PlayerStatus.planning;
                updateTestStep("moving", "planning");
            }
            else if(UseMovementStops && (UnityEngine.Random.value >= 0.75f))
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
            if(testStepOutput.Count > 0)
            {
                testOutputStaging.Add(testStepOutput);
                testStepOutput = new List<string>();
            }
            Vector3 target = (TargetIsEndPoint ? endPoint : startPoint);
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

        private float positiveOrZero(float val)
        {
            return (val >= 0.0f ? val : 0.0f);
        }
    }
}
