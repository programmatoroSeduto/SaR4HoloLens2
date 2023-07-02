using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SaR4Hololens2.Scenes.BuildingExplorationV2.Scripts.Utils;
using SaR4Hololens2.Scenes.BuildingExplorationV2.Scripts.Components;

namespace SaR4Hololens2.Scenes.BuildingExplorationV2.Scripts.ModuleTesting
{
    public class TestingPositionDB : MonoBehaviour
    {
        // ===== GUI ===== //

        [Header("Test Module Settings")]
        [Tooltip("Reference to the DB to test")]
        public PositionsDatabase DatabaseReference = null;
        [Tooltip("Manage new positions")]
        public bool ManageZoneCreated = true;
        [Tooltip("Manage position change")]
        public bool ManageZoneChanged = true;



        // ===== PRIVATE ===== //

        // init script
        private bool init = false;



        // ===== UNITY CALLBACKS ===== //

        private void Start()
        {
            if(DatabaseReference == null)
            {
                Debug.LogError("DatabaseReference is null!");
                return;
            }

            init = true;
        }



        // ===== EVENT METHODS ===== //

        public void EVENT_OnZoneCreated()
        {
            if (!init || !ManageZoneCreated) return;

            int id = DatabaseReference.DataZoneCreated.PositionID;
            Vector3 p = DatabaseReference.DataZoneCreated.AreaCenter;
            Debug.Log($"ZONE CREATED: ID:{id} (x:{p.x}, y:{p.y}, z:{p.z})");
        }

        public void EVENT_OnZoneChanged()
        {
            if (!init || !ManageZoneChanged) return;

            int id = DatabaseReference.CurrentZone.PositionID;
            Vector3 p = DatabaseReference.CurrentZone.AreaCenter;
            Debug.Log($"ZONE CHANGED: ID:{id} (x:{p.x}, y:{p.y}, z:{p.z})");
        }
    }

}