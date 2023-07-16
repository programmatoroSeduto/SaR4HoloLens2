using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;

public class MinimapSolver : Solver
{
    // ===== GUI ===== //

    [Header("Minimap options")]
    [Tooltip("Position offset")]
    public Vector3 PositionOffset = new Vector3(0.0f, -0.25f, 0.5f);
    [Tooltip("Rotation Offset")]
    public Vector3 RotationOffset = new Vector3(-45.0f, 0.0f, 0.0f);
    [Tooltip("Limits for the x rotation")]
    public float RotationMaxX = 15.0f;



    // ===== SOLVER FUNCTIONS ===== //

    public override void SolverUpdate()
    {
        if (SolverHandler != null && SolverHandler.TransformTarget != null)
        {
            Transform target = SolverHandler.TransformTarget;

            // rotation cap
            Vector3 euler = target.rotation.eulerAngles;
            Quaternion rot = Quaternion.Euler((Mathf.Abs(euler.x) > RotationMaxX ? (euler.x > 0 ? +1 : -1) * RotationMaxX : euler.x), euler.y, euler.z);

            GoalPosition = target.position + rot * PositionOffset;
            GoalRotation = rot * Quaternion.Euler(RotationOffset);
        }
    }
}
