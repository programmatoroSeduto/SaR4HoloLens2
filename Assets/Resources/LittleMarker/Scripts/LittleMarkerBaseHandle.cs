using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Input;

namespace Packages.VisualItems.LittleMarker.Components
{
    public class LittleMarkerBaseHandle : MonoBehaviour
    {
        public string InitObjectName = "";

        // Tooltip settings
        private string MarkerTextObjectPath = "tooltip";
        private GameObject MarkerTextObject = null;
        private ToolTip MarkerTextTooltipComponent = null;
        private ObjectManipulator objectManipulator = null;
        private NearInteractionGrabbable nearInteraction = null;

        void Start()
        {
            // if (InitObjectName == "") InitObjectName = gameObject.name;
            // else gameObject.name = InitObjectName;

            objectManipulator = gameObject.GetComponent<ObjectManipulator>();
            nearInteraction = gameObject.GetComponent<NearInteractionGrabbable>();
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

        public bool SetManipulation(bool opt = true)
        {
            if (objectManipulator == null || nearInteraction == null) return false;

            objectManipulator.enabled = opt;
            nearInteraction.enabled = opt;

            return true;
        }
    }
}
