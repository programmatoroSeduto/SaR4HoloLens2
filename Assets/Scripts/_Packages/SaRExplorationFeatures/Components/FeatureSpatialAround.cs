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
        [HideInInspector] // Max Drawable radius
        public float DrawableRadius = 10.0f;
        [HideInInspector] // GameObject where to draw paths inside
        public GameObject RootObject = null;



        // ===== FEATURE NEIGHBORHOOD EXPLORATION ===== //

        public void OnZoneChanged()
        {
            PositionDatabaseWaypoint wp = DbReference.CurrentZone;

            HashSet<string> instances = drawNeighborhood(wp, remainingDistance: DrawableRadius);
            DrawerReference.RemoveMarkerAll(ExclusionListWps: instances);
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
