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
        /// <summary> first level optimization </summary>
        //public bool UseFirstLevelOptimization = true;




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

        /// <summary> (performnces metric) average busy time </summary>
        public float AverageBusyTime
        {
            get => (busyCalls > 1.0 ? (float)(busyTime / busyCalls) : 0.0f);
        }

        /// <summary> (performnces metric) maximum of busy time </summary>
        public float MaxBusyTime
        {
            get => (float)(busyMaxTime);
        }

        /// <summary> (performnces metric) average number of swaps per step </summary>
        public float AverageSwapPerCall
        {
            get => (busyCalls > 1.0 ? (float)(swapCalls / busyCalls) : 0.0f);
        }





        // ===== PRIVATE ===== //

        // reference position for the sorting
        private Vector3 sortReferencePosition = Vector3.zero;
        // inernal positions database list
        private List<PositionDatabaseWaypoint> db = new List<PositionDatabaseWaypoint>();
        // list of currently enabled sorting indices
        private List<int> idx = new List<int>();
        // value used for updating the size of the clusters
        private int np = 0;
        // the max len of the array given MaxIdx and Clusters Len
        private int idxMax = 0;

        // used by first level optimization
        // private Dictionary<string, int> storageIndexLookup = new Dictionary<string, int>();
        
        // performances evaluation
        // total time in milliseconds spent for ordering the array
        private double busyTime = 0.0;
        // total number of iterations
        private double busyCalls = 0.0;
        // last step start time
        private DateTime startTime = DateTime.Now;
        // max step busy time
        private double busyMaxTime = 0.0;
        // total swap calls
        private double swapCalls = 0.0;




        // ===== UTILITY FUNCTIONS ===== //

        public void Reset() => Reset(cluster: ClusterLength, maxIdx: MaxIndices);

        public void Reset(int cluster = -1, int maxIdx = -1)
        {
            // N = db.Count;
            MaxIndices = maxIdx;
            ClusterLength = cluster;

            idx.Clear();
            if (cluster > 0)
            {
                int cap = (int)Math.Floor(Math.Min((float)db.Count / (float)cluster, (maxIdx <= 0 ? int.MaxValue : maxIdx)));
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



        // ===== SWAP FUNCTIONS ===== //

        private void swap(int i, int j)
        {
            swapCalls += 1.0;

            /*
            if(UseFirstLevelOptimization)
            {
                setStorageKeys(db[i].KeyStable, j);
                setStorageKeys(db[j].KeyStable, i);
            }
            */

            PositionDatabaseWaypoint wpt = db[i];
            db[i] = db[j];
            db[j] = wpt;
        }

        /*
        private void setStorageKeys(string key, int idx)
        {
            if (!storageIndexLookup.ContainsKey(key))
                storageIndexLookup.Add(key, idx);
            else
                storageIndexLookup[key] = idx;
        }

        private int getStorageKey(string key)
        {
            return (storageIndexLookup.ContainsKey(key) ? storageIndexLookup[key] : -1);
        }
        */



        // ===== SINGLE STEP SORT ===== //

        public void SortStep( )
        {
            startTime = DateTime.Now;

            if (!((db.Count < 2) || (UseCluster && ClusterLength <= 3)))
            {
                if (idx.Count == 0) idx.Add(0);

                // N = db.Count;
                if (MaxIndices > 0)
                    np = ClusterLength * MaxIndices;

                dynamicSortStep(sortReferencePosition);

                if (UseCluster)
                {
                    checkNewCluster();
                    if (UseMaxIndices)
                        checkMaxIdx();
                }
            }

            updatePerformanceMetrics();
        }

        private void updatePerformanceMetrics()
        {
            double delta = (DateTime.Now - startTime).TotalMilliseconds;
            if (delta > busyMaxTime) busyMaxTime = delta;
            busyCalls += 1.0;
            busyTime += delta;
        }

        private void dynamicSortStep(Vector3 Puser)
        {
            int j = 0;
            for (int i = 0; i < idx.Count; ++i)
            {
                j = idx[i];
                if (dist(Puser, db[j].AreaCenter) > dist(Puser, db[j + 1].AreaCenter))
                {
                    swap(j, j + 1);

                    // first level optimization (not working)
                    /*
                     * l'ottimizzazione gioca sul fatto che, se ti trovi più vicino ad un certo punto,
                     * allora sarà più probabile successivamente che tu ti trovi nei pressi di quel punto.
                     * in questo modo l'inseguimento diventa più veloce perchè la qualità di indicizzazione aumenta.
                     * 
                     * non sswappo solo la singola posizione: avvicino anche tutte le posizioni vicine, perchè 
                     * saranno le più probabili in cui mi troverò più avanti. 
                     * 
                     * NOTA BENE (vedi la Insert) : potrebbe esserci un disallineamento temporaneo delle storage keys
                     * se si andasse ad inserire una stub storage key (a zero) al momento dell'inserimento. 
                     * Per questo preferisco che la chiave, se mancante, venga assegnata solo al momento dello swap
                     * in modo da evitare possibili problematiche legate alla occasionale incoerenza delle storage keys.
                     * 
                     * (nota: l'aspetto più preoccupante di questa frase è legato alla parola 'occasionale' ... il debug
                     * diventa subito un incubo per i bug occasionali, quindi meglio evitare cose strane.)
                     * */
                    /*
                    if(UseFirstLevelOptimization)
                    {
                        int storageIdx = -1;
                        int jnext = j + 1;
                        PositionDatabaseWaypoint wp = db[j];
                        foreach (PositionDatabasePath pt in db[j].Paths)
                        {
                            if (jnext >= db.Count) break;

                            storageIdx = getStorageKey(pt.Next(wp).KeyStable);
                            if (storageIdx >= 0)
                            {
                                swap(jnext, storageIdx);
                                ++jnext;
                            }
                            else break;
                        }
                    }
                    */
                }

                idx[i] = (j + 1) % (db.Count - 1);
                if (idx.Count > 1 && idx[i] < j)
                {
                    redistributeIdx();
                }
            }
        }

        private void checkNewCluster()
        {
            if (UseMaxIndices && idx.Count >= MaxIndices) return;

            if (db.Count >= (idx.Count + 1) * ClusterLength)
            {
                idx.Add(0);
                redistributeIdx();
            }
        }

        private void checkMaxIdx()
        {
            if (UseMaxIndices && idx.Count < MaxIndices) return;

            if (db.Count - np == MaxIndices)
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



        // ===== INSERT AND UPDATE ===== //

        public void Insert(PositionDatabaseWaypoint wp)
        {
            db.Insert(0, wp);
            WpIndex.Add(wp.PositionID, wp);
        }



        // ===== SHARED INDEX ===== //

        public int GetSharedIndex()
        {
            return ++MaxSharedIndex;
        }

    }
}