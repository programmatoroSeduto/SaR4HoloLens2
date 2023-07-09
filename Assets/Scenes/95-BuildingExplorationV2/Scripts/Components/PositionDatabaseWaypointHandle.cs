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
        // ===== GUI ===== //
        [Header("Main settings")]
        [Tooltip("Represented position")]
        public PositionDatabaseWaypoint DatabasePosition = null;



        // ===== PUBLIC ===== //

        public bool CanChangeDB
        {
            get => CanModifyDbPosition;
        }



        // ===== PRIVATE ===== //

        // either the component is allowed to change the position of the row in the db, or not
        private bool CanModifyDbPosition = false;



        // ===== UNITY CALLBACKS ===== //

        private void OnDestroy()
        {
            if (DatabasePosition == null) return;

            DatabasePosition.TurnOffVisualization();
            DatabasePosition.ObjectCenterReference = null;
        }



        // ===== FEATURE SET CHANGABLE DB RECORD ===== //

        // ritorna se la richiesta è andata a buon fine o no
        public bool SetDbChangable(bool opt = false, bool handleReference = false)
        {
            if (DatabasePosition == null) return false;

            DatabasePosition.CanUpdate = opt;
            if (handleReference)
                DatabasePosition.ObjectCenterReference = ( opt ? this.gameObject : null );

            CanModifyDbPosition = opt;
            return true;
        }
    }
}
