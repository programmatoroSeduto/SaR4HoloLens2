using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Packages.MinimapTools.Components;

namespace Packages.SarExplorationFeatures.Components
{
    public class FeatureMinimapTransition : FeatureBase
    {
        // ===== PUBLIC ===== //

        [HideInInspector] // the root object of the minimap
        public GameObject MinimapRoot = null;
        [HideInInspector] // minimap scale factor
        public float ScaleFactor = 10.0f;
        [HideInInspector] // the size of the box containing the minimap
        public Vector3 SpaceSize = new Vector3(6.0f, 1.0f, 6.0f);

        public FeaturePositionVisualizer PositionVisualizer
        {
            get => positionVisualizer;
        }



        // ===== PRIVATE ===== //

        // minimap outer root game object
        private GameObject outerRoot = null;
        // minimap pivot game object
        private GameObject mapPivot = null;
        // minimap solver (external level)
        private MinimapSolver mapSolver = null;
        // minimap pivot component
        private MinimapPivot pivotComponent = null;
        // reference to the box collider of the minimap
        private GameObject boxCollider = null;
        // path of the material
        private static readonly string mapMaterialPath = "MinimapFeature/Materials/MinimapWrapperMaterial";
        // reference to the material for the minimap
        private Material mapMaterial = null;
        // path of the material used for pointing out the current position in movable map
        private static readonly string currentPosMaterialPath = "MinimapFeature/Materials/MinimapWrapperMaterial";
        // reference to the material for the current position
        private Material currentPosMaterial = null;
        // reference to the component used for pointing ut the user's current position
        private FeaturePositionVisualizer positionVisualizer = null;



        // ===== FEATURE BUILD MINIMAP STRUCTURE ===== //

        public bool CreateMinimapStructure()
        {
            if (MinimapRoot == null || mapPivot != null) 
                return false;

            outerRoot = new GameObject(name: "MinimapExternalRoot");
            outerRoot.transform.localPosition = Vector3.zero;
            outerRoot.transform.localRotation = Quaternion.identity;
            outerRoot.transform.localScale = Vector3.one;

            mapPivot = new GameObject(name: "MinimapPivot");
            mapPivot.transform.SetParent(outerRoot.transform);
            mapPivot.transform.localPosition = Vector3.zero;
            mapPivot.transform.localRotation = Quaternion.identity;
            mapPivot.transform.localScale = Vector3.one;

            MinimapRoot.transform.SetParent(mapPivot.transform);
            MinimapRoot.transform.localPosition = Vector3.zero;
            MinimapRoot.transform.localRotation = Quaternion.identity;
            MinimapRoot.transform.localScale = Vector3.one;

            return true;
        }

        public bool DeleteMinimapStructure()
        {
            if (MinimapRoot == null || mapPivot == null) 
                return false;

            MinimapRoot.transform.parent = null;
            MinimapRoot.transform.localPosition = Vector3.zero;
            MinimapRoot.transform.localRotation = Quaternion.identity;
            MinimapRoot.transform.localScale = Vector3.one;

            GameObject.Destroy(outerRoot);
            outerRoot = null;
            GameObject.Destroy(mapPivot);
            mapPivot = null;

            return true;
        }



        // ===== FEATURE FOLLOWING MINIMAP ===== //

        public bool MakeFollowingMinimapStructure()
        {
            if (MinimapRoot == null)
                return false;
            if (mapPivot == null || mapSolver != null)
                return false;
            if(mapMaterial == null)
            {
                mapMaterial = Resources.Load(mapMaterialPath) as Material;
                if(mapMaterial == null)
                {
                    Debug.LogError($"Unable to find the material, given folder '{mapMaterialPath}'");
                    return false;
                }
            }
            if (currentPosMaterial == null)
            {
                currentPosMaterial = Resources.Load(currentPosMaterialPath) as Material;
                if (currentPosMaterial == null)
                {
                    Debug.LogError($"Unable to find the material, given folder '{currentPosMaterialPath}'");
                    return false;
                }
            }

            mapSolver = outerRoot.AddComponent<MinimapSolver>();
            
            pivotComponent = mapPivot.AddComponent<MinimapPivot>();
            pivotComponent.SpaceSize = SpaceSize;
            pivotComponent.RotateMapByCamera = true;
            pivotComponent.ApplyDisplacement = true;
            
            pivotComponent.ScaleFactorPercent = ScaleFactor;
            
            pivotComponent.BoxReference = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pivotComponent.BoxReference.name = "MapBox";
            pivotComponent.BoxReference.transform.SetParent(outerRoot.transform);
            pivotComponent.BoxReference.transform.localPosition = Vector3.zero;
            pivotComponent.BoxReference.transform.localRotation = Quaternion.identity;
            pivotComponent.BoxReference.transform.localScale = SpaceSize;
            pivotComponent.BoxReference.GetComponent<Renderer>().material = mapMaterial;
            
            pivotComponent.MapWrapper = outerRoot;
            pivotComponent.MapPivot = mapPivot;
            pivotComponent.MapRoot = MinimapRoot;

            if(positionVisualizer == null)
            {
                positionVisualizer = outerRoot.AddComponent<FeaturePositionVisualizer>();
                positionVisualizer.DrawerReference = DrawerReference;
                positionVisualizer.DbReference = DbReference;
                positionVisualizer.CursorMaterial = currentPosMaterial;
            }
            positionVisualizer.IsRunning = true;

            return true;
        }

        public bool DeleteFollowingMinimapStructure(bool resetPosition = false, bool resetRotation = true)
        {
            if (MinimapRoot == null)
                return false;
            if (mapPivot == null || mapSolver == null || positionVisualizer == null)
                return false;

            if (resetPosition)
                MinimapRoot.transform.localPosition = Vector3.zero;
            if (resetRotation)
                MinimapRoot.transform.localRotation = Quaternion.identity;

            positionVisualizer.IsRunning = false;

            Destroy(mapSolver);
            mapSolver = null;

            Destroy(pivotComponent.BoxReference);
            Destroy(pivotComponent);
            pivotComponent = null;

            return true;
        }
    }
}
