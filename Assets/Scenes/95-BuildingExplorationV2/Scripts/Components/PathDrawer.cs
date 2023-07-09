using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Packages.VisualItems.LittleMarker.Components;
using SaR4Hololens2.Scenes.BuildingExplorationV2.Scripts.Utils;
using Packages.CustomRenderers.Components;
using SaR4Hololens2.Scenes.TestingFeatureMinimap.Scripts;

namespace SaR4Hololens2.Scenes.BuildingExplorationV2.Scripts.Components
{
    public class PathDrawer : MonoBehaviour
    {
        // ===== GUI ===== //

        [Header("Main settings")]
        [Tooltip("Reference to the Minimap Structure tool")]
        public MinimapStructure MinimapReference = null;
        [Tooltip("Managed GameObject where to draw paths inside")]
        public GameObject RootObject = null;



        // ===== PRIVATE ===== //

        // init script
        private bool init = false;
        // marker builder script
        private LittleMarkerGrabbableBuilder builder = null;
        // max ID 
        private int id = 0;
        // IDs set
        private HashSet<string> tags = new HashSet<string>();
        // IDs of the arch handled by the class
        private HashSet<string> linkKeys = new HashSet<string>();



        // ===== UNITY CALLBACKS ===== //

        private void Start()
        {
            if(MinimapReference == null)
            {
                Debug.LogError("MinimapReference is missing!");
                return;
            }
            builder = gameObject.AddComponent<LittleMarkerGrabbableBuilder>();
            RootObject = (RootObject == null ? gameObject : RootObject);

            init = true;
        }

        private void Update()
        {

        }



        // ===== CREATE MARKERS ===== //

        public bool CreatePoint(PositionDatabaseWaypoint pos, bool cleanBefore = false, bool canModifyPos = false)
        {
            if (!init) return false;
            if (RootObject == null) return false;
            if (pos == null) return false;
            if (pos.ObjectCenterReference != null && !tags.Contains(pos.ObjectCenterReference.name)) return false;

            if (cleanBefore)
                this.Clean();

            _ = StartCoroutine(ORCOR_CreatePoint(pos, canModifyPos: canModifyPos));

            return true;
        }

        private IEnumerator ORCOR_CreatePoint(PositionDatabaseWaypoint pos, bool canModifyPos = false)
        {
            if (pos.ObjectCenterReference == null)
                yield return InstanciateMarker(pos, canModifyPos: canModifyPos);

            if(canModifyPos)
            {
                PositionDatabaseWaypointHandle h = pos.ObjectCenterReference.GetComponent<PositionDatabaseWaypointHandle>();
                h.SetDbChangable(true);
            }
        }

        public bool CreatePathSegment(PositionDatabaseWaypoint startPos, PositionDatabaseWaypoint endPos, bool cleanBefore = false, bool canModifyPos = false)
        {
            if (!init) return false;
            if (RootObject == null) return false;
            if (startPos == null || endPos == null)
                return false;
            PositionDatabasePath link = startPos.GetPathTo(endPos);
            if (link == null)
                return false;
            if (startPos.ObjectCenterReference != null && !tags.Contains(startPos.ObjectCenterReference.name)) return false;
            if (endPos.ObjectCenterReference != null && !tags.Contains(endPos.ObjectCenterReference.name)) return false;

            if (cleanBefore)
                this.Clean();

            linkKeys.Add(link.PathKey);
            _ = StartCoroutine(ORCOR_CreatePathSegment(startPos, endPos, link, canModifyPos: canModifyPos));

            return true;
        }

        private IEnumerator ORCOR_CreatePathSegment(PositionDatabaseWaypoint startPos, PositionDatabaseWaypoint endPos, PositionDatabasePath link, bool canModifyPos = false)
        {
            bool startInstance = false;
            if (startPos.ObjectCenterReference == null)
            {
                yield return InstanciateMarker(startPos, canModifyPos: canModifyPos);
                startInstance = true;
            }

            bool endInstance = false;
            if (endPos.ObjectCenterReference == null)
            {
                yield return InstanciateMarker(endPos, canModifyPos: canModifyPos);
                endInstance = true;
            }

            link.Renderer = startPos.ObjectCenterReference.AddComponent<FlexibleLineRenderer>();
            ((FlexibleLineRenderer)link.Renderer).Object1 = startPos.ObjectCenterReference;
            ((FlexibleLineRenderer)link.Renderer).Object2 = endPos.ObjectCenterReference;

            if (canModifyPos)
            {
                if (startInstance)
                {
                    PositionDatabaseWaypointHandle hStart = startPos.ObjectCenterReference.GetComponent<PositionDatabaseWaypointHandle>();
                    hStart.SetDbChangable(true);
                }

                if (endInstance)
                {
                    PositionDatabaseWaypointHandle hEnd = endPos.ObjectCenterReference.GetComponent<PositionDatabaseWaypointHandle>();
                    hEnd.SetDbChangable(true);
                }
            }
        }

        private IEnumerator InstanciateMarker(PositionDatabaseWaypoint pos, bool canModifyPos = false)
        {
            string tag = "NODE_" + id.ToString("0000");
            ++id;

            yield return builder.BSCOR_Build(position: pos.AreaCenter);
            
            GameObject go = builder.GetLastSpawned().gameObject;
            go.name = tag;
            tags.Add(tag);
            go.transform.SetParent(RootObject.transform);
            PositionDatabaseWaypointHandle h = go.AddComponent<PositionDatabaseWaypointHandle>();
            h.DatabasePosition = pos;

            MinimapReference.TrackGameObject(go, tag, ignoreOrderCriterion: true);

            h.SetDbChangable(canModifyPos, handleReference: false);
            h.DatabasePosition.ObjectCenterReference = h.gameObject;
            if (canModifyPos)
                h.DatabasePosition.ObjectCenterReference = go;
        }



        // ===== DELETE MARKERS ===== //

        public void Clean()
        {
            if (!init) return;

            StopAllCoroutines();
            MinimapReference.UntrackAll(destroy: true);
            tags.Clear();
            linkKeys.Clear();
        }



        // ===== DATA CHECKS ===== //

        public bool IsHandledByDrawerWaypoint(string tag)
        {
            return tags.Contains(tag);
        }

        public bool IsHandledByDrawerPath(string tag)
        {
            return linkKeys.Contains(tag);
        }
    }
}
