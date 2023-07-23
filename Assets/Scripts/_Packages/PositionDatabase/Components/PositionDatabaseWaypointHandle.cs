using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Input;

using Packages.PositionDatabase.Utils;



namespace Packages.PositionDatabase.Components
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
        private ObjectManipulator objectManipulator = null;
        private NearInteractionGrabbable nearInteraction = null;



        // ===== UNITY CALLBACKS ===== //

        private void Start()
        {
            Init();
        }

        public void Init()
        {
            objectManipulator = gameObject.GetComponent<ObjectManipulator>();
            nearInteraction = gameObject.GetComponent<NearInteractionGrabbable>();
        }

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
                DatabasePosition.ObjectCenterReference = (opt ? this.gameObject : null);

            CanModifyDbPosition = opt;
            SetManipulation(opt);
            return true;
        }

        public bool SetManipulation(bool opt = true)
        {
            if (objectManipulator == null || nearInteraction == null)
            {
                Debug.LogError("SetManipulation returned false");
                return false;
            }

            objectManipulator.enabled = opt;
            nearInteraction.enabled = opt;

            return true;
        }
    }
}