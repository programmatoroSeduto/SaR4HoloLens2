using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Project.Scripts.Utils;

namespace Project.Scenes.SARHL2_DEVELOPMENT.Components
{
    public class MapPositionSettings : ProjectMonoBehaviour
    {

        public GameObject MapRoot = null;
        public bool ForceActive = false;
        public bool UserHeightFromParams = true;
        public bool CheckDebugMode = true;
        public float UserHeightGui = 1.85f;

        // Start is called before the first frame update
        void Start()
        {
            bool isDebugMode = ForceActive;
            if (CheckDebugMode)
                isDebugMode = isDebugMode || (bool)StaticAppSettings.GetObject("DebugMode", true);
            if ( !isDebugMode )
            {
                StaticLogger.Info("MapPositionSettings", "Debug mode is not enabled", logLayer: 3);
                gameObject.SetActive(false);
                return;
            }

            if(MapRoot == null) MapRoot = gameObject;
            float UserHeight = UserHeightGui;
            if (UserHeightFromParams)
            {
                UserHeight = (float)StaticAppSettings.GetObject("UserHeight", 1.85f);
                UserHeightGui = UserHeight;
            }
            MapRoot.transform.position -= UserHeight * Vector3.up;

            MapRoot.SetActive(true);
            Ready(disableComponent: true);
        }
    }

}