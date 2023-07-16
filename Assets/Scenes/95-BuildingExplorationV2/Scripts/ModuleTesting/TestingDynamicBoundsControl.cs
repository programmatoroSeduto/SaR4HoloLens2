using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI.BoundsControl;

public class TestingDynamicBoundsControl : MonoBehaviour
{

    // ===== GUI ===== //

    [Header("Main Settings")]
    [Tooltip("Main root object ref")]
    public GameObject MainRootRef = null;
    [Tooltip("Inner root object ref")]
    public GameObject InnerRootRef = null;
    // public GameObject BoundsControlCenter = null;
    public Vector3 Border = Vector3.zero;
    [Min(0.0f)]
    public float ScaleFactor = 100.0f;

    [Header("DebugZone")]
    public bool DebugZone = true;
    public int Children = 0;
    public List<Vector3> ObjPosInside = new List<Vector3>();
    public Vector3 AverageVector = Vector3.zero;
    public Vector3 BoxSize = Vector3.one;
    public bool SwitchBoundControl = false;



    // ===== PRIVATE ===== //

    // init op done
    private bool init = false;
    // bounds control to scale
    private BoundsControl boundsControl = null;
    // ...
    private BoxCollider dynamicCollider = null;



    // ===== UNITY CALLBACKS ===== //

    private void Start()
    {
        init = Init();
    }

    private bool Init()
    {
        dynamicCollider = MainRootRef.AddComponent<BoxCollider>();
        return true;
    }

    private void Update()
    {
        if (!init && !(init = Init())) return;

        // all the objects inside the inner root
        Children = InnerRootRef.transform.childCount;
        ObjPosInside.Clear();
        foreach (Transform child in InnerRootRef.transform)
            ObjPosInside.Add(child.localPosition);

        // compute average pos
        AverageVector = Vector3.zero;
        foreach (Transform child in InnerRootRef.transform)
            AverageVector += child.localPosition;
        AverageVector /= Children;
        // BoundsControlCenter.transform.localPosition = AverageVector;

        // compute scale
        BoxSize = 2 * (new Vector3(getMaxOverDimension(0, AverageVector), getMaxOverDimension(1, AverageVector), getMaxOverDimension(2, AverageVector)));
        dynamicCollider.center = AverageVector;
        dynamicCollider.size = BoxSize + Border;

        MainRootRef.transform.localScale = (ScaleFactor/100.0f) * Vector3.one;

        if (SwitchBoundControl && boundsControl == null)
            boundsControl = MainRootRef.AddComponent<BoundsControl>();
        else if (!SwitchBoundControl && boundsControl != null)
            Destroy(boundsControl);
    }



    // ===== PRIVATE FUNCTIONS ===== //

    private float getMaxOverDimension(int dim, Nullable<Vector3> posRef = null)
    {
        // x:0, y:1, z:2
        float max = float.MinValue;
        dim = dim % 3;
        foreach(Vector3 vv in ObjPosInside)
        {
            Vector3 v = vv - (posRef.HasValue ? posRef.Value : Vector3.zero);
            switch(dim)
            {
                case 0:
                    { if (Mathf.Abs(v.x) > max) max = Mathf.Abs(v.x); }
                    break;
                case 1:
                    { if (Mathf.Abs(v.y) > max) max = Mathf.Abs(v.y); }
                    break;
                case 2:
                    { if (Mathf.Abs(v.z) > max) max = Mathf.Abs(v.z); }
                    break;
            }
        }

        return max;
    }

    private float getMinOverDimension(int dim, Nullable<Vector3> posRef = null)
    {
        // x:0, y:1, z:2
        float min = float.MaxValue;
        dim = dim % 3;
        foreach (Vector3 vv in ObjPosInside)
        {
            Vector3 v = vv - (posRef.HasValue ? posRef.Value : Vector3.zero);
            switch (dim)
            {
                case 0:
                    { if (Mathf.Abs(v.x) < min) min = Mathf.Abs(v.x); }
                    break;
                case 1:
                    { if (Mathf.Abs(v.y) < min) min = Mathf.Abs(v.y); }
                    break;
                case 2:
                    { if (Mathf.Abs(v.z) < min) min = Mathf.Abs(v.z); }
                    break;
            }
        }

        return min;
    }
}
