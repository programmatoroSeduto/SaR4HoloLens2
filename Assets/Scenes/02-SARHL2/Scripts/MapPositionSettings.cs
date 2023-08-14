using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Project.Scripts.Utils;

namespace Project.Scenes.SARHL2.Components
{
    public class MapPositionSettings : MonoBehaviour
    {
        public GameObject MapRoot = null;
        // public float UserHeight = 1.85f;

        // Start is called before the first frame update
        void Start()
        {
            bool isDebugMode = ( StaticAppSettings.GetOpt("IsDebugMode", "true") != "false" );
            if ( !isDebugMode )
            {
                gameObject.SetActive(false);
                return;
            }

            if(MapRoot == null) MapRoot = gameObject;
            float UserHeight = float.Parse(StaticAppSettings.GetOpt("UserHeight", "1.85"));
            MapRoot.transform.position -= UserHeight * Vector3.up;
        }

        // Update is called once per frame
        void Update()
        {

        }
    }

}