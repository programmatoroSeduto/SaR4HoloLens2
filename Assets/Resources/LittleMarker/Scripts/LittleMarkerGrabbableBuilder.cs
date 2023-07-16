using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Packages.VisualItems.LittleMarker.Components
{
	public class LittleMarkerGrabbableBuilder : MonoBehaviour
	{
		public static readonly string LittleMarkerBasePrefabPath = "LittleMarker/LittleMarkerGrabbable";

		[Header("Other settings")]
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

		private LittleMarkerBaseHandle LastSpawnedItem;

		private void Start()
		{
			if (SpawnOnStart)
				this.Build();
		}

		public LittleMarkerBaseHandle GetLastSpawned()
		{
			return LastSpawnedItem;
		}

		public void EVENT_Build()
		{
			this.Build();
		}

		public GameObject Build(Nullable<Vector3> position = null, Nullable<Vector3> rotation = null)
		{
			/*
            Debug.Log($"Build(position:{(position == null ? "NULL" : position.Value.ToString())}, rotation:{(rotation == null ? "NULL" : rotation.Value.ToString())}) -- ");
            */
			Debug.Log($"Build(position:{(position == null ? "NULL" : position.Value.ToString())}, rotation:{(rotation == null ? "NULL" : rotation.Value.ToString())}) -- START");

			GameObject prefab = Resources.Load(LittleMarkerBasePrefabPath) as GameObject;
			if (prefab == null)
			{
				Debug.LogError("prefab is null!");
				return null;
			}

			InitPosition = (position == null ? InitPosition : (Vector3) position);
			InitRotation = (rotation == null ? InitRotation : (Vector3) rotation);

			GameObject go = Instantiate(prefab, InitPosition, Quaternion.Euler(InitRotation));
			go.name = InitName;

			var handle = go.AddComponent<LittleMarkerBaseHandle>();
			LastSpawnedItem = handle;

			if (SpawnUnderObject != null)
				go.transform.SetParent(SpawnUnderObject.transform);

			Debug.Log($"Build(position:{(position == null ? "NULL" : position.Value.ToString())}, rotation:{(rotation == null ? "NULL" : rotation.Value.ToString())}) -- createn new object with name:{go.name} parentName:{go.transform.parent.gameObject.name} position:{go.transform.position} rotation:{go.transform.rotation}");
			return go;
		}
	}
}