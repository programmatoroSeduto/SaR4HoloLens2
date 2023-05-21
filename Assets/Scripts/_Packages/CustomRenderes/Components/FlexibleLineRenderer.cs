using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Packages.CustomRenderers.Components
{
    public class FlexibleLineRenderer : MonoBehaviour
    {
        public GameObject Object1 = null;
        public GameObject Object2 = null;
        public Color LineColor = Color.green;
        public float LineWidth = 0.05f;

        private LineRenderer lineRenderer = null;

        // Start is called before the first frame update
        void Start()
        {
            if (Object1 == null) Object1 = gameObject;
        }

        // Update is called once per frame
        void Update()
        {
            UpdateLineRenderer();
        }

        private void UpdateLineRenderer()
        {
            if (Object1 == null || Object2 == null) return;
            if (lineRenderer == null)
            {
                lineRenderer = Object1.AddComponent<LineRenderer>();
                lineRenderer.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
            }
            
            lineRenderer.startColor = LineColor;
            lineRenderer.endColor = LineColor;

            lineRenderer.startWidth = LineWidth;
            lineRenderer.endWidth = LineWidth;

            lineRenderer.SetPositions(new Vector3[] { Object1.transform.position, Object2.transform.position });
        }
    }

}