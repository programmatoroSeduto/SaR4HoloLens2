using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Packages.CustomRenderers.Components
{
    public class FlexibleStarOfLinesRenderer : MonoBehaviour
    {
        public GameObject Center = null;
        public List<GameObject> Vertices = new List<GameObject>();
        public Color LineColor = Color.green;
        public float LineWidth = 0.05f;

        private bool initDone = false;
        private GameObject lineRendererRootObject = null;
        private List<GameObject> lineRenderers = new List<GameObject>();

        private void Start()
        {
            InitLines();
        }

        private void Update()
        {
            if (!initDone && !InitLines()) 
                return;
            if ((lineRenderers.Count != Vertices.Count) && !ReallocateLines()) 
                return;
            
            UpdateLines();
        }

        private bool InitLines()
        {
            if (initDone) return true;
            if (Center == null || Vertices.Count == 0) return false;

            if(lineRendererRootObject == null)
            {
                lineRendererRootObject = new GameObject();
                lineRendererRootObject.transform.SetParent(Center.transform);
                lineRendererRootObject.name = "lineRendererRootObject";
            }

            for (int i = 0; i < Vertices.Count; ++i)
            {
                GameObject go = new GameObject();
                go.name = $"lr{i}";
                go.transform.SetParent(lineRendererRootObject.transform);
                LineRenderer lr = go.AddComponent<LineRenderer>();
                lr.material = new Material(Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply"));
                lineRenderers.Add(go);
            }

            initDone = true;
            return true;
        }

        private bool ReallocateLines()
        {
            Destroy(lineRendererRootObject);
            lineRenderers.Clear();
            lineRendererRootObject = null;

            initDone = false;
            return InitLines();
        }

        private void UpdateLines()
        {
            if (Center == null || Vertices.Count == 0) return;

            for(int i=0; i<Vertices.Count; ++i)
            {
                LineRenderer lr = lineRenderers[i].GetComponent<LineRenderer>();

                lr.startColor = LineColor;
                lr.endColor = LineColor;
                lr.startWidth = LineWidth;
                lr.endWidth = LineWidth;

                lr.SetPositions(new Vector3[] { 
                    Center.transform.position,
                    Vertices[i].transform.position 
                });
            }
        }
    }
}
