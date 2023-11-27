using Packages.PositionDatabase.Components;
using Packages.StorageManager.Components;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Packages.SarExplorationFeatures.Components
{
    public class SarExplorationControlUnit : ProjectMonoBehaviour
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

        [Header("Other settings")]
        [Tooltip("User's heigh, used for spawning the markers in a more handly height for the user")]
        public float UserHeight = 1.85f;
        [Tooltip("The percentage of the height for dowing the height of the markers")]
        public float MarkerHeightPercent = 0.15f;
        [Tooltip("Send a signal to the other components when the status of the visual changes")]
        public List<UnityEvent> Signals = new List<UnityEvent>();



        // ===== PUBLIC ===== //

        // it indicates the status of the class
        public enum SarExplorationControlUnitStatus
        {
            NotReady,
            Ready,
            SpatialOnUpdate,
            SpatialAround,
            MinimapFollowingAround
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
        // FEATURE MinimapTransition
        private FeatureMinimapTransition minimapTransition = null;
        // Is minimap mode on 
        private bool isUsingMinimap = false;
        // Is minimap following the user
        private bool isMinimapFollowingUser = false;
        // position visualizer for minimap 
        private FeaturePositionVisualizer positionVisualizer = null;
        // ...
        UnityEvent onChangeCallbackEvent = null;



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
            // DbReference.CallOnZoneChanged.Clear();
            // DbReference.CallOnZoneCreated.Clear();

            if (StructureReference == null)
            {
                StructureReference = gameObject.AddComponent<MinimapStructure>();
            }

            if (DrawerReference == null)
            {
                DrawerReference = gameObject.AddComponent<PathDrawer>();
                DrawerReference.MarkerHeight -= MarkerHeightPercent * UserHeight;
                if (MarkersRoot == null)
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
            changeStatus();
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
            changeStatus();
        }

        public void VOICE_SpatialAroundIntensity(bool more = true)
        {
            if (spatialAround == null || !spatialAround.IsRunning) return;

            if (more)
                spatialAroundIntensity = (spatialAroundIntensity + 1 >= Intensities.Count ? Intensities.Count - 1 : spatialAroundIntensity + 1);
            else
                spatialAroundIntensity = (spatialAroundIntensity - 1 < 0 ? 0 : spatialAroundIntensity - 1);
            
            DrawableRadius = Intensities[spatialAroundIntensity];
            spatialAround.DrawableRadius = DrawableRadius;

            spatialAround.OnZoneChanged();
        }

        public void VOICE_MinimapFollowingAround(bool clean = false)
        {
            if (!init) return;

            if (!(status == SarExplorationControlUnitStatus.MinimapFollowingAround))
            {
                SWITCH_MinimapFollowingAround();
                ONOFF_MinimapFollowingAround(true);
            }
            else
            {
                if (clean) DrawerReference.RemoveMarkerAll();
                ONOFF_MinimapFollowingAround(false);
            }
            changeStatus();
        }

        public void VOICE_CommandClose()
        {
            if(status == SarExplorationControlUnitStatus.SpatialOnUpdate)
            {
                ONOFF_SpatialOnUpdate(false);
                DrawerReference.RemoveMarkerAll();
                changeStatus();
                return;
            }
            if (status == SarExplorationControlUnitStatus.SpatialAround)
            {
                ONOFF_SpatialAround(false);
                DrawerReference.RemoveMarkerAll();
                changeStatus();
                return;
            }
            if (status == SarExplorationControlUnitStatus.MinimapFollowingAround)
            {
                ONOFF_MinimapFollowingAround(false);
                DrawerReference.RemoveMarkerAll();
                changeStatus();
                return;
            }
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
                unregisterCallbacks();
                onChangeCallbackEvent = new UnityEvent();
                onChangeCallbackEvent.AddListener(spatialOnUpdate.OnZoneChanged);
                DbReference.CallOnZoneChanged.Add(onChangeCallbackEvent);

                spatialOnUpdate.IsRunning = true;
                status = SarExplorationControlUnitStatus.SpatialOnUpdate;
                spatialOnUpdate.OnZoneChanged();
            }
            else if(!opt && spatialOnUpdate.IsRunning)
            {
                unregisterCallbacks();

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
            else if(status == SarExplorationControlUnitStatus.MinimapFollowingAround)
            {
                ONOFF_MinimapFollowingAround(false);
            }
        }



        // ===== FEATURE SPATIAL AROUND ===== //

        private void ONOFF_SpatialAround(bool opt = true, bool useVisualizer = false)
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
                unregisterCallbacks();
                onChangeCallbackEvent = new UnityEvent();
                onChangeCallbackEvent.AddListener(spatialAround.OnZoneChanged);
                DbReference.CallOnZoneChanged.Add(onChangeCallbackEvent);

                spatialAround.IsRunning = true;
                spatialAround.DrawableRadius = this.DrawableRadius;
                status = SarExplorationControlUnitStatus.SpatialAround;

                if (useVisualizer && positionVisualizer != null)
                {
                    spatialAround.positionVisualizer = positionVisualizer;
                    positionVisualizer.IsRunning = true;
                }

                spatialAround.OnZoneChanged();
            }
            else if (!opt && spatialAround.IsRunning)
            {
                unregisterCallbacks();


                spatialAround.positionVisualizer = null;
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
            else if (status == SarExplorationControlUnitStatus.MinimapFollowingAround)
            {
                ONOFF_MinimapFollowingAround(false);
            }
        }



        // ===== FEATURE MINIMAP AROUND ===== //

        private void ONOFF_MinimapFollowingAround(bool opt = true)
        {
            if (!init) return;

            if(opt && minimapTransition == null)
            { 
                minimapTransition = gameObject.AddComponent<FeatureMinimapTransition>();
                // minimapTransition.ScaleFactor = 10.0f;
                minimapTransition.DbReference = DbReference;
                minimapTransition.DrawerReference = DrawerReference;
                minimapTransition.MinimapRoot = MarkersRoot;
                minimapTransition.IsRunning = false;
            }

            if (opt && !minimapTransition.IsRunning)
            {
                minimapTransition.IsRunning = true;
                minimapTransition.CreateMinimapStructure();
                minimapTransition.MakeFollowingMinimapStructure();
                positionVisualizer = minimapTransition.PositionVisualizer;

                ONOFF_SpatialAround(true, useVisualizer: true);
                status = SarExplorationControlUnitStatus.MinimapFollowingAround;
            }
            else if (!opt && minimapTransition.IsRunning)
            {
                minimapTransition.IsRunning = false;
                minimapTransition.DeleteFollowingMinimapStructure();
                minimapTransition.DeleteMinimapStructure();

                ONOFF_SpatialAround(false);
                status = SarExplorationControlUnitStatus.Ready;
            }
        }

        private void SWITCH_MinimapFollowingAround()
        {
            if (status == SarExplorationControlUnitStatus.Ready)
            {
                DrawerReference.RemoveMarkerAll();
                return;
            }
            if(status == SarExplorationControlUnitStatus.SpatialOnUpdate)
            {
                ONOFF_SpatialOnUpdate(false);
            }
            if (status == SarExplorationControlUnitStatus.SpatialAround)
            {
                ONOFF_SpatialAround(false);
                return;
            }
        }



        // ===== UTILITIES ===== //

        private void unregisterCallbacks()
        {
            if(onChangeCallbackEvent != null)
                DbReference.CallOnZoneChanged.Remove(onChangeCallbackEvent);
            onChangeCallbackEvent = null;
        }

        private void changeStatus()
        {
            foreach(UnityEvent sig in Signals)
                sig.Invoke();
        }
    }
}
