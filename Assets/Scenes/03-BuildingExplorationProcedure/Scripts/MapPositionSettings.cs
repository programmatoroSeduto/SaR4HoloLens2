using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SaR4Hololens2.Scenes.BuildingExplorationProcedure.Components
{
    public class MapPositionSettings : MonoBehaviour
    {
        public GameObject MapRoot = null;
        public float UserHeight = 1.85f;

        // Start is called before the first frame update
        void Start()
        {
            if(MapRoot == null) MapRoot = gameObject;
            MapRoot.transform.position -= UserHeight * Vector3.up;
        }

        // Update is called once per frame
        void Update()
        {

        }
    }

}