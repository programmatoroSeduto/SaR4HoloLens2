using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

using Packages.PositionDatabase.Components;
using Packages.StorageManager.Components;

namespace Packages.SarExplorationFeatures.Components
{
    public class SarExplorationControlUnit : MonoBehaviour
    {
        // ===== GUI ===== //

        [Header("SaR CU Base Settings")]
        [Tooltip("(mandatory) Reference to the Position Database")]
        public PositionsDatabase DbReference = null;
        [Tooltip("Reference to the path drawer; if null, the class creates its own Drawer")]
        public PathDrawer DrawerReference = null;
        [Tooltip("Minimap reference; ; if null, the class creates its own one")]
        public MinimapStructure StructureReference = null;
        [Tooltip("Root object for the drawer; if null, the class creates its own Go")]
        public GameObject MarkersRoot = null;

        [Header("SpatialAround Feature Settings")]
        [Tooltip("(dynamic) Max Drawable radius")]
        public float DrawableRadius = 10.0f;
        [Tooltip("(dynamic) Levels of search depth")]
        public List<float> Intensities = new List<float>{ 1.0f, 2.0f, 5.0f, 10.0f, 25.0f };



        // ===== PUBLIC ===== //

        // it indicates the status of the class
        public enum SarExplorationControlUnitStatus
        {
            NotReady,
            Ready,
            SpatialOnUpdate,
            SpatialAround
        }

        public SarExplorationControlUnitStatus Status
        {
            get => status;
        }



        // ===== PRIVATE ===== //

        // this class has to be initialized before going on
        private bool init = false;
        // internal class status
        private SarExplorationControlUnitStatus status = SarExplorationControlUnitStatus.NotReady;
        // FEATURE SpatialOnUpdate
        private FeatureSpatialOnUpdate spatialOnUpdate = null;
        // FEATURE SpatialAround
        private FeatureSpatialAround spatialAround = null;
        // intensity of the feature SpatialAround
        private int spatialAroundIntensity = 0;



        // ===== UNITY CALLBACKS AND CLASS INIT ===== //

        private void Start()
        {
            if(!SetupClass())
            {
                Debug.LogWarning("[SarExplorationControlUnit] Unable to initialize the class");
                return;
            }
        }

        public bool SetupClass()
        {
            if(init) return true;
            
            if (DbReference == null) return false;
            DbReference.CallOnZoneChanged.Clear();
            DbReference.CallOnZoneCreated.Clear();

            if (StructureReference == null)
            {
                StructureReference = gameObject.AddComponent<MinimapStructure>();
            }

            if (DrawerReference == null)
            {
                DrawerReference = gameObject.AddComponent<PathDrawer>();
                if (MarkersRoot)
                {
                    MarkersRoot = new GameObject(name: "SaRExplorationRoot");
                    MarkersRoot.transform.position = Vector3.zero;
                    MarkersRoot.transform.rotation = Quaternion.identity;
                    MarkersRoot.transform.localScale = Vector3.one;
                }
                DrawerReference.RootObject = MarkersRoot;
                DrawerReference.MinimapReference = StructureReference;
            }

            status = SarExplorationControlUnitStatus.Ready;
            init = true;
            return true;
        }



        // ===== INTERACTIONS AND EVENTS ===== //

        public void VOICE_SpatialOnUpdate(bool clean = false)
        {
            if (!init) return;

            if (!(status == SarExplorationControlUnitStatus.SpatialOnUpdate))
            {
                SWITCH_SpatialOnUpdate();
                ONOFF_SpatialOnUpdate(true);
            }
            else
            {
                if(clean) DrawerReference.RemoveMarkerAll();
                ONOFF_SpatialOnUpdate(false);
            }
        }

        public void VOICE_SpatialAround(bool clean = false)
        {
            if (!init) return;

            if (!(status == SarExplorationControlUnitStatus.SpatialAround))
            {
                SWITCH_SpatialAround();
                ONOFF_SpatialAround(true);
            }
            else
            {
                if (clean) DrawerReference.RemoveMarkerAll();
                ONOFF_SpatialAround(false);
            }
        }

        public void VOICE_SpatialAroundIntensity(bool more = true)
        {
            if (status != SarExplorationControlUnitStatus.SpatialAround) return;

            if (more)
                spatialAroundIntensity = (spatialAroundIntensity + 1 >= Intensities.Count ? Intensities.Count - 1 : spatialAroundIntensity + 1);
            else
                spatialAroundIntensity = (spatialAroundIntensity - 1 < 0 ? 0 : spatialAroundIntensity - 1);
            
            DrawableRadius = Intensities[spatialAroundIntensity];
            spatialAround.DrawableRadius = DrawableRadius;

            spatialAround.OnZoneChanged();
        }



        // ===== FEATURE SPATIAL ON UPDATE ===== //

        private void ONOFF_SpatialOnUpdate(bool opt = true)
        {
            if (!init) return;

            if(opt && spatialOnUpdate == null)
            {
                spatialOnUpdate = gameObject.AddComponent<FeatureSpatialOnUpdate>();
                spatialOnUpdate.DbReference = DbReference;
                spatialOnUpdate.DrawerReference = DrawerReference;
                spatialOnUpdate.IsRunning = false;
            }

            if(opt && !spatialOnUpdate.IsRunning)
            {
                DbReference.CallOnZoneChanged.Clear();
                UnityEvent onChangeCallbackEvent = new UnityEvent();
                onChangeCallbackEvent.AddListener(spatialOnUpdate.OnZoneChanged);
                DbReference.CallOnZoneChanged.Add(onChangeCallbackEvent);

                spatialOnUpdate.IsRunning = true;
                status = SarExplorationControlUnitStatus.SpatialOnUpdate;
                spatialOnUpdate.OnZoneChanged();
            }
            else if(!opt && spatialOnUpdate.IsRunning)
            {
                DbReference.CallOnZoneChanged.Clear();

                spatialOnUpdate.IsRunning = false;
                status = SarExplorationControlUnitStatus.Ready;
            }
        }

        private void SWITCH_SpatialOnUpdate()
        {
            if(status == SarExplorationControlUnitStatus.Ready)
            {
                DrawerReference.RemoveMarkerAll();
            }
            else if(status == SarExplorationControlUnitStatus.SpatialAround)
            {
                ONOFF_SpatialAround(false);
            }
        }



        // ===== FEATURE SPATIAL AROUND ===== //

        private void ONOFF_SpatialAround(bool opt = true)
        {
            if (!init) return;

            if (opt && spatialAround == null)
            {
                spatialAround = gameObject.AddComponent<FeatureSpatialAround>();
                spatialAround.DbReference = DbReference;
                spatialAround.DrawerReference = DrawerReference;
                spatialAround.IsRunning = false;
            }

            if (opt && !spatialAround.IsRunning)
            {
                DbReference.CallOnZoneChanged.Clear();
                UnityEvent onChangeCallbackEvent = new UnityEvent();
                onChangeCallbackEvent.AddListener(spatialAround.OnZoneChanged);
                DbReference.CallOnZoneChanged.Add(onChangeCallbackEvent);

                spatialAround.IsRunning = true;
                spatialAround.DrawableRadius = this.DrawableRadius;
                status = SarExplorationControlUnitStatus.SpatialAround;
                spatialAround.OnZoneChanged();
            }
            else if (!opt && spatialAround.IsRunning)
            {
                DbReference.CallOnZoneChanged.Clear();

                spatialAround.IsRunning = false;
                status = SarExplorationControlUnitStatus.Ready;
            }
        }

        private void SWITCH_SpatialAround()
        {
            if (status == SarExplorationControlUnitStatus.Ready)
            {
                DrawerReference.RemoveMarkerAll();
            }
            else if (status == SarExplorationControlUnitStatus.SpatialOnUpdate)
            {
                ONOFF_SpatialOnUpdate(false);
            }
        }



        // ===== FEATURE MINIMAP AROUND ===== //

        private void ONOFF_MinimapAround(bool opt = true)
        {
            if (!init) return;


        }
    }
}
