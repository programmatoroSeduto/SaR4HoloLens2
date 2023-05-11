using System.Collections;
using System.Collections.Generic;

using Microsoft.MixedReality.Toolkit.Input;

using UnityEngine;

namespace SaR4Hololens2.Scripts.ModuleTesting
{
    public class TestingGestures : MonoBehaviour, IMixedRealityGestureHandler
    {
        public void OnGestureCanceled(InputEventData eventData)
        {
            // throw new System.NotImplementedException();
        }

        public void OnGestureCompleted(InputEventData eventData)
        {
            // throw new System.NotImplementedException();
            Debug.Log($"Gesture completed! {eventData.Handedness.ToString()}");
            
        }

        public void OnGestureStarted(InputEventData eventData)
        {
            // throw new System.NotImplementedException();
        }

        public void OnGestureUpdated(InputEventData eventData)
        {
            // throw new System.NotImplementedException();
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}