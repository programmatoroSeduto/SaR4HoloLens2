using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Packages.VisualItems.LittleMarker.Components
{
	public class LittleMarkerBaseBuilder : MonoBehaviour
	{
		public static readonly string LittleMarkerBasePrefabPath = "LittleMarker/LittleMarkerBase";

		[Header("Other settings")]
		[Tooltip("Label of the item")]
		public string InitName = "";

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

		public LittleMarkerBaseHandle Build()
		{
			GameObject prefab = Resources.Load(LittleMarkerBasePrefabPath) as GameObject;
			if (prefab == null)
			{
				// ... ERROR! ...
				return null;
			}

			GameObject go = Instantiate(prefab);
			go.name = InitName;
			var handle = go.GetComponent<LittleMarkerBaseHandle>();

			if (SpawnUnderObject != null)
				go.transform.SetParent(SpawnUnderObject.transform);

			return handle;
		}
	}
}