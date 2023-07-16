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

        public string TagOf(PositionDatabaseWaypoint wp)
        {
            return wp.PositionID.ToString("0000");
        }

        public string TagOf(PositionDatabasePath link)
        {
            return link.PathKey;
        }

        public string CreatePoint(PositionDatabaseWaypoint pos, bool canModifyPos = false, string tag = "")
        {
            /*
            Debug.Log($"CreatePoint(posID:{pos.PositionID}, canModifyPos:{canModifyPos}, tag:{tag}) -- ");
            */

            if (!init) return null;
            if (RootObject == null) return null;
            if (pos == null) return null;

            if (tag == "") tag = TagOf(pos);

            if (tags.Contains(tag))
            {
                Debug.Log($"CreatePoint(posID:{pos.PositionID}, canModifyPos:{canModifyPos}, tag:{tag}) -- point already instanced; returning tag {tag}");
                return tag;
            }

            Debug.Log($"CreatePoint(posID:{pos.PositionID}, canModifyPos:{canModifyPos}, tag:{tag}) -- point not found: making new instance");
            InstanciateMarker(tag, pos, canModifyPos: canModifyPos);

            return tag;
        }

        public bool CreatePath(PositionDatabaseWaypoint startPos, PositionDatabaseWaypoint endPos, string tag1 = "", string tag2 = "", string tagLink = "")
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

            linkKeys.Add((tagLink == "" ? TagOf(link) : tagLink));
            return true;
        }

        private void InstanciateMarker(string tag, PositionDatabaseWaypoint pos, bool canModifyPos = false)
        {
            /*
            Debug.Log($"InstanciateMarker(tag:{tag}, pos:{pos.PositionID}, canModifyPos:{canModifyPos}) -- ");
            */

            Debug.Log($"InstanciateMarker(tag:{tag}, pos:{pos.PositionID}, canModifyPos:{canModifyPos}) -- START");
            Debug.Log($"InstanciateMarker(tag:{tag}, pos:{pos.PositionID}, canModifyPos:{canModifyPos}) -- (before instance) tags.Contains(tag:{tag}) ? {tags.Contains(tag)}");

            builder.SpawnUnderObject = RootObject;
            builder.InitName = tag;
            builder.Build(position: pos.AreaCenter);
            
            GameObject go = builder.GetLastSpawned().gameObject;
            MinimapReference.TrackGameObject(go, tag, ignoreOrderCriterion: true);

            tags.Add(tag);

            PositionDatabaseWaypointHandle h = go.AddComponent<PositionDatabaseWaypointHandle>();
            h.Init();
            h.DatabasePosition = pos;
            h.SetDbChangable(canModifyPos, handleReference: true);

            Debug.Log($"InstanciateMarker(tag:{tag}, pos:{pos.PositionID}, canModifyPos:{canModifyPos}) -- (after instance) tags.Contains(tag:{tag}) ? {tags.Contains(tag)}");
            Debug.Log($"InstanciateMarker(tag:{tag}, pos:{pos.PositionID}, canModifyPos:{canModifyPos}) -- END");
        }



        // ===== DELETE MARKERS ===== //

        public void RemoveMarkerAll(HashSet<string> ExclusionListWps = null)
        {
            /*
            Debug.Log($"RemoveMarkerAll(ExclusionListWps:{ExclusionListWps.Count}) -- ");
            */

            Debug.Log($"RemoveMarkerAll(ExclusionListWps:{ExclusionListWps.Count}) -- START");
            string ss = "";

            if (!init) return;

            ss = "";
            foreach (var tag in tags) ss += tag + ",";
            Debug.Log($"RemoveMarkerAll(ExclusionListWps:{ExclusionListWps.Count}) -- tags before cleanup: {ss}");

            if (ExclusionListWps == null) ExclusionListWps = new HashSet<string>();
            ss = "";
            foreach (var tag in ExclusionListWps) ss += tag + ",";
            Debug.Log($"RemoveMarkerAll(ExclusionListWps:{ExclusionListWps.Count}) -- tags in exclusion list: {ss}");

            StopAllCoroutines();

            /*
            foreach (string tag in tags)
            {
                MinimapReference.TryGetItemGameObject(tag).GetComponent<PositionDatabaseWaypointHandle>().SetDbChangable(false, handleReference: true);
            }
            */
            
            Debug.Log($"RemoveMarkerAll(ExclusionListWps:{ExclusionListWps.Count}) -- found tags to remove: {tags.Count}");
            var enm = tags.GetEnumerator();
            int deleted = 0;
            int skip = 0;
            while (enm.MoveNext())
            {
                string cur = enm.Current;
                Debug.Log($"RemoveMarkerAll(ExclusionListWps:{ExclusionListWps.Count}) -- cur:{cur}");

                if (ExclusionListWps.Contains(cur))
                {
                    Debug.Log($"RemoveMarkerAll(ExclusionListWps:{ExclusionListWps.Count}) -- object is in exclusion list; skip");
                    skip++;
                    continue;
                }

                Debug.Log($"RemoveMarkerAll(ExclusionListWps:{ExclusionListWps.Count}) -- removing marker with ID:{cur}");
                RemoveMarker(cur, removeWpRefFromHash: false);
                tags.Remove(cur);
                enm = tags.GetEnumerator();
                deleted++;
            }

            ss = "";
            foreach (var tag in tags) ss += tag + ",";
            Debug.Log($"RemoveMarkerAll(ExclusionListWps:{ExclusionListWps.Count}) -- tags after cleanup: {ss}");

            Debug.Log($"RemoveMarkerAll(ExclusionListWps:{ExclusionListWps.Count}) -- END with deleted:{deleted} skip:{skip}");
        }

        public bool RemoveMarker(string tag, bool removeWpRefFromHash = true)
        {
            /*
            Debug.Log($"RemoveMarker(tag:{tag}) -- ");
            */

            Debug.Log($"RemoveMarker(tag:{tag}) -- START");

            if (!init) return false;
            if (!tags.Contains(tag)) return false;

            GameObject toDel = MinimapReference.TryGetItemGameObject(tag);
            PositionDatabaseWaypointHandle hDel = toDel.GetComponent<PositionDatabaseWaypointHandle>();
            Debug.Log($"RemoveMarker(tag:{tag}) -- toDel with posID:{hDel.DatabasePosition.PositionID}");

            hDel.SetDbChangable(false, handleReference: true);

            Debug.Log($"RemoveMarker(tag:{tag}) -- found links:{hDel.DatabasePosition.Paths.Count}");
            foreach (PositionDatabasePath link in hDel.DatabasePosition.Paths)
            {
                string pathKey = link.PathKey;
                if(!linkKeys.Contains(pathKey))
                {
                    Debug.Log($"RemoveMarker(tag:{tag}) -- link with key:{pathKey} not instanced; skip");
                    continue;
                }

                Debug.Log($"RemoveMarker(tag:{tag}) -- removing link with key:{pathKey}");

                FlexibleLineRenderer line = (FlexibleLineRenderer) link.Renderer;
                Debug.Log($"RemoveMarker(tag:{tag}) -- line to Detroy is owned by go:{line.gameObject.name}");
                MonoBehaviour.DestroyImmediate(line, true);

                linkKeys.Remove(pathKey);
            }

            MinimapReference.UntrackGameObject(tag, destroy: true);
            if(removeWpRefFromHash) tags.Remove(tag);

            Debug.Log($"RemoveMarker(tag:{tag}) -- END returning true");
            return true;
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
