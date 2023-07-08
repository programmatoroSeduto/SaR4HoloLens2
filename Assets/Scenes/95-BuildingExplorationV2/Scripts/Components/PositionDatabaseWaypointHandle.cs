using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Packages.VisualItems.LittleMarker.Components;
using SaR4Hololens2.Scenes.BuildingExplorationV2.Scripts.Utils;
using Packages.CustomRenderers.Components;
using SaR4Hololens2.Scenes.TestingFeatureMinimap.Scripts;

namespace SaR4Hololens2.Scenes.BuildingExplorationV2.Scripts.Components
{
    public class PositionDatabaseWaypointHandle : MonoBehaviour
    {
        // ===== GUI/PUBLIC ===== //
        [Header("Main settings")]
        [Tooltip("Represented position")]
        public PositionDatabaseWaypoint DatabasePosition = null;
        [Tooltip("If the position can be altered or not")]
        public bool CanModifyDbPosition = false;



        // ===== UNITY CALLBACKS ===== //

        private void OnDestroy()
        {
            if (DatabasePosition == null) return;

            DatabasePosition.TurnOffVisualization();
            DatabasePosition.ObjectCenterReference = null;
        }
    }
}
