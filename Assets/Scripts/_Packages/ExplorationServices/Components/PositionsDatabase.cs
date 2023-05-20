using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Packages.ExplorationServices.Utils;

namespace Packages.ExplorationServices.Components
{
    public class PositionsDatabase : MonoBehaviour
    {
        [Min(0.1f)]
        public float UpdatePeriod = 1.0f;

        [Min(0.0f)]
        public float MinItemsDistance = 1.0f;

        [Range(0.0f, 1.0f)]
        public float PercentListClosestPoint = 0.1f;

        [Min(2)]
        public int NumItemsPerIndex = 100;

        private List<PositionItem> db = new List<PositionItem>(); // ordinato (dinamicamente e parzialmente sulla base della distanza dall'attuale posizione)
        private List<PositionLink> links = new List<PositionLink>(); // non ordinato (ordinabile in base alla distanza)
        private List<PositionItem> records = new List<PositionItem>(); // ordinato cronologicamente (dal più vecchio al più recente)

        private int dbIdx = 0;
        private Vector3 currentPosition;
        private Coroutine COR_MapUpdate = null;
        private PositionItem currentZone = null;
        private int posIdx = 1;

        private int closestIdxNum = -1;
        private float closestIdxDist = float.MaxValue;




        private void Start()
        {
            COR_MapUpdate = StartCoroutine(ORCOR_MapUpdate());
        }

        private void Update()
        {
            currentPosition = Camera.main.transform.position;
            SortingStep();
        }

        public void EVENT_CreateMarker()
        {
            AddPositionItem(PositionItemType.Marker);
        }

        public void EVENT_CreateCheckpoint()
        {
            AddPositionItem(PositionItemType.Checkpoint);
        }










        private void SortingStep()
        {
            if (db.Count < 2) return;

            if (dbIdx >= db.Count-1) dbIdx = 0;

            int idx1 = dbIdx;
            int idx2 = dbIdx + 1;
            if (db[idx2].DistFromUser(currentPosition) < db[idx1].DistFromUser(currentPosition))
                Swap(idx1, idx2);

            ++dbIdx;
        }

        private void Swap(int idx1, int idx2)
        {
            if (idx1 < 0 || idx1 >= db.Count) return;
            if (idx2 < 0 || idx2 >= db.Count) return;

            PositionItem temp = db[idx1];
            db[idx1] = db[idx2];
            db[idx2] = temp;
        }

        private int FindClosestPoint(float percentList = 0.1f, float maxDistFromItem = float.MaxValue)
        {
            if (db.Count == 0) return -1;

            float mindist = Vector3.Distance(currentPosition, db[0].uP);
            int minIdx = 0;
            for (int i = 1; i < ( db.Count * PercentListClosestPoint >= 10 ? db.Count * PercentListClosestPoint : Mathf.Min(db.Count, 10) ); ++i)
            {
                float d = Vector3.Distance(currentPosition, db[i].uP);
                if (d < mindist)
                {
                    mindist = d;
                    minIdx = i;
                }
            }
            closestIdxNum = minIdx;
            closestIdxDist = mindist;

            if (mindist > maxDistFromItem)
                return -1;
            else
                return minIdx;
        }

        private IEnumerator ORCOR_MapUpdate()
        {
            if(currentZone == null)
            {
                currentZone = new PositionItem(Camera.main.transform.position, 0);
                db.Add(currentZone);
                records.Add(currentZone);
            }
            
            while(true)
            {
                yield return new WaitForSecondsRealtime(UpdatePeriod);

                int closestIdx = FindClosestPoint(PercentListClosestPoint, MinItemsDistance);

                PositionItem pi = null;
                if (closestIdx < 0)
                {
                    pi = new PositionItem(currentPosition, posIdx, PositionItemType.Base);
                    PositionLink pl = new PositionLink(db[closestIdxNum], pi);

                    db.Insert(0, pi);
                    links.Add(pl);
                }
                else
                {
                    if (closestIdx != 0)  Swap(closestIdx, 0);

                    pi = new PositionItem(currentPosition, posIdx, PositionItemType.Record);
                    db[0].ZoneItems.Add(pi);
                }

                records.Add(pi);
                ++posIdx;

                currentZone = db[0];
                PrintClassStatus();
            }
        }

        private void AddPositionItem(PositionItemType type = PositionItemType.Marker)
        {
            if (type == PositionItemType.Record) return;

            FindClosestPoint(PercentListClosestPoint, MinItemsDistance);
            if (closestIdxNum != 0) Swap(closestIdxNum, 0);

            PositionItem pi = new PositionItem(currentPosition, posIdx, type);
            PositionLink pl = new PositionLink(db[closestIdxNum], pi);
            ++posIdx;

            db.Insert(0, pi);
            links.Add(pl);
            records.Add(pi);

            currentZone = db[0];
        }

        private void PrintClassStatus()
        {
            string s = "";
            if (currentZone != null)
                s += $"currentZone: {currentZone.uP}";
            for(int i=0; i<db.Count; ++i)
            {
                s += $"\t[{i}] : {db[i].uP}";
            }
            Debug.Log(s);
        }
    }
}

