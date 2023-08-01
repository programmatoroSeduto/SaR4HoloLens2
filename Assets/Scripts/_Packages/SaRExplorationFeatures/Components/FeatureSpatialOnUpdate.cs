using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Packages.PositionDatabase.Components;
using Packages.PositionDatabase.Utils;
using Packages.StorageManager.Components;

namespace Packages.SarExplorationFeatures.Components
{
    public class FeatureSpatialOnUpdate : FeatureBase
    {
        // ===== FEATURE ON UPDATE ===== //

        public void OnZoneChanged()
        {
            if (!IsRunning) return;

            PositionDatabaseWaypoint wp = DbReference.CurrentZone;
            
            if (!DrawerReference.IsHandledByDrawerWaypoint(DrawerReference.TagOf(wp)))
                DrawerReference.CreatePoint(wp, canModifyPos: false);

            foreach (PositionDatabasePath link in wp.Paths)
            {
                if (DrawerReference.IsHandledByDrawerWaypoint(DrawerReference.TagOf(wp)) && !DrawerReference.IsHandledByDrawerPath(DrawerReference.TagOf(link)))
                    DrawerReference.CreatePath(wp, link.Next(wp));
            }
        }
    }
}
