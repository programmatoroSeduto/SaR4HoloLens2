using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Packages.PositionDatabase.Components;



namespace Packages.PositionDatabase.ModuleTesting
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

        [Header("DEBUG ZONE")]
        [Tooltip("Current zone")]
        public int debug_currentZoneID = -1;
        [Tooltip("Hit counter (ZONE CHANGED)")]
        public int debug_hit = 0;
        [Tooltip("Miss counter (ZONE CREATED)")]
        public int debug_miss = 0;



        // ===== PRIVATE ===== //

        // init script
        private bool init = false;
        // the previously received id
        private int prevID = -1;



        // ===== UNITY CALLBACKS ===== //

        private void Start()
        {
            if (DatabaseReference == null)
            {
                Debug.LogError("DatabaseReference is null!");
                return;
            }

            init = true;
        }

        private void Update()
        {
            debug_currentZoneID = prevID;
        }



        // ===== EVENT METHODS ===== //

        public void EVENT_OnZoneCreated()
        {
            if (!init || !ManageZoneCreated) return;

            int id = DatabaseReference.DataZoneCreated.PositionID;
            Vector3 p = DatabaseReference.DataZoneCreated.AreaCenter;
            // Debug.Log($"ZONE CREATED: ID:{id} (x:{p.x}, y:{p.y}, z:{p.z})");

            ++debug_miss;
            prevID = id;
        }

        public void EVENT_OnZoneChanged()
        {
            if (!init || !ManageZoneChanged) return;

            int id = DatabaseReference.CurrentZone.PositionID;
            Vector3 p = DatabaseReference.CurrentZone.AreaCenter;
            // Debug.Log($"ZONE CHANGED: ID:{id} (x:{p.x}, y:{p.y}, z:{p.z})");

            if (id != prevID) ++debug_hit;
            prevID = id;
        }
    }

}