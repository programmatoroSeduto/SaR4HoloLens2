using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Packages.PositionDatabase.Utils
{
    public class PositionDatabasePath
    {
        // ===== PUBLIC ===== //

        public PositionDatabaseWaypoint wp1 = null;
        public PositionDatabaseWaypoint wp2 = null;

        public bool HasRenderer
        {
            get => (Renderer != null);
        }

        public MonoBehaviour Renderer = null;

        public float Distance
        {
            get
            {
                Vector3 pos1 = wp1.AreaCenter;
                Vector3 pos2 = wp2.AreaCenter;
                return Vector3.Distance(pos1, pos2);
            }
        }

        public string Key
        {
            get => wp1.Key + "_" + wp2.Key;
        }



        // ===== PUBLIC METHODS ===== //

        public PositionDatabasePath(PositionDatabaseWaypoint wpFrom = null, PositionDatabaseWaypoint wpTo = null)
        {
            wp1 = wpFrom;
            wp2 = wpTo;
        }

        public PositionDatabaseWaypoint Next(PositionDatabaseWaypoint wpFrom)
        {
            if (wpFrom == null) return null;

            return (wp1 == wpFrom ? wp2 : wp1);
        }
    }
}