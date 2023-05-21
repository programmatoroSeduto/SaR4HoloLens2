using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Packages.ExplorationServices.Utils;

namespace Packages.ExplorationServices.Components
{
    public class FeatureVisualizeNearMarkers : MonoBehaviour
    {
        public PositionsDatabase DatabaseReference = null;
        public VisualAllocationHandle VisualAllocator = null;
        public float UpdatePeriod = 1.0f;
        public bool LaunchOnStart = false;
        // public int MappingDepth = 1;
        public bool DebugMode = false;

        private bool active = false;
        private bool visualizing = false;
        private PositionItem currentZone = null;
        private List<PositionItem> NearPositions = new List<PositionItem>();
        private Coroutine COR_UpdateVisualization;

        private void Start()
        {
            if(DatabaseReference == null)
            {
                Debug.LogWarning("ERROR: missing PositionsDatabase reference");
                return;
            }
            if(VisualAllocator == null)
            {
                Debug.LogWarning("ERROR: missing VisualAllocator reference!");
                return;
            }

            active = true;
            if (LaunchOnStart) EVENT_VisualizeNearMarkersTurnOn();
        }

        private IEnumerator ORCOR_UpdateVisualization()
        {
            do
            {
                if (IsNewZoneFromDb())
                {
                    ExploreZone();
                    UpdateVisualization();

                    Debug.Log($"Using zone no.{currentZone.ItemIndex}");
                }

                yield return new WaitForSecondsRealtime(UpdatePeriod);
            }
            while (visualizing);
        }

        public void EVENT_VisualizeNearMarkersTurnOn()
        {
            if (!active || visualizing) return;
            visualizing = true;
            
            COR_UpdateVisualization = StartCoroutine(ORCOR_UpdateVisualization());
        }

        public void EVENT_VisualizeNearMarkersTurnOff()
        {
            if (!active || !visualizing) return;
            visualizing = false;

            StopCoroutine(COR_UpdateVisualization);
        }

        public void EVENT_VisualizeNearMarkersSwitch()
        {
            if (!active) return;

            if (!visualizing)
                EVENT_VisualizeNearMarkersTurnOn();
            else
                EVENT_VisualizeNearMarkersTurnOff();
        }

        private bool IsNewZoneFromDb()
        {
            if (!active || !visualizing) return false;

            PositionItem newZone = DatabaseReference.GetCurrentZone();
            if (currentZone == null && newZone == null) return false;
            if (currentZone == newZone) return false;

            currentZone = newZone;
            return true;
        }

        private void ExploreZone()
        {
            if (!active || !visualizing) return;

            NearPositions.Clear();
            NearPositions.Add(currentZone);
            NearPositions.AddRange(currentZone.GetNearPositions());
        }

        private void UpdateVisualization()
        {
            if (!active || !visualizing) return;

            VisualAllocator.DeallocateAll();
            foreach (PositionItem pos in NearPositions)
                VisualAllocator.Allocate(pos.ItemIndex, $"marker no{pos.ItemIndex}", pos.uP, Quaternion.identity);
        }
    }
}
