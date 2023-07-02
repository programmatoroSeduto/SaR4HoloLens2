using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Packages.ExplorationServices.Utils;
using Packages.CustomRenderers.Components;

namespace Packages.ExplorationServices.Components
{
    public class TestFeatureVisualizeNearMarkers : MonoBehaviour
    {
        public TestPositionsDatabase DatabaseReference = null;
        public TestVisualAllocationHandle VisualAllocator = null;
        public float UpdatePeriod = 1.0f;
        public bool LaunchOnStart = false;
        public int MappingDepth = 1;
        public bool DebugMode = false;

        private bool active = false;
        private bool visualizing = false;
        private PositionItem currentZone = null;
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
                    UpdateVisualization();

                    if(DebugMode)
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

        private void UpdateVisualization()
        {
            if (!active || !visualizing) return;

            VisualAllocator.DeallocateAll();
            BuildLocalMap(currentZone, MappingDepth);
        }

        private GameObject BuildLocalMap(PositionItem point, int depthCounter, bool firstCall = true, PositionItem startFrom = null)
        {
            GameObject go = VisualAllocator.Allocate(point.ItemIndex, $"marker no{point.ItemIndex}", point.uP, Quaternion.identity);
            
            if (depthCounter > 0)
            {
                FlexibleStarOfLinesRenderer star = go.GetComponent<FlexibleStarOfLinesRenderer>();
                if (star == null)
                {
                    star = go.AddComponent<FlexibleStarOfLinesRenderer>();
                    star.Center = go;
                }
                else
                    star.Vertices.Clear();

                foreach (PositionItem p in point.GetNearPositions())
                {
                    if (p == startFrom) continue;
                    GameObject gop = BuildLocalMap(p, depthCounter - 1, false, point);
                    star.Vertices.Add(gop);
                }

                if (firstCall) 
                    star.LineColor = Color.red;
                else 
                    star.LineColor = Color.green;
            }

            return go;
        }
    }
}
