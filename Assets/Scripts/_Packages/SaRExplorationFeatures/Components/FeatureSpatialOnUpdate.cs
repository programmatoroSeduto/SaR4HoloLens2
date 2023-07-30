using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Packages.PositionDatabase.Components;
using Packages.PositionDatabase.Utils;
using Packages.StorageManager.Components;

namespace Packages.SarExplorationFeatures.Components
{
    public class FeatureSpatialOnUpdate : MonoBehaviour
    {
        // ===== PUBLIC ===== //
        
        [HideInInspector] // reference to the database
        public PositionsDatabase DbReference = null;
        [HideInInspector] // reference to the path drawer
        public PathDrawer DrawerReference = null;

        public bool IsRunning
        {
            get
            {
                return isRunning;
            }

            set
            {
                isRunning = value;
                this.enabled = value;

                if (!value)
                {
                    DrawerReference.RemoveMarkerAll();
                    isFirst = true;
                }
            }
        }
        private bool isRunning = false;



        // ===== PRIVATE ===== //

        // either the position is the first record or not
        private bool isFirst = true;



        // ===== FEATURE ON UPDATE ===== //

        public void OnZoneChanged()
        {
            if (!IsRunning) return;

            PositionDatabaseWaypoint wp = DbReference.CurrentZone;

            if (isFirst)
            {
                DrawerReference.RemoveMarkerAll();
                DrawerReference.CreatePoint(wp, tag: DrawerReference.TagOf(wp), canModifyPos: false);
                isFirst = false;
            }
            else
            {
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
}
