using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Packages.PositionDatabase.Components;
using Packages.PositionDatabase.Utils;
using Packages.StorageManager.Components;

namespace Packages.SarExplorationFeatures.Components
{
    public class FeatureSpatialAround : FeatureBase
    {
        // ===== PUBLIC ===== //
        [HideInInspector]
        [Tooltip("Max Drawable radius")]
        public float DrawableRadius = 10.0f;
        [HideInInspector]
        [Tooltip("GameObject where to draw paths inside")]
        public GameObject RootObject = null;
        [HideInInspector]
        [Tooltip("Reference to the Position Visualizer to run after the update process")]
        public FeaturePositionVisualizer positionVisualizer = null;



        // ===== FEATURE NEIGHBORHOOD EXPLORATION ===== //

        public void OnZoneChanged()
        {
            HashSet<string> instances = new HashSet<string>();
            foreach(PositionDatabaseWaypoint wp in DbReference.GetNearestWaypoints(maxItems: 10, maxDistance: DrawableRadius))
            {
                if (instances.Contains(DrawerReference.TagOf(wp))) 
                    continue;
                instances.UnionWith(drawNeighborhood(wp, remainingDistance: DrawableRadius));
            }
            DrawerReference.RemoveMarkerAll(ExclusionListWps: instances);

            if (positionVisualizer != null)
                positionVisualizer.onChangeCallback();
        }

        private HashSet<string> drawNeighborhood(PositionDatabaseWaypoint wp, PositionDatabaseWaypoint userPos = null, float remainingDistance = float.MaxValue, HashSet<string> instances = null)
        {
            if (instances == null)
            {
                instances = new HashSet<string>();
                userPos = wp;
                string tag = DrawerReference.CreatePoint(wp, canModifyPos: false, tag: DrawerReference.TagOf(wp));
                if (tag == null) return instances; // unexpected...
                instances.Add(tag);
            }

            if (remainingDistance > 0.0f || Vector3.Distance(userPos.AreaCenter, wp.AreaCenter) <= DrawableRadius)
            {
                foreach (PositionDatabasePath link in wp.Paths)
                {
                    PositionDatabaseWaypoint wpNext = link.Next(wp);
                    if (instances.Contains(DrawerReference.TagOf(wpNext)))
                        continue;

                    string tag = DrawerReference.CreatePoint(wpNext, canModifyPos: false, tag: DrawerReference.TagOf(wpNext));
                    if (tag == null) return instances; // unexpected...
                    DrawerReference.CreatePath(wp, wpNext, tag1: DrawerReference.TagOf(wp), tag2: DrawerReference.TagOf(wpNext));
                    
                    instances.Add(tag);
                    instances = drawNeighborhood(wpNext, userPos, (remainingDistance - link.Distance), instances);
                }
            }

            return instances;
        }
    }
}
