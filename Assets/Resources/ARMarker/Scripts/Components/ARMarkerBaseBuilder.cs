using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Packages.VisualItems.ARMarker.Components
{
    public class ARMarkerBaseBuilder : MonoBehaviour
    {
        public static readonly string ARMarkerBasePrefabPath = "ARMarker/ARMarkerBase";



        [Header("Marker Positioning")]
        [Tooltip("Position wrt the World frame")]
        public Vector3 MarkerPosition = Vector3.zero;

        [Tooltip("Orientation angle wrt Yaw axis")]
        public float YawOrientation = 0.0f;


        [Header("Marker settings")]
        [Tooltip("Name of the marker root")]
        public string InitMarkerName = "Marker";
        
        [Tooltip("Initial Text inside the marker textbox")]
        public string InitText = "";


        [Header("Other settings")]
        [Tooltip("Spawn on start")]
        public bool SpawnOnStart = false;

        [Tooltip("Spawn under a given GameObject")]
        public GameObject SpawnUnderObject = null;



        private void Start()
        {
            if (SpawnOnStart)
                this.Build();
        }

        public void EVENT_Build() => this.Build();

        public ARMarkerBaseHandle Build()
        {
            GameObject prefab = Resources.Load(ARMarkerBasePrefabPath) as GameObject;
            if(prefab == null)
            {
                Debug.LogError($"[ARMarkerBuilder] ERROR: cannot find prefab '{ARMarkerBasePrefabPath}'");
                return null;
            }

            GameObject marker = Instantiate(prefab, MarkerPosition, Quaternion.Euler(0.0f, 0.0f, YawOrientation));
            marker.name = InitMarkerName;
            ARMarkerBaseHandle markerHandle = marker.GetComponent<ARMarkerBaseHandle>();
            markerHandle.EVENT_SET_TextContent(InitText);

            if(SpawnUnderObject != null)
                marker.transform.SetParent(SpawnUnderObject.transform);

            return markerHandle;
        }
    }
}

