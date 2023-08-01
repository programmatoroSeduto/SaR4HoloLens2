using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Packages.PositionDatabase.Components;
using Packages.PositionDatabase.Utils;
using Packages.StorageManager.Components;

namespace Packages.SarExplorationFeatures.Components
{
    public class FeatureBase : MonoBehaviour
    {
        // ===== PUBLIC ===== //

        [HideInInspector] // reference to the database
        public PositionsDatabase DbReference = null;
        [HideInInspector] // reference to the path drawer
        public PathDrawer DrawerReference = null;

        public virtual bool IsRunning
        {
            get
            {
                return isRunning;
            }

            set
            {
                isRunning = value;
                this.enabled = value;
            }
        }



        // ===== PRIVATE ===== //

        // either the feature is runningor not
        protected bool isRunning = false;

    }
}
