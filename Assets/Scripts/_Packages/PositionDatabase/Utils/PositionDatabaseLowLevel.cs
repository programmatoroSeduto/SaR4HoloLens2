using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;

using Packages.PositionDatabase.Types;
using Project.Scripts.Utils;

namespace Packages.PositionDatabase.Utils
{
    public class PositionDatabaseLowLevel : IPositionDatabaseLowLevel
    {
        // ===== PUBLIC ===== //

        /// <summary> the size of the cluster for the dynamic sort (-1 if not used) </summary>
        public int ClusterLength = -1;
        /// <summary> the maximum number of indices active for sorting the list (-1 if not used) </summary>
        public int MaxIndices = -1;
        /// <summary> A index table used for mapping indices and waypoints </summary>
        public Dictionary<int, PositionDatabaseWaypoint> WpIndex = new Dictionary<int, PositionDatabaseWaypoint>();
        /// <summary> used for assigning position IDs </summary>
        public int MaxSharedIndex = -1;
        



        // ===== PUBLIC GETTERS ===== //

        /// <summary> the database is a sem-ordered list of positions (the current zone "is" the first element of the list, best approximation) </summary>
        public List<PositionDatabaseWaypoint> Database { get => db; set { db = value; } }

        /// <summary> Object reference distance for dynamic sort </summary>
        public Vector3 SortReferencePosition { get => sortReferencePosition; set { sortReferencePosition = value; } }

        /// <summary> How many positions are handled by this class </summary>
        public int Count { get => db.Count; }

        /// <summary> if the MaxIndices feature is enabled or not; if activated, the number will be set to 10 by default </summary>
        public bool UseMaxIndices
        {
            get
            {
                return MaxIndices > 0;
            }
            set
            {
                if (value)
                    MaxIndices = 10;
                else
                    MaxIndices = -1;
            }
        }

        /// <summary> if the Clusters feature is enabled or not; if activated, the number will be set to 100 by default </summary>
        public bool UseCluster
        {
            get
            {
                return ClusterLength > 0;
            }
            set
            {
                if (value)
                    ClusterLength = 100;
                else
                    ClusterLength = -1;
            }
        }

        /// <summary> it returns the number of enabled clusters </summary>
        public int WorkingClusters
        {
            get => idx.Count;
        }

        /// <summary> it returns the read-only list of currently enabled indices </summary>
        public IReadOnlyList<int> WorkingIndices
        {
            get => idx;
        }

        /// <summary> it returns the current zone </summary>
        public PositionDatabaseWaypoint CurrentZone
        {
            get => (db.Count == 0 ? null : db[0]);
        }



        // ===== PRIVATE ===== //

        // reference position for the sorting
        private Vector3 sortReferencePosition = Vector3.zero;
        // inernal positions database list
        private List<PositionDatabaseWaypoint> db = new List<PositionDatabaseWaypoint>();
        // list of currently enabled sorting indices
        private List<int> idx = new List<int>();
        // it corresponds to the lenght of the array
        private int N = 0;
        // value used for updating the size of the clusters
        private int np = 0;
        // the max len of the array given MaxIdx and Clusters Len
        private int idxMax = 0;



        // ===== UTILITY FUNCTIONS ===== //

        public void Reset() => Reset(cluster: ClusterLength, maxIdx: MaxIndices);

        public void Reset(int cluster = -1, int maxIdx = -1)
        {
            N = db.Count;
            MaxIndices = maxIdx;
            ClusterLength = cluster;

            idx.Clear();
            if (cluster > 0)
            {
                int cap = (int)Math.Floor(Math.Min((float)N / (float)cluster, (maxIdx <= 0 ? int.MaxValue : maxIdx)));
                for (int i = 0; i < cap; ++i) idx.Add(0);
                redistributeIdx();
            }
        }

        private void redistributeIdx()
        {
            for (int i = 0; i < idx.Count; ++i)
                idx[i] = i * ClusterLength;
        }

        private float dist(Vector3 pos1, Vector3 pos2)
        {
            return Vector3.Distance(pos1, pos2);
        }

        private void swap(int i, int j)
        {
            PositionDatabaseWaypoint wpt = db[i];
            db[i] = db[j];
            db[j] = wpt;
        }



        // ===== SINGLE STEP SORT ===== //

        public void SortStep( )
        {
            if (db.Count < 2) return;
            if (UseCluster && ClusterLength <= 3) return;

            if (idx.Count == 0) idx.Add(0);

            N = db.Count;
            if (MaxIndices > 0)
                np = ClusterLength * MaxIndices;

            dynamicSortStep(sortReferencePosition);
            
            if (UseCluster) checkNewCluster();
            
            if (UseCluster && UseMaxIndices) checkMaxIdx();
        }

        private void dynamicSortStep(Vector3 Puser)
        {
            int j = 0;
            for (int i = 0; i < idx.Count; ++i)
            {
                j = idx[i];
                if (dist(Puser, db[j].AreaCenter) > dist(Puser, db[j + 1].AreaCenter))
                    swap(j, j + 1);

                idx[i] = (j + 1) % (N - 1);
                if (idx.Count > 1 && idx[i] < j)
                {
                    redistributeIdx();
                }
            }
        }

        private void checkNewCluster()
        {
            if (UseMaxIndices && idx.Count >= MaxIndices) return;

            if (N == (idx.Count + 1) * ClusterLength)
            {
                idx.Add(0);
                redistributeIdx();
            }
        }

        private void checkMaxIdx()
        {
            if (UseMaxIndices && idx.Count < MaxIndices) return;

            if (N - np == MaxIndices)
            {
                ++ClusterLength;
                np = ClusterLength * MaxIndices;
                redistributeIdx();
            }
        }



        // ===== ONESHOT SORT ===== //

        public void SortAll()
        {
            if (sortReferencePosition != null)
                db.Sort((wp1, wp2) => {
                    return Vector3.Distance(wp1.AreaCenter, sortReferencePosition).CompareTo(Vector3.Distance(wp2.AreaCenter, sortReferencePosition));
                });
        }

        public Task SortAllAsync()
        {
            return new Task(() => {
                SortAll();
            });
        }



        // ===== INSERT AND UPDATE ===== //

        public void Insert(PositionDatabaseWaypoint wp)
        {
            db.Insert(0, wp);
            // StaticLogger.Info("PositionDatabaseLowLevel:Insert", $"new waypoint with assigned ID:{wp.PositionID} with low level MAX_ID:{MaxSharedIndex}", logLayer: 2);
            /*
            if(WpIndex.ContainsKey(wp.PositionID))
            {
                if(WpIndex[wp.PositionID] == null)
                    StaticLogger.Info("PositionDatabaseLowLevel:Insert", $"waypoint {wp.PositionID}: entry exists and it is null (OK)", logLayer: 2);
                else
                    StaticLogger.Warn("PositionDatabaseLowLevel:Insert", $"waypoint {wp.PositionID}: entry exists, but IT IS NOT NULL!", logLayer: 2);
            }
            else
                StaticLogger.Info("PositionDatabaseLowLevel:Insert", $"waypoint {wp.PositionID} is new", logLayer: 2);
            */
            WpIndex.Add(wp.PositionID, wp);
        }



        // ===== SHARED INDEX ===== //

        public int GetSharedIndex()
        {
            return ++MaxSharedIndex;
        }

    }
}