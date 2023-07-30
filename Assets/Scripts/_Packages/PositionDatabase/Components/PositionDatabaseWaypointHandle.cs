using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Input;

using Packages.PositionDatabase.Utils;
using Packages.ARMarker.Components;



namespace Packages.PositionDatabase.Components
{
    public class PositionDatabaseWaypointHandle : MonoBehaviour
    {
        // ===== GUI ===== //
        [Header("Main settings")]
        [Tooltip("Represented position")]
        public PositionDatabaseWaypoint DatabasePosition = null;
        [Tooltip("The script of the marker")]
        public ARMarkerHandle MarkerHandle = null;



        // ===== PUBLIC ===== //

        public bool CanChangeDB
        {
            get => CanModifyDbPosition;
        }



        // ===== PRIVATE ===== //

        // either the component is allowed to change the position of the row in the db, or not
        private bool CanModifyDbPosition = false;



        // ===== UNITY CALLBACKS ===== //

        private void Start()
        {
            MarkerHandle = gameObject.GetComponent<ARMarkerHandle>();
        }

        private void OnDestroy()
        {
            if (DatabasePosition == null) return;

            DatabasePosition.TurnOffVisualization();
            DatabasePosition.ObjectCenterReference = null;
        }



        // ===== FEATURE SET CHANGABLE DB RECORD ===== //
        
        public bool SetDbChangable(bool opt = false, bool handleReference = false)
        {
            if (DatabasePosition == null) return false;

            DatabasePosition.CanUpdate = opt;
            if (handleReference)
                DatabasePosition.ObjectCenterReference = (opt ? this.gameObject : null);

            CanModifyDbPosition = opt;
            SetManipulation(opt);
            return true;
        }

        public bool SetManipulation(bool opt = true)
        {
            if(MarkerHandle == null)
            {
                MarkerHandle = gameObject.GetComponent<ARMarkerHandle>();
                if (MarkerHandle == null)
                    return false;
            }

            MarkerHandle.IsManipulable = opt;
            return true;
        }
    }
}