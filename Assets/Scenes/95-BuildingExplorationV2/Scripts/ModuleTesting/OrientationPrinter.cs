using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OrientationPrinter : MonoBehaviour
{
    // ===== GUI ===== //

    [Header("Debug Zone")]
    public Vector3 orientation = Vector3.zero;
    public float VerticalAngle = 0.0f;



    // ===== UNITY CALLBACKS ===== //

    // Update is called once per frame
    void Update()
    {
        orientation = Camera.main.transform.rotation.eulerAngles;
        VerticalAngle = orientation.y;
    }



}
