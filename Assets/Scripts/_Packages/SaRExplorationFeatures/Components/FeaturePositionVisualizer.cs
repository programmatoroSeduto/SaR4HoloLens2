using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Packages.ARMarker.Components;
using Packages.PositionDatabase.Components;
using Packages.PositionDatabase.Utils;
using Packages.StorageManager.Components;
using Packages.CustomRenderers.Components;

namespace Packages.SarExplorationFeatures.Components
{
    public class FeaturePositionVisualizer : FeatureBase
    {
        // ===== PUBLIC ===== //

        [HideInInspector]
        [Tooltip("Reference to the material used for visualizing the current position")]
        public Material CursorMaterial = null;

        public override bool IsRunning
        {
            get
            {
                return isRunning;
            }

            set
            {
                isRunning = value;
                this.enabled = value;

                if(prevWp != null)
                {
                    GameObject wpPrevObj = DrawerReference.GetWaypointGameObject(DrawerReference.TagOf(prevWp));
                    if (wpPrevObj == null)
                    {
                        prevWp = null; 
                        return;
                    }
                    if (!swichMarkerStatus(wpPrevObj, prevWp, false)) return;

                    prevWp = null;
                }
            }
        }



        // ===== PRIVATE ===== //

        // previouly registered position
        private PositionDatabaseWaypoint prevWp = null;
        // reference to the base material
        private Material baseMaterial = null;



        // ===== FEATURE ENABLE CURRENT POSITION ===== //

        public void onChangeCallback()
        {
            if (DrawerReference == null || DbReference == null || CursorMaterial == null) return;
            PositionDatabaseWaypoint wp = DbReference.CurrentZone;

            if (prevWp != null && prevWp.PositionID == wp.PositionID)
                return;

            if (prevWp != null)
            {
                GameObject wpPrevObj = DrawerReference.GetWaypointGameObject(DrawerReference.TagOf(prevWp));
                if (wpPrevObj == null) return;
                if (!swichMarkerStatus(wpPrevObj, prevWp, false)) return;

                prevWp = null;
            }

            string wpTag = DrawerReference.TagOf(wp);
            if (!DrawerReference.IsHandledByDrawerWaypoint(wpTag)) return;

            GameObject wpObj = DrawerReference.GetWaypointGameObject(wpTag);
            if (!swichMarkerStatus(wpObj, wp, true)) return;

            prevWp = wp;
        }

        private bool swichMarkerStatus(GameObject wp, PositionDatabaseWaypoint wph, bool opt)
        {
            ARMarkerHandle h = wp.GetComponent<ARMarkerHandle>();
            if(h == null) return false;

            if(opt)
            {
                h.SecondMaterial = CursorMaterial;
                h.SwitchMaterial(useFirst: false);
            }
            else
            {
                h.SwitchMaterial(useFirst: true);
            }

            foreach (PositionDatabasePath link in wph.Paths)
            {
                if (!DrawerReference.IsHandledByDrawerPath(DrawerReference.TagOf(link)))
                {
                    continue;
                }

                if (((FlexibleLineRenderer)link.Renderer).TrySetupFlexibleLineRenderer())
                    ((FlexibleLineRenderer)link.Renderer).SetLineColor((opt ? Color.red : Color.green));
            }

            return true;
        }

    }
}

