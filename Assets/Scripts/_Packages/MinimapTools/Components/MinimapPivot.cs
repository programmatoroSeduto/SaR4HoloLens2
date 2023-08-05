using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
using Microsoft.MixedReality.Toolkit.UI.BoundsControlTypes;

namespace Packages.MinimapTools.Components
{
    public class MinimapPivot : MonoBehaviour
    {
        // ===== GUI ===== //

        [Header("Main Settings")]
        [Tooltip("Minimap main root object reference (not the external wrapper!)")]
        public GameObject MapRoot = null;
        [Tooltip("The outmost wrapper of the minimap structure")]
        public GameObject MapWrapper = null;
        [Tooltip("Reference to the Game Object representing the box of the minimap")]
        public GameObject BoxReference = null;
        [Tooltip("Minimap Pivot reference")]
        public GameObject MapPivot = null;
        [Tooltip("(test) Apply pivot displace?")]
        public bool ApplyDisplacement = true;
        [Tooltip("Apply rotation wrt the camera")]
        public bool RotateMapByCamera = true;
        [Tooltip("Scaling factor for the main object")]
        [Min(0.0f)]
        public float ScaleFactorPercent = 100.0f;

        [Header("Minimap Size")]
        [Tooltip("Overall space starting from a cube vertex")]
        public Vector3 SpaceSize = Vector3.one;



        // ===== PRIVATE ===== //

        // ...
        private BoundsControl boundsControl = null;
        // prev space size
        private Vector3 prevSpaceSize = -Vector3.one;



        // ===== UNITY CALLBACKS ===== //

        private void Update()
        {
            if (MapWrapper == null || MapPivot == null || MapRoot == null) return;

            AdjustBoundsControl();
            Vector3 avgPos = ComputeAveragePosition();

            if (ApplyDisplacement)
                MapRoot.transform.localPosition = -avgPos;

            if(MapWrapper != null)
                MapWrapper.transform.localScale = (ScaleFactorPercent / 100.0f) * Vector3.one;

            if (RotateMapByCamera)
                MapPivot.transform.localRotation = Quaternion.Euler(x: 0.0f, y: -Camera.main.transform.rotation.eulerAngles.y, z: 0.0f);
        }



        // ===== FEATURE COMPUTE AVERAGE POSITION ===== //

        private Vector3 ComputeAveragePosition()
        {
            Vector3 avgPos = Vector3.zero;
            int N = 0;
            foreach (Transform t in MapRoot.transform)
            {
                avgPos += t.localPosition;
                ++N;
            }

            return (N > 0 ? (1.0f / ((float)N)) * avgPos : Vector3.zero);
        }



        // ===== FEATURE DYNAMIC BOUNDS CONTROL ===== //

        private void AdjustBoundsControl()
        {
            if(SpaceSize.x != prevSpaceSize.x || SpaceSize.y != prevSpaceSize.y || SpaceSize.z != prevSpaceSize.z)
            {
                prevSpaceSize = SpaceSize;
                BoxReference.transform.localScale = SpaceSize;
            }
        }
    }
}
