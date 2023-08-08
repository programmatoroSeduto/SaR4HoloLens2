using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Packages.PositionDatabase.Utils;
using Packages.DiskStorageServices.Components;
using System.Threading;
using System.Threading.Tasks;

namespace Packages.PositionDatabase.Components
{
    public class PositionsDatabase : MonoBehaviour
    {
        // ===== GUI ===== //

        [Header("Positions Recording Settings")]
        [Tooltip("Base distance around a waypoint (in meters); each waypoint will be distant to another one by two times this value. ")]
        public float BaseDistance = 1.0f;
        [Tooltip("Height of the cilinder around one waypoint")]
        public float BaseHeight = 0.8f;
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

        [Header("Import Export settings")]
        [Tooltip("reference to the writer")]
        public StorageHubOneShot WriterReference = null;



        // ===== PUBLIC ===== //

        // the current zone
        public PositionDatabaseWaypoint CurrentZone
        {
            get => (db.Count == 0 ? null : db[0]);
        }
        public PositionDatabaseWaypoint DataZoneCreated
        {
            get => dataZoneCreated;
        }



        // ===== PRIVATE ===== //

        // init done?
        private bool init = false;
        // Object reference distance for dynamic sort
        private Vector3 sortReferencePosition = Vector3.zero;
        // the current zone 
        private PositionDatabaseWaypoint currentZone = null;
        private PositionDatabaseWaypoint prevZone = null;
        // used by notifications
        private PositionDatabaseWaypoint dataZoneCreated = null;
        // active when the database is importing infos from file
        private bool isImporting = false;

        // the database is a sem-ordered list of positions (the current zone "is" the first element of the list, best approximation)
        private List<PositionDatabaseWaypoint> db = new List<PositionDatabaseWaypoint>();

        private class DynamicSortSupport
        {
            public List<PositionDatabaseWaypoint> db = null; // reference to the database to sort

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
            public int WorkingClusters
            {
                get => idx.Count;
            }

            public IReadOnlyList<int> WorkingIndices
            {
                get => idx;
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
            private int np = 0; // used for updating cluster size
            private List<int> idx = new List<int>();
            private int idxMax = 0;

            public void Reset(int cluster=-1, int maxIdx=-1)
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

            public void DynamicSort(Vector3 sortReferencePosition)
            {
                if (db.Count < 2) return;
                if (UseCuster && ClusterLength <= 3) return;

                if (idx.Count == 0) idx.Add(0);

                N = db.Count;
                if (MaxIndices > 0)
                    np = ClusterLength * MaxIndices;

                sortStep(sortReferencePosition);
                if (UseCuster) checkNewCluster();
                if (UseCuster && UseMaxIndices) checkMaxIdx();
            }

            private void sortStep(Vector3 Puser)
            {
                int j = 0;
                for (int i = 0; i < idx.Count; ++i)
                {
                    j = idx[i];
                    if (dist(Puser, db[j].AreaCenter) > dist(Puser, db[j + 1].AreaCenter))
                        swap(j, j + 1);

                    idx[i] = (j + 1) % (N - 1);
                    if (idx.Count > 1 && idx[i] < j)
                        redistributeIdx();
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
        }
        private DynamicSortSupport dynSortData = new DynamicSortSupport();



        // ===== UNITY CALLBACKS ===== //

        private void Start()
        {
            dynSortData.db = db;
            dynSortData.UseCuster = UseClusters;
            if (UseClusters) dynSortData.ClusterLength = ClusterSize;
            dynSortData.UseMaxIndices = UseMaxIndices;
            if (UseMaxIndices) dynSortData.MaxIndices = MaxIndices;

            init = true;
        }

        private void Update()
        {
            if (isImporting) return;

            tryInsertPosition();
            updateReferenceObject();
            dynamicSortStep();
            onZoneChange();
        }



        // ===== FEATURE IMPORT EXPORT ===== //

        public void EVENT_ExportJson()
        {
            if (WriterReference == null) return;

            JSONPositionDatabase dump = new JSONPositionDatabase(this);

            foreach(PositionDatabaseWaypoint wp in db)
            {
                JSONWaypoint jsonWp = new JSONWaypoint(wp);

                foreach(PositionDatabasePath link in wp.Paths)
                {
                    if(link.Key.StartsWith(wp.Key))
                    {
                        jsonWp.Paths.Add(new JSONPath(link));
                    }
                }
                dump.Waypoints.Add(jsonWp);
            }

            WriterReference.WriteOneShot("db_export", "json", JsonUtility.ToJson(dump), useTimestamp: false);
        }

        public void EVENT_ImportJson(bool fullRefresh = false)
        {
            StartCoroutine(COR_ImportJson());
        }

        public IEnumerator COR_ImportJson(bool fullRefresh = false)
        {
            yield return null;
            isImporting = true;

            if (fullRefresh)
            {
                // DA RIVEDERE -- molto pericolosa
                db.Clear();
            }

            yield return StartCoroutine(WriterReference.ReadOneShot("db_export.json"));
            if(!WriterReference.FileReadSuccess)
            {
                isImporting = false;
                yield break;
            }

            JSONPositionDatabase jdb = JsonUtility.FromJson<JSONPositionDatabase>(WriterReference.FileContent);
            jdb.SetDatabase(this);

            Dictionary<int, PositionDatabaseWaypoint> wpDict = new Dictionary<int, PositionDatabaseWaypoint>();
            Dictionary<int, PositionDatabasePath> waitingLinks = new Dictionary<int, PositionDatabasePath>();
            foreach(JSONWaypoint wp in jdb.Waypoints)
            {
                PositionDatabaseWaypoint dbwp = wp.FromJsonWaypoint();

                foreach(JSONPath link in wp.Paths)
                {
                    PositionDatabasePath dblink = new PositionDatabasePath();
                    dblink.wp1 = dbwp;
                    if (wpDict.ContainsKey(link.Waypoint2))
                    {
                        dblink.wp2 = wpDict[link.Waypoint2];
                        dbwp.AddPath(wpDict[link.Waypoint2]);
                    }
                    else
                    {
                        waitingLinks.Add(link.Waypoint2, dblink);
                    }
                        
                }

                wpDict.Add(wp.PositionID, dbwp);
                db.Add(dbwp);
            }
            foreach(KeyValuePair<int, PositionDatabasePath> unresolved in waitingLinks)
            {
                unresolved.Value.wp2 = wpDict[unresolved.Key];
                wpDict[unresolved.Key].AddPath(unresolved.Value.wp2);
            }

            dynSortData.Reset();
            yield return BSCOR_SortAll();
            currentZone = db[0];
            onZoneChange();

            isImporting = false;
        }



        // ===== PUBLIC METHODS ===== //

        public void SortAll()
        {
            if (sortReferencePosition != null)
                db.Sort((wp1, wp2) => { 
                    updateReferenceObject(); 
                    return Vector3.Distance(wp1.AreaCenter, sortReferencePosition).CompareTo(Vector3.Distance(wp2.AreaCenter, sortReferencePosition)); 
                });
        }

        public IEnumerator BSCOR_SortAll()
        {
            yield return null;

            Task t = new Task(() =>
            {
                if (sortReferencePosition != null)
                    db.Sort((wp1, wp2) =>
                    {
                        updateReferenceObject();
                        return Vector3.Distance(wp1.AreaCenter, sortReferencePosition).CompareTo(Vector3.Distance(wp2.AreaCenter, sortReferencePosition));
                    });
            });

            while (!t.IsCompleted)
                yield return new WaitForEndOfFrame();
        }



        // ===== PRIVATE METHODS ===== //

        private void updateReferenceObject()
        {
            if (ReferenceObject == null)
                sortReferencePosition = Camera.main.transform.position;
            else
                sortReferencePosition = ReferenceObject.transform.position;

            if (sortReferencePosition == null)
                Debug.LogError("sortReferencePosition == null");

            currentZone = CurrentZone;
        }

        private void tryInsertPosition()
        {
            if (!init) return;

            bool distCheck = checkReferenceDistance();
            if (currentZone == null || distCheck)
            {
                insertPosition();
                onZoneCreated();
            }
        }

        private void insertPosition()
        {
            if (!init) return;

            PositionDatabaseWaypoint wp = new PositionDatabaseWaypoint();
            wp.setPositionID(db.Count);
            wp.AreaCenter = sortReferencePosition;
            wp.AreaRadius = BaseDistance;
            wp.DBReference = this;

            db.Insert(0, wp);

            if (currentZone != null)
                wp.AddPath(currentZone);

            currentZone = wp;
        }

        private bool checkReferenceDistance()
        {
            if (!init || currentZone == null || db.Count == 0) return false;

            float planeDist = (currentZone.AreaCenter.x - sortReferencePosition.x) * (currentZone.AreaCenter.x - sortReferencePosition.x) + (currentZone.AreaCenter.z - sortReferencePosition.z) * (currentZone.AreaCenter.z - sortReferencePosition.z);

            return between(
                planeDist,
                2.0f * BaseDistance - DistanceTolerance, 2.0f * BaseDistance + DistanceTolerance,
                strict: false ) || 
                between(
                currentZone.AreaCenter.y - sortReferencePosition.y,
                2.0f * BaseHeight - DistanceTolerance, 2.0f * BaseHeight + DistanceTolerance,
                strict: false );
        }

        private void dynamicSortStep()
        {
            if (sortReferencePosition != null)
                dynSortData.DynamicSort(sortReferencePosition);
        }

        private void onZoneCreated()
        {
            dataZoneCreated = currentZone;
            foreach (UnityEvent ue in CallOnZoneCreated)
                ue.Invoke();
        }

        private void onZoneChange()
        {
            if (prevZone != currentZone)
            {
                foreach (UnityEvent ue in CallOnZoneChanged)
                    ue.Invoke();

                prevZone = currentZone;
            }
        }

        private bool between(float val, float a, float b, bool strict = false)
        {
            if (strict)
                return (val > a && val < b);
            else
                return (val >= a && val <= b);
        }
    }

}