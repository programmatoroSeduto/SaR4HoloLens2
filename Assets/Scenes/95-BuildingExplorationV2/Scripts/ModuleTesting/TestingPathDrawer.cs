using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Packages.VisualItems.LittleMarker.Components;
using SaR4Hololens2.Scenes.BuildingExplorationV2.Scripts.Utils;
using Packages.CustomRenderers.Components;
using SaR4Hololens2.Scenes.BuildingExplorationV2.Scripts.Components;
using SaR4Hololens2.Scenes.TestingFeatureMinimap.Scripts;

namespace SaR4Hololens2.Scenes.BuildingExplorationV2.Scripts.ModuleTesting
{
    public class TestingPathDrawer : MonoBehaviour
    {
        // ===== GUI ===== //

        [Header("Test Module Settings")]
        [Tooltip("Reference to the DB to test (already tuned)")]
        public PositionsDatabase DatabaseReference = null;
        [Tooltip("The path drawer to test (you can set the component from this script)")]
        public PathDrawer DrawerReference = null;

        [Header("Path Drawer Settings")]
        [Tooltip("Reference to the Minimap Structure tool (leave as None if already set by the component)")]
        public MinimapStructure MinimapReference = null;
        [Tooltip("Managed GameObject where to draw paths inside")]
        public GameObject RootObject = null;
        [Tooltip("Check this if you want to make the DB changable as well")]
        public bool CanChangeDB = false;



        // ===== PRIVATE ===== //

        // init script
        private bool init = false;
        // the previously received id
        private bool isFirst = true;



        // ===== UNITY CALLBACKS ===== //

        private void Start()
        {
            if (MinimapReference == null)
            {
                Debug.LogError("MinimapReference is missing!");
                return;
            }
            if (DatabaseReference == null)
            {
                Debug.LogError("DatabaseReference is missing!");
                return;
            }
            if (DrawerReference == null)
            {
                Debug.LogError("DrawerReference is missing!");
                return;
            }

            RootObject = (RootObject == null ? (DrawerReference.RootObject == null ? gameObject : DrawerReference.RootObject) : RootObject);
            DrawerReference.MinimapReference = MinimapReference;
            DrawerReference.RootObject = RootObject;

            UnityEvent onInsertCallbackEvent = new UnityEvent();
            onInsertCallbackEvent.AddListener(EVENT_OnInsertCallback);
            DatabaseReference.CallOnZoneCreated.Add(onInsertCallbackEvent);

            init = true;
        }



        // ===== FEATURE DRAW ON INSERT ===== //

        public void EVENT_OnInsertCallback()
        {
            PositionDatabaseWaypoint pos = DatabaseReference.DataZoneCreated;

            if(isFirst)
            {
                DrawerReference.CreatePoint(pos, cleanBefore: true, canModifyPos: CanChangeDB);
                isFirst = false;
            }
            else
            {
                foreach(PositionDatabasePath link in pos.Paths)
                {
                    PositionDatabaseWaypoint startPos = link.Next(pos);
                    if (DrawerReference.IsHandledByDrawerWaypoint(startPos.ObjectCenterReference.name))
                        DrawerReference.CreatePathSegment(startPos, pos, cleanBefore: false, canModifyPos: CanChangeDB);
                }
            }
        }





    }
}
