using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using SaR4Hololens2.Scenes.BuildingExplorationV2.Scripts.Utils;

namespace SaR4Hololens2.Scenes.BuildingExplorationV2.Scripts.Components
{
    public class PositionsDatabase : MonoBehaviour
    {
        // ===== GUI ===== //

        [Header("Positions Recording Settings")]
        [Tooltip("Base distance around a waypoint (in meters); each waypoint will be distant to another one by two times this value. ")]
        public float BaseDistance = 1.0f;
        [Tooltip("Tolerance in collecting the measurements (in meters). ")]
        public float DistanceTolerance = 0.01f;

        [Header("Dynamic Sort Settings")]
        [Tooltip("The object against which to make the sort; main camera is used if ReferenceObject is null")]
        public GameObject ReferenceObject = null;
        [Tooltip("The list of positions to keep sorted can be divived into chunks called Clusters")]
        public bool UseClusters = false;
        [Tooltip("(Only if UseClusters is true) the size of the cluster starting from 3")]
        [Min(4)]
        public int ClusterSize = 4;
        [Tooltip("Each cluster has its index, but sometimes updating too many indices could get performances worse. In these situations, having a max number of indices could be helpful")]
        public bool UseMaxIndices = false;
        [Tooltip("(Only with UseMaxIndices checked) the maximum number of indices to create; it corresponds to the maximum number of clusters")]
        [Min(2)]
        public int MaxIndices = 2;

        [Header("DB Events")]
        [Tooltip("Events called when a new position is created")]
        public List<UnityEvent> CallOnZoneCreated = new List<UnityEvent>();
        [Tooltip("Events called when the current zone changes")]
        public List<UnityEvent> CallOnZoneChanged = new List<UnityEvent>();

        [Header("For in-editor Debugging mode")]
        public Vector3 debug_currentZoneVector;
        public bool debug_checkReferenceDistance = false;
        public float debug_distance = 0.0f;
        public bool debug_between = false;
        public bool debug_currentZoneIsNull = false;




        // ===== PUBLIC ===== //

        // the current zone
        public PositionDatabaseWaypoint CurrentZone
        {
            get => currentZone;
        }
        public PositionDatabaseWaypoint DataZoneCreated
        {
            get => dataZoneCreated;
        }



        // ===== PRIVATE ===== //

        // init done?
        private bool init = false;
        // reference object for te dynamic sort
        private GameObject goDynamicSortReference = null;
        // Object reference distance for dynamic sort
        private Vector3 sortReferencePosition = Vector3.zero;
        // the current zone 
        private PositionDatabaseWaypoint currentZone = null;
        private PositionDatabaseWaypoint prevZone = null;
        // used by notifications
        private PositionDatabaseWaypoint dataZoneCreated = null;

        // the database is a sem-ordered list of positions (the current zone "is" the first element of the list, best approximation)
        private class DatabaseList
        {
            private List<PositionDatabaseWaypoint> db = new List<PositionDatabaseWaypoint>();

            // I'm not sure the list is a true list... so, better to think it "in append" instead of in insert
            public PositionDatabaseWaypoint this[int i]
            {
                get 
                {
                    if (i < db.Count && i >= 0)
                        return db[db.Count - 1 - i];
                    else
                        return null;
                }
            }

            public int Count
            {
                get => db.Count;
            }

            public bool Set(int i, PositionDatabaseWaypoint wp)
            {
                
                if (i < db.Count && i >= 0)
                {
                    db[db.Count - 1 - i] = wp;
                    return true;
                }
                else
                    return false;
            }

            public PositionDatabaseWaypoint First { get => db.Count == 0 ? null : this[0]; }

            public void Insert(PositionDatabaseWaypoint wp)
            {
                db.Add(wp);
            }

            public bool Swap(int i, int j = -1)
            {
                if (j == -1) j = i+1;

                if (i < db.Count && i >= 0 && j < db.Count && j >= 0 && i != j)
                {
                    PositionDatabaseWaypoint wptemp = this[i];
                    Set(i, this[j]);
                    Set(j, wptemp);

                    return true;
                }
                else
                    return false;
            }
        }
        private DatabaseList db = new DatabaseList();

        private class DynamicSortSupport
        {
            public DatabaseList db = null; // reference to the database to sort

            public int ClusterLength = -1; // -1 if not used
            public bool UseCuster
            {
                get
                {
                    return ClusterLength > 0;
                }
                set
                {
                    if (value)
                        ClusterLength = 4;
                    else
                        ClusterLength = -1;
                }
            }

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
            public int MaxIndices = -1; // -1 if not used

            private int N = 0; // elements in the array
            private int np; // used for updating cluster size
            private List<int> idx = new List<int>() { 0 };
            private int idxMin = 0;
            private int idxMax = 0;

            public bool DynamicSort(Vector3 sortReferencePosition)
            {
                if (ClusterLength <= 3) return false;

                N = db.Count;
                if(MaxIndices > 0)
                    np = ClusterLength * MaxIndices;

                sortStep(sortReferencePosition);
                if (UseCuster) checkNewCluster();
                if (UseCuster && UseMaxIndices) checkMaxIdx();

                return true;
            }

            private void sortStep(Vector3 Puser)
            {
                int j = 0;
                for(int i=0; i<idx.Count; ++i)
                {
                    j = idx[i];

                    if (dist(Puser, db[j].AreaCenter) > dist(Puser, db[j + 1].AreaCenter))
                        db.Swap(j, j + 1);
                    
                    idx[i] = (j + 1) % (db.Count - 1);
                    if( idx[i] < j )
                    {
                        idxMin = i;
                        idxMax = (i > 0 ? i - 1 : idx.Count - 1);
                    }
                }
            }

            private void checkNewCluster()
            {
                if (UseMaxIndices && idx.Count >= MaxIndices) return;

                if (N > ClusterLength && N % ClusterLength == 3)
                    idx.Add( ((N-3) + idx[idxMax]) % (N-1) );
            }

            private void checkMaxIdx()
            {
                if( N - np == MaxIndices )
                {
                    ++ClusterLength;
                    np = N * ClusterLength;

                    for (int i = 0; i < idx.Count; ++i)
                        idx[i] = (idx[i] + 1) % (N - 1);
                }
            }

            private float dist(Vector3 pos1, Vector3 pos2)
            {
                return Vector3.Distance(pos1, pos2);
            }
        }
        private DynamicSortSupport dynSortData = new DynamicSortSupport();



        // ===== UNITY CALLBACKS ===== //

        private void Start()
        {
            goDynamicSortReference = (ReferenceObject == null ? Camera.main.gameObject : ReferenceObject);
            ReferenceObject = goDynamicSortReference;

            dynSortData.db = db;
            dynSortData.UseCuster = UseClusters;
            if (UseClusters) dynSortData.ClusterLength = ClusterSize;
            dynSortData.UseMaxIndices = UseMaxIndices;
            if (UseMaxIndices) dynSortData.MaxIndices = MaxIndices;

            init = true;
        }

        private void Update()
        {
            if (!init) return;

            updateReferenceObect();
            tryInsertPosition();
            dynamicSortStep();
            onZoneChange();
        }



        // ===== EVENTS ===== //

        // ...



        // ===== PUBLIC CLASS METHODS ===== //

        // ...



        // ===== PRIVATE METHODS ===== //

        private void updateReferenceObect()
        {
            if(goDynamicSortReference != ReferenceObject)
            {
                goDynamicSortReference = (ReferenceObject == null ? Camera.main.gameObject : ReferenceObject);
                ReferenceObject = goDynamicSortReference;
            }
            sortReferencePosition = goDynamicSortReference.transform.position;
        }

        private void tryInsertPosition()
        {
            if (!init) return;

            // DEBUG ZONE
            debug_checkReferenceDistance = checkReferenceDistance();
            // DEBUG ZONE

            if (currentZone == null || checkReferenceDistance())
                insertPosition();
            currentZone = db.First;

            // DEBUG ZONE
            debug_currentZoneVector = currentZone.AreaCenter;
            debug_currentZoneIsNull = currentZone == null;
            Debug.Break();
            // DEBUG ZONE


            onZoneCreated();
        }

        private void insertPosition()
        {
            if (!init) return;

            PositionDatabaseWaypoint wp = new PositionDatabaseWaypoint();
            wp.AreaCenter = sortReferencePosition;
            wp.AreaRadius = BaseDistance;
            wp.DBReference = this;

            db.Insert(wp);
            
            if (currentZone != null)
                wp.AddPath(currentZone);
        }

        private bool checkReferenceDistance()
        {
            if (!init || currentZone == null || db.Count == 0) return false;

            // DEBUG ZONE
            debug_distance = Vector3.Distance(currentZone.AreaCenter, sortReferencePosition);
            debug_between = between(
                Vector3.Distance(currentZone.AreaCenter, sortReferencePosition),
                2.0f * BaseDistance - DistanceTolerance, 2.0f * BaseDistance + DistanceTolerance,
                strict: false
            );
            // DEBUG ZONE

            return between(
                Vector3.Distance(currentZone.AreaCenter, sortReferencePosition),
                2.0f*BaseDistance - DistanceTolerance, 2.0f * BaseDistance + DistanceTolerance,
                strict: false
            );
        }

        private void dynamicSortStep()
        {
            dynSortData.DynamicSort(sortReferencePosition);
        }

        private void onZoneCreated()
        {
            dataZoneCreated = db.First;
            foreach (UnityEvent ue in CallOnZoneCreated)
                ue.Invoke();
        }

        private void onZoneChange()
        {
            if(prevZone != currentZone)
            {
                foreach (UnityEvent ue in CallOnZoneChanged)
                    ue.Invoke();

                prevZone = currentZone;
            }
        }

        private bool between(float val, float a, float b, bool strict = false)
        {
            if (strict)
                return (val > a || val < b);
            else
                return (val >= a || val <= b);
        }
    }

}