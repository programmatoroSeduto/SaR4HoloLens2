using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Packages.VisualItems.LittleMarker.Components;
using SaR4Hololens2.Scenes.BuildingExplorationV2.Scripts.Utils;
using Packages.CustomRenderers.Components;

namespace SaR4Hololens2.Scenes.BuildingExplorationV2.Scripts.Components
{
    public class MarkerSpawner : MonoBehaviour
    {
        // ===== GUI ===== //

        [Header("Test Module Settings")]
        [Tooltip("Reference to the DB to test")]
        public PositionsDatabase DatabaseReference = null;



        // ===== PRIVATE ===== //

        // init script
        private bool init = false;
        // the previously received id
        private int prevID = -1;
        // marker spawner
        private LittleMarkerGrabbableBuilder spawner;



        // ===== UNITY CALLBACKS ===== //

        private void Start()
        {
            if (DatabaseReference == null)
            {
                Debug.LogError("DatabaseReference is null!");
                return;
            }

            spawner = gameObject.AddComponent<LittleMarkerGrabbableBuilder>();

            init = true;
        }



        // ===== EVENT METHODS ===== //

        public void EVENT_OnZoneCreated()
        {
            if (!init) return;

            StartCoroutine(BSCOR_SpawnMarker());
        }

        private IEnumerator BSCOR_SpawnMarker()
        {
            yield return null;

            PositionDatabaseWaypoint pos = DatabaseReference.DataZoneCreated;
            spawner.InitName = "ID" + pos.PositionID.ToString("000");
            spawner.InitPosition = pos.AreaCenter;
            spawner.Build();

            yield return new WaitForEndOfFrame();

            pos.ObjectCenterReference = spawner.GetLastSpawned().gameObject;
            PositionDatabasePath link = pos.GetPathTo(DatabaseReference.CurrentZone);
            if(link != null && !link.HasRenderer)
            {
                FlexibleLineRenderer lr = pos.ObjectCenterReference.AddComponent<FlexibleLineRenderer>();
                link.Renderer = lr;
                lr.Object1 = link.wp1.ObjectCenterReference;
                lr.Object2 = link.wp2.ObjectCenterReference;
            }
        }
    }
}
