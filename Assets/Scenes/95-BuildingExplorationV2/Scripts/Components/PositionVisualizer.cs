using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Packages.VisualItems.LittleMarker.Components;
using SaR4Hololens2.Scenes.BuildingExplorationV2.Scripts.Utils;
using Packages.CustomRenderers.Components;
using SaR4Hololens2.Scenes.TestingFeatureMinimap.Scripts;

namespace SaR4Hololens2.Scenes.BuildingExplorationV2.Scripts.Components
{
    public class PositionVisualizer : MonoBehaviour
    {
        // ===== GUI ===== //

        [Header("Main settings")]
        [Tooltip("Database reference (Path Drawer instance)")]
        public PathDrawer DrawerReference = null;
        [Tooltip("Reference to the Position Db (for getting current position)")]
        public PositionsDatabase DatabaseReference = null;
        [Tooltip("Reference to the material used for visualizing the current position")]
        public Material CursorMaterial = null;

        [Header("Debug Zone")]
        public int prevID = -1;
        public int ID = 0;



        // ===== PRIVATE ===== //

        // ...
        private bool init = false;
        // ...
        private UnityEvent onChangeCallbackEvent = null;
        // previouly registered position
        private PositionDatabaseWaypoint prevWp = null;
        // reference to the base material
        private Material baseMaterial = null;



        // ===== UNITY CALLBACKS ===== //

        // Start is called before the first frame update
        void Start()
        {
            if(DrawerReference == null)
            {
                Debug.LogError("DrawerReference instance missing");
                return;
            }
            if (DatabaseReference == null)
            {
                Debug.LogError("DatabaseReference instance missing");
                return;
            }
            if (CursorMaterial == null)
            {
                Debug.LogError("CursorMaterial instance missing");
                return;
            }

            /*
            onChangeCallbackEvent = new UnityEvent();
            onChangeCallbackEvent.AddListener(onChangeCallback);
            DatabaseReference.CallOnZoneChanged.Add(onChangeCallbackEvent);
            */

            init = true;
        }

        // Update is called once per frame
        void Update()
        {
            // if (!init) return;
            onChangeCallback();
        }



        // ===== FEATURE ENABLE CURRENT POSITION ===== //

        private void onChangeCallback()
        {
            /*
            Debug.Log($"onChangeCallback() -- START");
            */

            if (!init) return;
            PositionDatabaseWaypoint wp = DatabaseReference.CurrentZone;
            ID = wp.PositionID;

            if(prevWp != null && prevWp.PositionID == wp.PositionID)
            {
                // Debug.Log($"onChangeCallback() -- position unchanged -- CLOSING");
                return;
            }

            if (prevWp != null)
            {
                // Debug.Log($"onChangeCallback() -- Found prevWP:{prevWp.PositionID}");

                string wpPrevTag = DrawerReference.TagOf(prevWp);
                PositionDatabaseWaypointHandle wpPrevObj = DrawerReference.GetHandleOfWaypoint(wpPrevTag);
                if(wpPrevObj == null)
                {
                    // Debug.LogError($"onChangeCallback() -- DrawerReference.GetHandleOfWaypoint(wpPrevTag:{wpPrevTag}) returned NULL");
                }

                if (!swichMarkerStatus(wpPrevObj, false)) return;

                prevWp = null;
                prevID = -1;
            }

            string wpTag = DrawerReference.TagOf(wp);
            // Debug.Log($"onChangeCallback() -- current zone is {wp.PositionID} with TAG:{wpTag}");
            if (!DrawerReference.IsHandledByDrawerWaypoint(wpTag))
            {
                // Debug.Log($"onChangeCallback() -- unknown TAG:{wpTag} -- CLOSING");
                return;
            }

            PositionDatabaseWaypointHandle wpObj = DrawerReference.GetHandleOfWaypoint(wpTag);

            if (!swichMarkerStatus(wpObj, true)) return;

            prevWp = wp;
            prevID = wp.PositionID;
        }

        private bool swichMarkerStatus(PositionDatabaseWaypointHandle wpHandle, bool opt)
        {
            GameObject wpGo = wpHandle.gameObject;
            if(wpGo.transform.Find("home") == null)
            {
                Debug.LogError("swichMarkerStatus() : can't get the objec!");
                return false;
            }
            GameObject renderedGo = wpGo.transform.Find("home").gameObject;

            if (opt)
            {
                // attiva (current)
                baseMaterial = renderedGo.GetComponent<Renderer>().material;
                renderedGo.GetComponent<Renderer>().material = CursorMaterial;

                foreach (PositionDatabasePath link in wpHandle.DatabasePosition.Paths)
                {
                    if (!DrawerReference.IsHandledByDrawerPath(DrawerReference.TagOf(link)))
                        continue;

                    ((FlexibleLineRenderer)link.Renderer).SetLineColor(Color.red);
                }
            }
            else
            {
                // disattiva (prev)
                renderedGo.GetComponent<Renderer>().material = baseMaterial;

                foreach (PositionDatabasePath link in wpHandle.DatabasePosition.Paths)
                {
                    if (!DrawerReference.IsHandledByDrawerPath(DrawerReference.TagOf(link)))
                        continue;

                    ((FlexibleLineRenderer)link.Renderer).SetLineColor(Color.green);
                }
            }

            return true;
        }
    }
}