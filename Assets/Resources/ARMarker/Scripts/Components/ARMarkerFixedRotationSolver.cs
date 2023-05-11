using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Microsoft.MixedReality.Toolkit.Utilities.Solvers;








namespace Packages.VisualItems.ARMarker.Components
{
    public class ARMarkerFixedRotationSolver : Solver
    {
        [Header("Max Distance Feature")]
        [Tooltip("Use Max Distance from the point")]
        public bool UseMaxDistance = true;

        [Tooltip("Max Distance (if enabled)")]
        public float MaxDistanceFromPoint = 1.5f;

        public override void SolverUpdate()
        {
            if (SolverHandler != null && SolverHandler.TransformTarget != null)
            {
                Transform target = SolverHandler.TransformTarget;

                Vector3 ray = this.transform.position - target.position;

                if( ray.magnitude < MaxDistanceFromPoint || !UseMaxDistance )
                    GoalRotation = Quaternion.LookRotation( new Vector3( ray.x, 0.0f, ray.z ), Vector3.up );
            }
        }
    }
}

