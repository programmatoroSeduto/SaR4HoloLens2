using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Packages.VisualItems.LittleMarker.Components;
using SaR4Hololens2.Scenes.BuildingExplorationV2.Scripts.Utils;
using Packages.CustomRenderers.Components;
using SaR4Hololens2.Scenes.TestingFeatureMinimap.Scripts;
using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;

namespace SaR4Hololens2.Scenes.BuildingExplorationV2.Scripts.Components
{
    public class MinimapTool : MonoBehaviour
    {
        // ===== GUI ===== //

        [Header("Main settings")]
        [Tooltip("Map Main root")]
        public GameObject MainRootReference = null;
        [Tooltip("Inner root reference")]
        public GameObject InnerRootReference = null;



        // ===== PUBLIC ===== //

        // ...



        // ===== PRIVATE ===== //

        // init for class initialization
        private bool init = false;
        // Bounds control reference
        private BoundsControl boundsControl = null;
        // ...
        private BoxCollider boxCollider = null;
        // ...
        private NearInteractionGrabbable nearInteractionGrabbable = null;
        // ...
        private ObjectManipulator objectManipulator = null;



        // ===== UNITY CALLBACKS ===== //

        private void Start()
        {
            init = Init();
        }

        private bool Init()
        {
            if (MainRootReference == null || InnerRootReference == null)
                return false;

            if(boundsControl == null)
            {
                boxCollider = MainRootReference.AddComponent<BoxCollider>();
                boundsControl = MainRootReference.AddComponent<BoundsControl>();
                nearInteractionGrabbable = MainRootReference.AddComponent<NearInteractionGrabbable>();
                objectManipulator = MainRootReference.AddComponent<ObjectManipulator>();
            }

            boxCollider.size = InnerRootReference.transform.localScale;
            return true;
        }

        private void Update()
        {
            if(!init)
            {
                init = Init();
                return;
            }

            boxCollider.size = InnerRootReference.transform.localScale;
        }



        // ===== FEATURE ===== //

        // ...
    }
}