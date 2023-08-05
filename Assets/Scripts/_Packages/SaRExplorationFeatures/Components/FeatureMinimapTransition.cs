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
        private BoxCollider boxCollider = null;



        // ===== FEATURE BUILD MINIMAP STRUCTURE ===== //

        public bool CreateMinimapStructure()
        {
            if (MinimapRoot == null || mapPivot != null) 
                return false;

            outerRoot = new GameObject(name: "MinimapExternalRoot");
            outerRoot.transform.localPosition = Vector3.zero;
            outerRoot.transform.localRotation = Quaternion.identity;
            outerRoot.transform.localScale = Vector3.one;
            boxCollider = outerRoot.AddComponent<BoxCollider>();
            boxCollider.center = Vector3.zero;
            boxCollider.size = SpaceSize;

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

            GameObject.Destroy(outerRoot);
            outerRoot = null;
            GameObject.Destroy(mapPivot);
            mapPivot = null;

            return true;
        }



        // ===== FEATURE FOLLOWING MINIMAP ===== //

        public bool MakeFollowingMinimapStructure()
        {
            if (MinimapRoot == null || mapPivot == null || mapSolver != null)
                return false;

            mapSolver = outerRoot.AddComponent<MinimapSolver>();
            
            pivotComponent = mapPivot.AddComponent<MinimapPivot>();
            pivotComponent.SpaceSize = SpaceSize;
            pivotComponent.RotateMapByCamera = true;
            pivotComponent.ApplyDisplacement = true;
            
            boxCollider.size = SpaceSize;
            pivotComponent.ScaleFactorPercent = ScaleFactor;
            pivotComponent.BoxReference = boxCollider;
            pivotComponent.MapRoot = outerRoot;
            pivotComponent.MapPivot = mapPivot;

            return true;
        }

        public bool DeleteFollowingMinimapStructure()
        {
            if (MinimapRoot == null || mapPivot == null || mapSolver == null)
                return false;

            Destroy(mapSolver);
            mapSolver = null;

            Destroy(pivotComponent);
            pivotComponent = null;

            return true;
        }
    }
}
