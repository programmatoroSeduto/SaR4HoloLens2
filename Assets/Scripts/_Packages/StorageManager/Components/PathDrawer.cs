using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Project.Scripts.Components;
using Project.Scripts.Utils;

using Packages.ARMarker.Components;
using Packages.PositionDatabase.Components;
using Packages.PositionDatabase.Utils;
using Packages.CustomRenderers.Components;

namespace Packages.StorageManager.Components
{
    public class PathDrawer : MonoBehaviour
    {
        // ===== GUI ===== //

        [Header("Main settings")]
        [Tooltip("Reference to the Minimap Structure tool")]
        public MinimapStructure MinimapReference = null;
        [Tooltip("(dynamic) Managed GameObject where to draw paths inside")]
        public GameObject RootObject = null;
        [Tooltip("Markers Relative Height compard to the original height")]
        public float MarkerHeight = 0.0f;



        // ===== PRIVATE ===== //

        // init script
        private bool init = false;
        // marker builder script
        private ARMarkerBuilder builder = null;
        // max ID 
        private int id = 0;
        // IDs set
        private HashSet<string> tags = new HashSet<string>();
        // IDs of the arch handled by the class
        private HashSet<string> linkKeys = new HashSet<string>();



        // ===== UNITY CALLBACKS ===== //

        private void Start()
        {
            if (MinimapReference == null)
            {
                Debug.LogError("MinimapReference is missing!");
                return;
            }
            builder = gameObject.AddComponent<ARMarkerBuilder>();
            RootObject = (RootObject == null ? gameObject : RootObject);
            builder.SpawnUnderObject = RootObject;

            init = true;
        }



        // ===== GO TAGGING SYSTEM ===== //

        public string TagOf(PositionDatabaseWaypoint wp)
        {
            if(wp == null)
            {
                StaticLogger.Warn(this, "cannot get tag from a null waypoint; the 'wp' parameter is null", logLayer: 1);
                return "";
            }
            return wp.KeyStable;
        }

        public string TagOf(PositionDatabasePath link)
        {
            if (link == null)
            {
                StaticLogger.Warn(this, "cannot get tag from a null link; the 'link' parameter is null", logLayer: 1);
                return "";
            }
            return link.KeyStable;
        }



        // ===== FEATURE CREATE MARKERS AND PATHS ===== //

        public string CreatePoint(PositionDatabaseWaypoint pos, bool canModifyPos = false, string tag = "")
        {
            if (!init) return null;
            if (RootObject == null) return null;
            if (pos == null) return null;

            if (tag == "") tag = TagOf(pos);

            if (tags.Contains(tag))
            {
                return tag;
            }

            InstanciateMarker(tag, pos, canModifyPos: canModifyPos);

            return tag;
        }

        public bool CreatePath(PositionDatabaseWaypoint startPos, PositionDatabaseWaypoint endPos, string tag1 = "", string tag2 = "")
        {
            if (!init) return false;
            if (RootObject == null) return false;
            if (startPos == null || endPos == null)
                return false;

            if (tag1 == "") tag1 = TagOf(startPos);
            if (tag2 == "") tag2 = TagOf(endPos);
            if (!tags.Contains(tag1) || !tags.Contains(tag2)) return false;
            PositionDatabasePath link = startPos.GetPathTo(endPos);
            if (link == null) return false;
            if (linkKeys.Contains(TagOf(link))) return false;

            GameObject goStart = MinimapReference.TryGetItemGameObject(tag1);
            GameObject goEnd = MinimapReference.TryGetItemGameObject(tag2);

            link.Renderer = goStart.AddComponent<FlexibleLineRenderer>();
            ((FlexibleLineRenderer)link.Renderer).Object1 = goStart;
            ((FlexibleLineRenderer)link.Renderer).Object2 = goEnd;

            linkKeys.Add(TagOf(link));
            return true;
        }

        private void InstanciateMarker(string tag, PositionDatabaseWaypoint pos, bool canModifyPos = false)
        {
            builder.SpawnUnderObject = RootObject;
            builder.InitName = tag;
            builder.Build(position: pos.AreaCenter + MarkerHeight * Vector3.up);

            GameObject go = builder.LastSpawnedGameObject;
            MinimapReference.TrackGameObject(go, tag, ignoreOrderCriterion: true);

            tags.Add(tag);

            PositionDatabaseWaypointHandle h = go.AddComponent<PositionDatabaseWaypointHandle>();
            h.DatabasePosition = pos;
            h.SetDbChangable(canModifyPos, handleReference: true);
        }



        // ===== DELETE MARKERS ===== //

        public void RemoveMarkerAll(HashSet<string> ExclusionListWps = null)
        {
            if (!init) return;
            if (ExclusionListWps == null) ExclusionListWps = new HashSet<string>();

            StopAllCoroutines();
            
            var enm = tags.GetEnumerator();
            while (enm.MoveNext())
            {
                string cur = enm.Current;
                if (ExclusionListWps.Contains(cur))
                    continue;

                RemoveMarker(cur, removeWpRefFromHash: false);
                tags.Remove(cur);
                enm = tags.GetEnumerator();
            }
        }

        public bool RemoveMarker(string tag, bool removeWpRefFromHash = true)
        {
            if (!init) return false;
            if (!tags.Contains(tag)) return false;

            GameObject toDel = MinimapReference.TryGetItemGameObject(tag);
            PositionDatabaseWaypointHandle hDel = toDel.GetComponent<PositionDatabaseWaypointHandle>();

            hDel.SetDbChangable(false, handleReference: true);

            foreach (PositionDatabasePath link in hDel.DatabasePosition.Paths)
            {
                string pathKey = TagOf(link);
                if (!linkKeys.Contains(pathKey)) continue;

                FlexibleLineRenderer line = (FlexibleLineRenderer)link.Renderer;
                
                MonoBehaviour.DestroyImmediate(line, true);
                linkKeys.Remove(pathKey);
            }

            MinimapReference.UntrackGameObject(tag, destroy: true);
            if (removeWpRefFromHash) tags.Remove(tag);

            return true;
        }



        // ===== GET MARKERS ===== //

        public PositionDatabaseWaypointHandle GetHandleOfWaypoint(string tag)
        {
            if (!init) return null;
            if (!tags.Contains(tag)) return null;

            return MinimapReference.TryGetItemGameObject(tag).GetComponent<PositionDatabaseWaypointHandle>();
        }

        public GameObject GetWaypointGameObject(string tag)
        {
            if (!init) return null;
            if (!tags.Contains(tag)) return null;

            return MinimapReference.TryGetItemGameObject(tag);
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
