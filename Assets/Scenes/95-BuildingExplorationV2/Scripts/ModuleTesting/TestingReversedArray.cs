using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SaR4Hololens2.Scenes.BuildingExplorationV2.Scripts.Utils;
using SaR4Hololens2.Scenes.BuildingExplorationV2.Scripts.Components;

namespace SaR4Hololens2.Scenes.BuildingExplorationV2.Scripts.ModuleTesting
{
    public class TestingReversedArray : MonoBehaviour
    {
        [Min(3)]
        public int maxPos = 10;

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
                if (j == -1) j = i + 1;

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

        // Start is called before the first frame update
        void Start()
        {
            for(int i=0; i<maxPos; ++i)
            {
                PositionDatabaseWaypoint wp = new PositionDatabaseWaypoint();
                wp.AreaCenter = i * Vector3.one;
                wp.AreaRadius = i;
                wp.DBReference = null;

                db.Insert(wp);
                Debug.Log($"wp no.{i}: \n{wp}");
            }

            Debug.Log($"db Count: {db.Count}");
            for (int i = 0; i < db.Count; ++i)
                Debug.Log($"wp no.{i}: \n{db[i]}");

            Debug.Log($"First is: {db.First}");
        }
    }

}

