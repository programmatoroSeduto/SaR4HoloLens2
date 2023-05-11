using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;


namespace SaR4Hololens2.Scripts.ModuleTesting
{
    public class TestingHandFeatures : MonoBehaviour
    {
        private GameObject sphere;

        private void Start()
        {
            // sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        }

        void Update()
        {
            foreach (var source in CoreServices.InputSystem.DetectedInputSources)
            {
                // Ignore anything that is not a hand because we want articulated hands
                if (source.SourceType == Microsoft.MixedReality.Toolkit.Input.InputSourceType.Hand)
                {
                    foreach (var p in source.Pointers)
                    {
                        if (p is IMixedRealityNearPointer)
                        {
                            // Ignore near pointers, we only want the rays
                            continue;
                        }
                        if (p.Result != null)
                        {
                            var startPoint = p.Position;
                            var endPoint = p.Result.Details.Point;
                            // var hitObject = p.Result.Details.Object;
                            // sphere.transform.localScale = Vector3.one;
                            // sphere.transform.position = endPoint;
                            Debug.Log($"Start at {startPoint} -- ends at {endPoint}");
                        }

                    }
                }
            }
        }
    }

}