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



        // ===== PUBLIC ===== //

        // it indicates the status of the class
        public enum SarExplorationControlUnitStatus
        {
            NotReady,
            Ready,
            SpatialOnUpdate
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

        public void VOICE_SpatialOnUpdate()
        {
            if (!init) return;

            Debug.Log($"{!(status == SarExplorationControlUnitStatus.SpatialOnUpdate)}");
            if (!(status == SarExplorationControlUnitStatus.SpatialOnUpdate))
            {
                SWITCH_SpatialOnUpdate();
                ONOFF_SpatialOnUpdate(true);
            }
            else
                ONOFF_SpatialOnUpdate(false);
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
            if(status == SarExplorationControlUnitStatus.NotReady)
            {
                return;
            }
            if(status == SarExplorationControlUnitStatus.Ready)
            {
                return;
            }
            if(status == SarExplorationControlUnitStatus.SpatialOnUpdate)
            {
                return;
            }
        }
    }
}
