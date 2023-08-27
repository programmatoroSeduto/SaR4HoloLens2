using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Project.Scripts.Utils;
using Packages.PositionDatabase.Components;

namespace Project.Scripts.Components
{
    public class CalibrationUtility : MonoBehaviour
    {
        // ===== GUI ===== //

        [Header("General Properties")]
        [Tooltip("A sound can be reproduced while the calibration is in progress")]
        public AudioSource AudioSourceComponent = null;



        // ===== PRIVATE ===== //

        // if the calibration has been successfully done or not
        private bool calibrationDone = false;
        // calibration status
        private bool calibrationInProgress = false;



        // ===== UNITY CALLBACKS ===== //

        private void Start()
        {
            if (AudioSourceComponent == null)
                StaticLogger.Warn(this, "AudioSource not found", logLayer: 2);
            else if (AudioSourceComponent.isPlaying)
                AudioSourceComponent.Stop();
        }

        // update the position of the static object
        private void Update()
        {
            if(calibrationDone)
                StaticTransform.AppPosition = Camera.main.transform.position;
        }



        // ===== CALIBRATION COMMAND ===== //

        // The main command to start the calibration
        public void EVENT_Calibrate(bool redo = false)
        {
            StaticLogger.Info(this, $"EVENT_Calibrate(bool redo = {redo})", logLayer: 2);

            if (calibrationInProgress)
            {
                StaticLogger.Warn(this, "Cannot calibrate when another calibration process is running", logLayer: 2);
                return;
            }

            if (redo)
                StaticLogger.Info(this, "Rerunning calibration");
            else if (calibrationDone)
            {
                StaticLogger.Warn(this, "trying to re-calibrate without the 'redo' flag; this is not allowed. ");
                return;
            }

            string refName = StaticAppSettings.GetOpt("CalibrationPositionID", "");
            if(refName == "")
            {
                StaticLogger.Warn(this, "Configuration Issue! reference name not set in the appSettings!");
                return;
            }

            calibrationInProgress = true;
            StartCoroutine(BSCOR_CalibrationProcess(refName));
        }

        // the calibration base coroutine
        private IEnumerator BSCOR_CalibrationProcess(string refName)
        {
            yield return null;

            PlayAudio();
            yield return new WaitForSecondsRealtime(1.0f);

            StaticLogger.Info(this, "Collecting calibration positions ... ", logLayer: 1);
            
            GameObject pointer = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            for(int i=30; i>0; --i)
            {
                pointer.transform.localPosition = Camera.main.transform.position + Camera.main.transform.rotation * Vector3.forward;
                pointer.transform.localScale = 0.1f * Vector3.one;
                yield return new WaitForSecondsRealtime(0.1f);
            }

            Vector3 refPos = Camera.main.transform.position;
            Quaternion refRot = Camera.main.transform.rotation;

            pointer.transform.localScale = 0.25f * Vector3.one;
            yield return new WaitForSecondsRealtime(1.0f);
            DestroyImmediate(pointer);
            StaticLogger.Info(this, "Collecting calibration positions ... OK ", logLayer: 1);

            StaticLogger.Info(this, "Calibrating the device ... ", logLayer: 1);
            StaticTransform.SetReference(refName, refPos, refRot);
            StaticLogger.Info(this, "Calibrating the device ... OK ", logLayer: 1);

            StaticLogger.Info(this, $"Calibration test\n" + 
                $"\tCurrent position: {StaticTransform.ReferencePosition} (expected any coordinate, the coordinates of the reference point)\n" + 
                $"\tTranformed Position: {StaticTransform.ToRefPoint(StaticTransform.ReferencePosition)} (expected very close to zero)\n" + 
                $"\tRefPos: {refPos}\n" + 
                $"\tRefRot (euler Angles): {refRot.eulerAngles}\n", logLayer: 2);

            StaticLogger.Info(this, "Enabling position database ... ", logLayer: 1);
            PositionsDatabase db = StaticAppSettings.AppSettings.PositionsDatabase;
            if(db == null)
            {
                StaticLogger.Warn(this, "Global reference to main position database found unset at runtime! Unexpected");
            }
            else
            {
                db.enabled = true;
                StaticLogger.Info(this, "Enabling position database ... OK ", logLayer: 1);
            }

            StopAudio();
            yield return new WaitForSecondsRealtime(1.0f);

            calibrationInProgress = false;
            calibrationDone = true;
        }



        // ===== AUDIO EFFECT NOTIFICATION AND MINIMAL USER INTERACTION ===== //

        private void PlayAudio()
        {
            StaticLogger.Info(this, "Starting calibration audio effect ... ", logLayer: 2);
            if (AudioSourceComponent == null)
                StaticLogger.Warn(this, "AudioSource not found, silently performing calibration", logLayer: 2);
            else
            {
                AudioSourceComponent.loop = true;
                AudioSourceComponent.Play();
                StaticLogger.Info(this, "Starting calibration audio effect ... OK ", logLayer: 2);
            }
        }

        private void StopAudio()
        {
            if (AudioSourceComponent != null)
            {
                StaticLogger.Info(this, "Stopping calibration audio effect ... ", logLayer: 2);
                AudioSourceComponent.Stop();
                StaticLogger.Info(this, "Stopping calibration audio effect ... OK ", logLayer: 2);
            }
        }
    }
}
