using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Input;

namespace Packages.ARMarker.Components
{
    public class ARMarkerHandle : MonoBehaviour
    {
        // ===== GUI ===== // 

		[Header("AR Marker Base Settings")]
        [Tooltip("Tet inside the tooltip of the marker (read only)")]
        public string MarkerTooltipTextContent = "";
        [Tooltip("Take the text of the tooltip from the gameObject")]
        public bool MarkerTooltipTextFromGameObject = true;
        [Tooltip("Set the marker manipulable (dynamic switch)")]
        public bool IsManipulable = false;
        [Tooltip("Update tooltip position for improving readability")]
        public bool UpdateTooltipOrientation = true;

        [Header("Appearance Settings")]
        [Tooltip("Either use the first material or the second one")]
        public bool UseSecondMaterial = false;
        [Tooltip("First Material (default: first material)")]
        public Material FirstMaterial = null;
        [Tooltip("Second Material")]
        public Material SecondMaterial = null;
        [Tooltip("The first material is the BaseMaterial by default; check this if you want to choose another material as First. Notice that the base Material internal reference is not overwritten, hence putting false into this field will override the first material with base material as default")]
        public bool AllowRewriteFirstMaterial = false;


        // ===== PRIVATE ===== // 

        // object containing the tooltip component
        private GameObject MarkerTextObject = null;
        // reference to the tooltip component (for setting the text)
        private ToolTip MarkerTextTooltipComponent = null;
        // MRTK manipulation functionality
        private ObjectManipulator objectManipulator = null;
        // MRTK manipulation feedback
        private NearInteractionGrabbable nearInteraction = null;
        // required for MRTK manipulation
        private BoxCollider boxCollider = null;
        // the first material of the object
        private Material baseMaterial = null;
        // reference to the renderer of the gameObject
        private Renderer markerRenderer = null;
        // reference to the script which updated the position of the tooltip every time
        private Billboard billboard = null;
        // to improve update performances
        private bool prevIsManipulable = true;
        private bool prevUseSecondMaterial = true;
        private bool prevUpdateTooltipOrientation = true;



        // ===== UNITY CALLBACKS ===== // 

        private void Start()
        {
            if(gameObject.GetComponent<BoxCollider>() == null)
            {
                boxCollider = gameObject.AddComponent<BoxCollider>();
                boxCollider.size = gameObject.transform.lossyScale.x * new Vector3(0.1f, 0.2f, 0.1f);
            }

            if (gameObject.GetComponent<ObjectManipulator>() == null)
            {
                objectManipulator = gameObject.AddComponent<ObjectManipulator>();
            }

            if (gameObject.GetComponent<NearInteractionGrabbable>() == null)
            {
                nearInteraction = gameObject.AddComponent<NearInteractionGrabbable>();
            }

            markerRenderer = gameObject.transform.Find("home").gameObject.GetComponent<Renderer>();
            baseMaterial = markerRenderer.material;
            billboard = gameObject.transform.Find("tooltip/Pivot/ContentParent").gameObject.GetComponent<Billboard>();

            _ = SetManipulation(IsManipulable);
            prevIsManipulable = IsManipulable;

            _ = SwitchMaterial(useFirst: !UseSecondMaterial, rewriteBaseMat: AllowRewriteFirstMaterial);
            prevUseSecondMaterial = UseSecondMaterial;

            billboard.enabled = UpdateTooltipOrientation;
            prevUpdateTooltipOrientation = UpdateTooltipOrientation;
        }

        private void Update()
        {
            if (MarkerTextTooltipComponent == null)
            {
                MarkerTextObject = gameObject.transform.Find("tooltip").gameObject;
                if(MarkerTextObject != null)
                    MarkerTextTooltipComponent = MarkerTextObject.GetComponent<ToolTip>();
            }

            if(MarkerTextTooltipComponent != null) 
                MarkerTextTooltipComponent.ToolTipText = (MarkerTooltipTextFromGameObject ? gameObject.name : MarkerTooltipTextContent );

            if (prevIsManipulable != IsManipulable && SetManipulation(IsManipulable))
                prevIsManipulable = IsManipulable;

            if (prevUseSecondMaterial != UseSecondMaterial && SwitchMaterial(useFirst: !UseSecondMaterial, rewriteBaseMat: AllowRewriteFirstMaterial))
                prevUseSecondMaterial = UseSecondMaterial;

            if(prevUpdateTooltipOrientation != UpdateTooltipOrientation)
            {
                billboard.enabled = UpdateTooltipOrientation;
                prevUpdateTooltipOrientation = UpdateTooltipOrientation;
            }
        }



        // ===== FEATURE MANIPULABLE MARKER ===== // 

        public bool SetManipulation(bool opt = true)
        {
            if (objectManipulator == null || nearInteraction == null) return false;

            objectManipulator.enabled = opt;
            nearInteraction.enabled = opt;

            return true;
        }



        // ===== FEATURE SWITCH MATERIAL ===== // 

        public bool SwitchMaterial(bool useFirst=false, bool rewriteBaseMat=false)
        {
            if (markerRenderer == null || (!useFirst && SecondMaterial == null))
                return false;

            if(useFirst)
                markerRenderer.material = ( rewriteBaseMat ? FirstMaterial : baseMaterial );
            else
                markerRenderer.material = SecondMaterial;

            return true;
        }



        // ===== FEATURE MODIFY MARKER TEXT ===== // 

        public bool SetText(string txt)
        {
            if (MarkerTextTooltipComponent == null) return false;

            MarkerTooltipTextFromGameObject = false;
            MarkerTextTooltipComponent.ToolTipText = txt;

            return true;
        }

    }
}
