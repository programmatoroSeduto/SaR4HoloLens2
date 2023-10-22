using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Packages.PositionDatabase.Utils;
using Packages.DiskStorageServices.Components;
using System.Threading.Tasks;

namespace Packages.PositionDatabase.Components
{
    public class PositionsDatabase : ProjectMonoBehaviour
    {
        // ===== GUI ===== //

        [Header("Positions Recording Settings")]
        [Tooltip("Base distance around a waypoint (in meters); each waypoint will be distant to another one by two times this value. ")]
        public float BaseDistance = 0.5f;
        [Tooltip("Height of the cilinder around one waypoint")]
        public float BaseHeight = 0.8f;
        [Tooltip("Tolerance in collecting the measurements (in meters). ")]
        public float DistanceTolerance = 0.05f;

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
        // (not working so far)
        //[Tooltip("First Level Optimization: the database tries to move also the linked position to the nearest one when feasible; it decreases a bit the performances, but also allows a better position chasing")]
        // public bool UseFirstLevelOptimization = true;


        [Header("DB Events")]
        [Tooltip("Events called when a new position is created")]
        public List<UnityEvent> CallOnZoneCreated = new List<UnityEvent>();
        [Tooltip("Events called when the current zone changes")]
        public List<UnityEvent> CallOnZoneChanged = new List<UnityEvent>();



        // ===== PUBLIC ===== //

        // the DB low level reference
        public PositionDatabaseLowLevel LowLevelDatabase
        {
            get => lowLevel;
        }

        // the current zone
        public PositionDatabaseWaypoint CurrentZone
        {
            get => lowLevel.CurrentZone;
        }

        // the last inserted zone
        public PositionDatabaseWaypoint DataZoneCreated
        {
            get => dataZoneCreated;
        }

        // the area renaming structure
        public Dictionary<int, int> AreaRenamingLookup
        {
            get => AreaRenaming;
            set
            {
                AreaRenaming = value;
            }
        }
        
        // percentage of hits
        public float HitPercent
        {
            get => hitMissTotalCount > 0 ? (float)(hitCount / hitMissTotalCount) : 0.0f;
        }

        // percentage of misses
        public float MissPercent
        {
            get => hitMissTotalCount > 0 ? (float)(missCount / hitMissTotalCount) : 0.0f;
        }



        // ===== PRIVATE ===== //

        // not exported reference to the low level DB
        private PositionDatabaseLowLevel lowLevel = new PositionDatabaseLowLevel();
        // used for the Update event
        private PositionDatabaseWaypoint prevZone = null;
        // used by notifications
        private PositionDatabaseWaypoint dataZoneCreated = null;
        // active when the database is importing infos from file
        private bool isImporting = false;
        // the object which is importing data
        private MonoBehaviour activeImportUtility = null;
        // type of insert
        private bool linkedInsert = false;
        private bool unlinkedInsert = false;
        // area index
        private int areaIndex = 0;
        // active when the class has to reorder the daabase due to a previous deactivation
        private bool needSort = false;
        // the coroutine sorting the class when it is re-enabled
        private Coroutine COR_SortAfterEnable = null;
        // area efficient renaming
        private Dictionary<int, int> AreaRenaming = new Dictionary<int, int>();
        // total count
        private double hitMissTotalCount = 0.0;
        // hit count
        private double hitCount = 0.0;
        // miss count
        private double missCount = 0.0;



        // ===== UNITY CALLBACKS ===== //

        private void Start()
        {
            // cluster optimization
            lowLevel.UseCluster = UseClusters;
            if (UseClusters)
                lowLevel.ClusterLength = ClusterSize;
            else
                lowLevel.ClusterLength = -1;

            // max indices optimization
            lowLevel.UseMaxIndices = UseMaxIndices;
            if (UseMaxIndices) 
                lowLevel.MaxIndices = MaxIndices;
            else
                lowLevel.MaxIndices = -1;

            // first level optimization (not wrking)
            // lowLevel.UseFirstLevelOptimization = UseFirstLevelOptimization;

            // area division algorithm first setup
            AreaRenaming.Add(0, 0);

            Ready();
        }

        private void Update()
        {
            if (isImporting || needSort) return;

            updateReferenceObject();
            lowLevel.SortStep();
            tryInsertPosition();
            onZoneChanged();
        }

        public void OnDisable()
        {
            needSort = true;   
        }

        public void OnEnable()
        {
            if(needSort)
                COR_SortAfterEnable = StartCoroutine(BSCOR_SortAll());
        }

        public void OnEnable(bool forceSort = false)
        {
            if (forceSort || needSort)
                COR_SortAfterEnable = StartCoroutine(BSCOR_SortAll());
        }

        private IEnumerator BSCOR_SortAll()
        {
            yield return null;

            updateReferenceObject();
            lowLevel.SortAll();

            needSort = false;
            COR_SortAfterEnable = null;
        }



        // ===== REFERENCE POSITION ===== //

        private void updateReferenceObject()
        {
            if (ReferenceObject == null)
                lowLevel.SortReferencePosition = Camera.main.transform.position;
            else
                lowLevel.SortReferencePosition = ReferenceObject.transform.position;
        }



        // ===== POSITION INSERT ===== //

        private void tryInsertPosition()
        {
            if (checkReferenceDistance())
            {
                insertPosition();
                onZoneCreated();
            }
        }

        private void insertPosition()
        {
            PositionDatabaseWaypoint wp = new PositionDatabaseWaypoint();
            wp.DBReference = this;
            
            //  TODO: rework this feature (areas currently not working)
            /*
            if (unlinkedInsert && !linkedInsert)
            {
                ++areaIndex;
                AreaRenaming.Add(areaIndex, areaIndex);
            }
            */
            // wp.AreaIndex = AreaRenaming[areaIndex];
            
            wp.AreaIndex = 0;
            wp.setPositionID(lowLevel.GetSharedIndex());
            wp.AreaCenter = lowLevel.SortReferencePosition;
            wp.AreaRadius = BaseDistance;

            //  TODO: rework this feature (areas currently not working)
            //if (CurrentZone != null && !unlinkedInsert)
            if (CurrentZone != null)
            {
                wp.AddPath(CurrentZone);
            }

            lowLevel.Insert(wp);
        }

        /// <summary>
        /// this method checks when the distance condition is satisfied to insert a new position
        /// </summary>
        /// <returns> true if it is time to add a new waypoint to the database </returns>
        private bool checkReferenceDistance()
        {
            linkedInsert = false;
            unlinkedInsert = false;

            if (CurrentZone == null) return true;

            linkedInsert = 
                checkLinkedInsert(CurrentZone.AreaCenter, lowLevel.SortReferencePosition, BaseDistance, BaseHeight, DistanceTolerance);
            
            unlinkedInsert = 
                checkUnlinkedInsert(CurrentZone.AreaCenter, lowLevel.SortReferencePosition, BaseDistance, BaseHeight, DistanceTolerance);

            return linkedInsert || unlinkedInsert;
        }

        private bool checkLinkedInsert(Vector3 curP, Vector3 refP, float baseDist, float baseH, float toll)
        {
            float planeDist =
                (curP.x - refP.x) * (curP.x - refP.x) +
                (curP.z - refP.z) * (curP.z - refP.z);

            float verticalDist = curP.y - refP.y;

            return 
                between(planeDist, 2.0f * baseDist - toll, 2.0f * baseDist + toll, strict: false) ||
                between(verticalDist, 2.0f * baseH - toll, 2.0f * baseH + toll, strict: false);
        }

        private bool checkUnlinkedInsert(Vector3 curP, Vector3 refP, float baseDist, float baseH, float toll)
        {
            float planeDist =
                (curP.x - refP.x) * (curP.x - refP.x) +
                (curP.z - refP.z) * (curP.z - refP.z);

            float verticalDist = curP.y - refP.y;

            return (planeDist > 2.0f * baseDist + toll) || (verticalDist > 2.0f * baseH + toll);
        }

        private bool between(float val, float a, float b, bool strict = false)
        {
            if (strict)
                return (val > a && val < b);
            else
                return (val >= a && val <= b);
        }



        // ===== EVENTS MANAGEMENT AND UPDATE ===== //

        private void onZoneCreated()
        {
            dataZoneCreated = CurrentZone;
            foreach (UnityEvent ue in CallOnZoneCreated)
                ue.Invoke();
            
            if (linkedInsert || unlinkedInsert)
            {
                ++hitMissTotalCount;
                ++missCount;
            }
        }

        private void onZoneChanged()
        {
            if (prevZone != CurrentZone)
            {
                if (prevZone != null && 
                    !unlinkedInsert &&
                    AreaRenaming[prevZone.AreaIndex] != AreaRenaming[CurrentZone.AreaIndex] && 
                    !prevZone.IsLinkedWith(CurrentZone)
                    )
                {
                    prevZone.AddPath(CurrentZone);
                    AreaRenaming[prevZone.AreaIndex] = CurrentZone.AreaIndex;
                }
                if(prevZone != null)
                    prevZone.AreaIndex = AreaRenaming[prevZone.AreaIndex];
                CurrentZone.AreaIndex = AreaRenaming[CurrentZone.AreaIndex];

                foreach (UnityEvent ue in CallOnZoneChanged)
                    ue.Invoke();

                prevZone = CurrentZone;

                if (!linkedInsert && !unlinkedInsert)
                {
                    ++hitMissTotalCount;
                    ++hitCount;
                }
            }
        }



        // ===== IMPORT EXPORT SUPPORT ===== //

        public bool SetStatusImporting(MonoBehaviour who, bool opt)
        {
            if (isImporting && opt) return false;

            if (opt)
                activeImportUtility = who;
            else if (!opt && activeImportUtility == who)
                activeImportUtility = null;
            else
                return false;

            isImporting = opt;
            return true;
        }



        // ===== POSITION QUERY SUPPORT ===== //

        public List<PositionDatabaseWaypoint> GetNearestWaypoints(float maxDistance = float.MaxValue, int maxItems = int.MaxValue)
        {
            List<PositionDatabaseWaypoint> res = new List<PositionDatabaseWaypoint>();
            PositionDatabaseWaypoint wpCur = lowLevel.CurrentZone;

            foreach (PositionDatabaseWaypoint wp in lowLevel.Database)
            {
                if (Vector3.Distance(wpCur.AreaCenter, wp.AreaCenter) > maxDistance || res.Count > maxItems)
                    break;
                else
                    res.Add(wp);
            }

            return res;
        }
    }

}