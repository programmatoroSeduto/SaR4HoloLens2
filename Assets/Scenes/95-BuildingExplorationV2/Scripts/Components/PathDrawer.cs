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

        public bool CreatePathSegment(PositionDatabaseWaypoint startPos, PositionDatabaseWaypoint endPos, bool cleanBefore = false)
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
            StartCoroutine(BSCOR_CreateMarker(startPos, endPos, link));

            return true;
        }

        private IEnumerator BSCOR_CreateMarker(PositionDatabaseWaypoint startPos, PositionDatabaseWaypoint endPos, PositionDatabasePath link, bool canModifyPos = false)
        {
            if (startPos.ObjectCenterReference == null)
                yield return InstanciateMarker(startPos, canModifyPos: canModifyPos);

            if (endPos.ObjectCenterReference == null)
                yield return InstanciateMarker(endPos, canModifyPos: canModifyPos);

            link.Renderer = startPos.ObjectCenterReference.AddComponent<FlexibleLineRenderer>();
            ((FlexibleLineRenderer)link.Renderer).Object1 = startPos.ObjectCenterReference;
            ((FlexibleLineRenderer)link.Renderer).Object2 = endPos.ObjectCenterReference;
        }

        private IEnumerator InstanciateMarker(PositionDatabaseWaypoint pos, bool canModifyPos = false)
        {
            string tag = "NODE_" + id.ToString("0000");
            
            yield return builder.BSCOR_Build(position: pos.AreaCenter);
            
            GameObject go = builder.GetLastSpawned().gameObject;
            go.name = tag;
            tags.Add(tag);
            go.transform.SetParent(RootObject.transform);
            PositionDatabaseWaypointHandle h = go.AddComponent<PositionDatabaseWaypointHandle>();
            h.DatabasePosition = pos;

            MinimapReference.TrackGameObject(go, tag, ignoreOrderCriterion: true);

            h.CanModifyDbPosition = canModifyPos;
            if (canModifyPos)
                h.DatabasePosition.ObjectCenterReference = go;

            ++id;
        }



        // ===== DELETE MARKERS ===== //

        public void Clean()
        {
            if (!init) return;

            StopAllCoroutines();
            MinimapReference.UntrackAll(destroy: true);
            tags.Clear();
        }
    }
}
