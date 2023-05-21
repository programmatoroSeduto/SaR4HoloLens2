using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Microsoft.MixedReality.Toolkit.UI;

namespace Packages.VisualItems.LittleMarker.Components
{
    public class LittleMarkerBaseHandle : MonoBehaviour
    {
        public string InitObjectName = "LittleMarker";

        // Tooltip settings
        private string MarkerTextObjectPath = "tooltip";
        private GameObject MarkerTextObject = null;
        private ToolTip MarkerTextTooltipComponent = null;

        void Start()
        {
            if (InitObjectName == "") InitObjectName = gameObject.name;
            else gameObject.name = InitObjectName;
        }

        void Update()
        {
            if(MarkerTextObject == null)
            {
                MarkerTextObject = gameObject.transform.Find(MarkerTextObjectPath).gameObject;
                MarkerTextTooltipComponent = MarkerTextObject.GetComponent<ToolTip>();
            }

            InitObjectName = gameObject.name;
            MarkerTextTooltipComponent.ToolTipText = InitObjectName;
        }
    }
}
