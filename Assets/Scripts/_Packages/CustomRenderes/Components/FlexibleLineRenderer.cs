using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Packages.CustomRenderers.Components
{
    public class FlexibleLineRenderer : MonoBehaviour
    {
        // ===== GUI ===== //

        [Header("Main settings")]
        [Tooltip("Reference to the first object to link. The first object is also the starting point of the line")]
        public GameObject Object1 = null;
        [Tooltip("(dynamic) either to use a custom statico position or not")]
        public bool UseStaticFirstPosition = false;
        [Tooltip("(UseStaticFirstPosition is true) If you want to use a fixed position instead of a object as first, set a position here")]
        public Vector3 StaticFirstPosition = Vector3.zero;
        [Tooltip("Reference to the second object to be linked")]
        public GameObject Object2 = null;
        [Tooltip("(dynamic) either to use a end custom statico position or not")]
        public bool UseStaticSecondPosition = false;
        [Tooltip("(UseStaticSecondPosition is true) If you want to use a fixed position instead of a object as second, set a position here")]
        public Vector3 StaticSecondPosition = Vector3.zero;

        [Header("Flexible Line Style")]
        [Tooltip("Color of the line")]
        public Color LineColor = Color.green;
        [Tooltip("Start Width of the line")]
        public float LineWidth = 0.05f;
        [Tooltip("Use a particular material; if no material is set, a default one is used")]
        public Material LineMaterialCustom = null;

        [Header("Flexible Line Update Settings")]
        [Tooltip("Update on start?")]
        public bool UpdateOnStart = true;
        [Tooltip("(dyynamic) update rate in 1/seconds")]
        [Min(0.01f)]
        public float UpdateRate = 60.0f;
        [Tooltip("(dyynamic) update line position at each frame")]
        public bool UpdateLinePos = true;
        [Tooltip("(dyynamic) update line material at each frame")]
        public bool UpdateMaterial = false;
        [Tooltip("(dyynamic) update line width at each frame")]
        public bool UpdateWidth = true;
        public bool ScaleWithParent = true;
        [Tooltip("(dyynamic) update line colour at each frame")]
        public bool UpdateColor = false;



        // ===== PRIVATE ===== //

        // init script
        private bool init = false;
        // default material (shared with other FLRs, created on Start())
        private static Material lineMaterialDefault = null;
        // line renderers ids
        private static int id = 0;
        // the material assigned to the line
        private Material lineMaterialCurrent = null;
        // the main component -- lineRenderer
        private LineRenderer lineRenderer = null;
        // the root containing the lineRenderer component
        private GameObject goLineRenderer = null;
        // main execution coroutine
        private Coroutine COR_UpdateLineRenderer = null;
        // minimum update rate value
        private float MinUpdateRateDefault = 0.5f;
        // maximun update rate value
        private float MaxUpdateRateDefault = 60.0f;



        // ===== UNITY CALLBACKS ===== //

        private void Update()
        {
            if(!init)
                init = TrySetupFlexibleLineRenderer();
        }

        private void OnEnable()
        {
            if (!init) return;
            if (COR_UpdateLineRenderer == null) EVENT_StartUpdate();
        }

        private void OnDisable()
        {
            if (!init) return;
            if (COR_UpdateLineRenderer != null) EVENT_StopUpdate();
        }

        private void OnDestroy()
        {
            // Debug.Log("FlexibleLineRenderer.OnDestroy() called");
            if (goLineRenderer != null)
            {
                StopAllCoroutines();
                // Debug.Log($"FlexibleLineRenderer.OnDestroy() go with renderer go:{goLineRenderer.name}");
                MonoBehaviour.DestroyImmediate(lineRenderer, true);
                lineRenderer = null;
                GameObject.DestroyImmediate(goLineRenderer, true);
                goLineRenderer = null;

                init = false;
            }
        }



        // ===== OBJECT EVENTS ===== //

        public void EVENT_StartUpdate()
        {
            if (!init && !TrySetupFlexibleLineRenderer())
                return;

            if (COR_UpdateLineRenderer != null) EVENT_StopUpdate();
            COR_UpdateLineRenderer = StartCoroutine(BSCOR_UpdateLineRenderer());
        }

        public void EVENT_StopUpdate()
        {
            if (!init) return;

            StopAllCoroutines();
            COR_UpdateLineRenderer = null;
        }



        // ===== FEATURE FLEXIBLE LINE SETUP AND UPDATE ===== //

        public bool TrySetupFlexibleLineRenderer(bool startCoroutine = true)
        {
            if(goLineRenderer == null)
            {
                goLineRenderer = new GameObject();
                goLineRenderer.name = "LINE_RENDERER_" + (id++).ToString("0000");
                goLineRenderer.transform.SetParent(gameObject.transform);
            }

            lineRenderer = goLineRenderer.AddComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                GameObject.Destroy(goLineRenderer);
                goLineRenderer = null;

                Debug.LogError("Can't instanciate LineRenderer component!");

                init = false;
                return false;
            }

            if (Object1 == null) Object1 = goLineRenderer.transform.parent.gameObject;
            init = true;
            
            SetMaterial(LineMaterialCustom);
            SetLineWidth(LineWidth);
            SetLineColor(LineColor);
            if (UpdateOnStart)
                COR_UpdateLineRenderer = StartCoroutine(BSCOR_UpdateLineRenderer());

            return true;
        }

        private IEnumerator BSCOR_UpdateLineRenderer()
        {
            while(true)
            {
                int fps = (int)(1f / Time.unscaledDeltaTime);
                if (UpdateRate > Mathf.Min(fps, MaxUpdateRateDefault))
                    yield return new WaitForEndOfFrame( );
                else
                    yield return new WaitForSecondsRealtime(1.0f / ( UpdateRate >= MinUpdateRateDefault ? UpdateRate : MinUpdateRateDefault));

                if(UpdateLinePos && !UpdateLinePosition())
                    Debug.LogWarning("ERROR: can't set line position!");
                if (UpdateWidth && !SetLineWidth(LineWidth))
                    Debug.LogWarning("ERROR: can't set line width!");
                if (UpdateColor && !SetLineColor(LineColor))
                    Debug.LogWarning("ERROR: can't set line colour!");
                if (UpdateMaterial && !SetMaterial(LineMaterialCustom))
                    Debug.LogWarning("ERROR: can't set material!");
            }
        }



        // ===== FEATURE FLEXIBLE LINE ===== //

        private bool UpdateLinePosition()
        {
            if (!init) return false;

            if (Object1 == null && !UseStaticFirstPosition) return false;
            if (Object2 == null && !UseStaticSecondPosition) return false;

            Vector3[] dir = new Vector3[] { 
                (UseStaticFirstPosition ? StaticFirstPosition : Object1.transform.position ),
                (UseStaticSecondPosition ? StaticSecondPosition : Object2.transform.position )
            };

            lineRenderer.SetPositions(dir);
            return true;
        }



        // ===== FEATURE SET LINE STYLE ===== //

        public bool SetMaterial(Material mat = null)
        {
            if (!init)
                return false;

            if(lineMaterialDefault == null)
                lineMaterialDefault = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));

            lineRenderer.material = (mat == null ? lineMaterialDefault : LineMaterialCustom);
            LineMaterialCustom = (mat == null ? lineMaterialDefault : LineMaterialCustom);
            lineMaterialCurrent = LineMaterialCustom;

            return true;
        }

        public bool SetLineWidth(float lw)
        {
            if (!init || lw < 0.0f) return false;

            float lWidth = lw * (ScaleWithParent ? gameObject.transform.parent.localScale.x : gameObject.transform.localScale.x) * 0.25f;
            lineRenderer.startWidth = lWidth;
            lineRenderer.endWidth = lWidth;

            return true;
        }

        public bool SetLineColor(Color c)
        {
            if (!init) return false;

            LineColor = c;
            lineRenderer.startColor = LineColor;
            lineRenderer.endColor = LineColor;

            return true;
        }
    }

}