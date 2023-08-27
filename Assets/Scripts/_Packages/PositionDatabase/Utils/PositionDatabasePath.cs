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

        public string KeyStable
        {
            get => wp1.KeyStable + "_" + wp2.KeyStable;
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



        // ===== PUBLIC METHODS ===== //

        public PositionDatabasePath()
        {
            
        }



        // ===== FEATURE EXPORT DATA ===== //

        // export as JSON item
        public string ToJson()
        {
            return "{" + $"'key':'{this.Key}' , 'wp1':'{this.wp1.Key}' , 'wp2':'{this.wp2.Key}'" + "}";
        }

        // export as CSV item
        public List<string> ToCsv(bool header=false)
        {
            if (header)
                return new List<string> { "key", "wp1", "wp2" };
            else
                return new List<string> { this.Key, this.wp1.Key, this.wp2.Key };
        }
    }
}