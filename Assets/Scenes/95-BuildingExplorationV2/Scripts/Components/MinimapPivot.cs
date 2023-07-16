using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
using Microsoft.MixedReality.Toolkit.UI.BoundsControlTypes;

public class MinimapPivot : MonoBehaviour
{
    // ===== GUI ===== //

    [Header("Main Settings")]
    [Tooltip("Minimap main root object reference")]
    public GameObject MapRoot = null;
    [Tooltip("Reference to the box collider of the minimap")]
    public BoxCollider BoxReference = null;
    [Tooltip("Minimap Pivot reference")]
    public GameObject MapPivot = null;
    [Tooltip("Is update active?")]
    public bool SwitchUpdate = true;
    [Tooltip("(test) Apply pivot displace?")]
    public bool ApplyDisplacement = true;
    [Tooltip("Scaling factor for the main object")]
    [Min(0.0f)]
    public float ScaleFactor = 100.0f;

    [Header("Minimap Size")]
    [Tooltip("Overall space starting from a cube vertex")]
    public Vector3 SpaceSize = Vector3.one;

    [Header("Debug Zone")]
    public bool dbZone = true;
    public Vector3 averageVector = Vector3.zero;
    public Vector3 spacePoint = Vector3.zero;
    public Vector3 displaceVector = Vector3.zero;



    // ===== PRIVATE ===== //

    // ...
    private BoundsControl boundsControl = null;

    

    // ===== UNITY CALLBACKS ===== //

    private void Start()
    {
        BoxReference.size = SpaceSize;
        BoxReference.center = 0.5f * SpaceSize;
        boundsControl = MapRoot.AddComponent<BoundsControl>();
        boundsControl.CalculationMethod = BoundsCalculationMethod.ColliderOnly;
    }

    private void Update()
    {
        if (!SwitchUpdate) MapPivot.transform.localPosition = Vector3.zero;

        Vector3 avgPos = ComputeAveragePosition();
        averageVector = avgPos;
        Vector3 spaceCenter = 0.5f * SpaceSize;
        spacePoint = spaceCenter;

        displaceVector = spaceCenter - avgPos;
        if(ApplyDisplacement)
            // MapPivot.transform.localPosition = MapRoot.transform.localPosition + (spaceCenter - avgPos);
            MapPivot.transform.localPosition = (spaceCenter - avgPos);

        MapRoot.transform.localScale = (ScaleFactor / 100.0f) * Vector3.one;
    }



    // ===== FEATURE COMPUTE AVERAGE POSITION ===== //

    private Vector3 ComputeAveragePosition()
    {
        Vector3 avgPos = Vector3.zero;
        int N = 0;
        foreach(Transform t in MapPivot.transform)
        {
            avgPos += t.localPosition;
            ++N;
        }

        return ( N > 0 ? (1.0f / ((float)N)) * avgPos : Vector3.zero );
    }
}
