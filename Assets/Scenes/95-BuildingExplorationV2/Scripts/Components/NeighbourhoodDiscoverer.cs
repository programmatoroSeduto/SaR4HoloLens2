using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Packages.VisualItems.LittleMarker.Components;
using SaR4Hololens2.Scenes.BuildingExplorationV2.Scripts.Utils;
using Packages.CustomRenderers.Components;
using SaR4Hololens2.Scenes.BuildingExplorationV2.Scripts.Components;
using SaR4Hololens2.Scenes.TestingFeatureMinimap.Scripts;



namespace SaR4Hololens2.Scenes.BuildingExplorationV2.Scripts.Components
{
    public class NeighbourhoodDiscoverer : MonoBehaviour
    {
        // ===== GUI ===== //

        [Header("Main settings")]
        [Tooltip("Reference to the DB to test (already tuned)")]
        public PositionsDatabase DatabaseReference = null;
        [Tooltip("The path drawer to test (you can set the component from this script)")]
        public PathDrawer DrawerReference = null;
        [Tooltip("(dynamic) Max Drawable radius")]
        public float DrawableRadius = 10.0f;
        [Tooltip("(dynamic) update period")]
        public float Period = 2.5f;

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
        // main coroutine
        private Coroutine COR_NeighborhoodDiscoverer = null;
        // previous ID from the server
        private int prevID = -1;



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

            init = true;
        }

        private void Update()
        {
            PositionDatabaseWaypoint wp = DatabaseReference.CurrentZone;

            if (wp.PositionID == prevID) return;
            else prevID = wp.PositionID;

            HashSet<string> instances = drawNeighborhood(wp, remainingDistance: DrawableRadius);
            DrawerReference.RemoveMarkerAll(ExclusionListWps: instances);
        }



        // ===== FEATURE NEIGHBORHOOD EXPLORATION ===== //

        private HashSet<string> drawNeighborhood(PositionDatabaseWaypoint wp, PositionDatabaseWaypoint userPos = null, float remainingDistance = float.MaxValue, HashSet<string> instances = null, int iterID = 0)
        {
            /*
             * se instances != null, allora il wp della chiamata della funzione è già stato istanziato
             * altrimenti va istanziato e aggiunto alla instances
             * 
             * a prescindere, l'algoritmo istanzia il waypoint appena ci arriva sopra, dopodichè itera
             * 
             * MODIFICA : oltre che ragionare in termini di distanza percorsa, ragionerei anche in termini di 
             * distanza diretta rispetto all'utente
             * */

            // Debug.Log($"[{iterID}] ");

            // Debug.Log($"[{iterID}] START");
            string ss = "";

            if (instances == null)
            {
                // Debug.Log($"[{iterID}] first cycle!");
                instances = new HashSet<string>();
                userPos = wp;

                string tag = DrawerReference.CreatePoint(wp, canModifyPos: CanChangeDB);
                if(tag == null)
                {
                    // Debug.LogError($"Cannot instanciate wp with tag '{tag}'!");
                    return instances;
                }
                instances.Add(tag);
            }

            // Debug.Log($"[{iterID}] wp with ID:{wp.PositionID} remainingDistance:{remainingDistance}");

            if (remainingDistance > 0.0f || Vector3.Distance(userPos.AreaCenter, wp.AreaCenter) <= DrawableRadius)
            {
                // Debug.Log($"[{iterID}] wp with ID:{wp.PositionID} found links:{wp.Paths.Count}");
                foreach (PositionDatabasePath link in wp.Paths)
                {
                    PositionDatabaseWaypoint wpNext = link.Next(wp);
                    if (instances.Contains(DrawerReference.TagOf(wpNext)))
                    {
                        // Debug.Log($"[{iterID}] next with ID:{wpNext.PositionID} already instanced; skip");
                        continue;
                    }
                    
                    // Debug.Log($"[{iterID}] next with ID:{wpNext.PositionID} visualizing wp");
                    string tag = DrawerReference.CreatePoint(wpNext, canModifyPos: CanChangeDB);
                    if (tag == null)
                    {
                        // Debug.LogError($"Cannot instanciate wp with tag '{tag}'!");
                        return instances;
                    }
                    // Debug.Log($"[{iterID}] next with ID:{wpNext.PositionID} visualizing link");
                    DrawerReference.CreatePath(wp, wpNext);
                    instances.Add(tag);

                    ss = "";
                    foreach (var ttag in instances) ss += ttag + ",";
                    // Debug.Log($"[{iterID}] tags in instance: {ss}");

                    // Debug.Log($"[{iterID}] iterating with iterID:{iterID+1}");
                    instances = drawNeighborhood(wpNext, userPos, (remainingDistance - link.Distance), instances, iterID + 1);
                }
            }

            // Debug.Log($"[{iterID}] END");
            return instances;
        }

    }
}
