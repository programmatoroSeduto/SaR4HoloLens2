using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Packages.ARMarker.Components
{
	public class ARMarkerBuilder : MonoBehaviour
	{
		// ===== GLOBALS ===== // 

		// Path of the resource
		[HideInInspector]
		public static readonly string LittleMarkerBasePrefabPath = "ARMarker/ARMarkerBase";



		// ===== GUI ===== // 

		[Header("AR Marker Base Settings")]
		[Tooltip("Label of the item")]
		public string InitName = "";
		[Tooltip("Init position")]
		public Vector3 InitPosition = Vector3.zero;
		[Tooltip("Init orientation")]
		public Vector3 InitRotation = Quaternion.identity.eulerAngles;
		[Tooltip("Spawn on start")]
		public bool SpawnOnStart = false;
		[Tooltip("Spawn under a given GameObject")]
		public GameObject SpawnUnderObject = null;



		// ===== PUBLIC ===== // 

		// last spawned item
		public GameObject LastSpawnedGameObject
        {
			get => lastSpawnedGo;
        }
		// handle related to the last spawned GameObject
		public ARMarkerHandle LastSpawnedHandle
        {
			get => lastSpawnedHandle;
        }



		// ===== PRIVATE ===== // 

		// the last spawned item (GameObject)
		private GameObject lastSpawnedGo = null;
		// the last spawned item (Handle)
		private ARMarkerHandle lastSpawnedHandle = null;



		// ===== UNITY CALLBACKS ===== // 

		private void Start()
		{
			if (SpawnOnStart)
				this.Build();
		}



		// ===== EVENTS ===== // 

		public void EVENT_Build()
		{
			this.Build();
		}



		// ===== FEATURE BUILD MARKER ===== // 

		public GameObject Build(Nullable<Vector3> position = null, Nullable<Vector3> rotation = null)
		{
			GameObject prefab = Resources.Load(LittleMarkerBasePrefabPath) as GameObject;
			if (prefab == null)
			{
				Debug.LogError("prefab is null!");
				return null;
			}

			InitPosition = (position == null ? InitPosition : (Vector3)position);
			InitRotation = (rotation == null ? InitRotation : (Vector3)rotation);
			Quaternion qRot = Quaternion.Euler(InitRotation);

			GameObject go = Instantiate(prefab, InitPosition, qRot);
			go.name = InitName;

			var handle = go.AddComponent<ARMarkerHandle>();
			lastSpawnedGo = go;
			lastSpawnedHandle = handle;

			if (SpawnUnderObject != null)
				go.transform.SetParent(SpawnUnderObject.transform);
			
			go.transform.localPosition = InitPosition;
			go.transform.localRotation = qRot;
			go.transform.localScale = Vector3.one;
			
			return go;
		}
	}
}
