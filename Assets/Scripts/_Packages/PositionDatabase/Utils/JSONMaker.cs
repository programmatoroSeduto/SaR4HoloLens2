
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Packages.PositionDatabase.Components;
using Project.Scripts.Components;
using Project.Scripts.Utils;

namespace Packages.PositionDatabase.Utils
{
    public class JSONMaker
    {
        // ===== TO JSON FEATURE ===== //

        public JSONWaypoint ToJsonClass(PositionDatabaseWaypoint wp)
        {
            JSONWaypoint res = new JSONWaypoint();

            res.Key = wp.Key;
            res.PositionID = wp.PositionID;
            res.AreaRadius = wp.AreaRadius;
            res.AreaCenter = Vector3ToJSON(wp.AreaCenter);
            res.FirstAreaCenter = Vector3ToJSON(wp.FirstAreaCenter);
            res.Description = wp.Description;
            res.CreatedAt = wp.Timestamp.ToString();
            res.AreaIndex = wp.AreaIndex;

            return res;
        }


        public JSONPath ToJsonClass(PositionDatabasePath link)
        {
            JSONPath res = new JSONPath();

            res.Key = link.Key;
            res.Waypoint1 = link.wp1.Key;
            res.Waypoint2 = link.wp2.Key;

            return res;
        }


        public JSONPositionDatabase ToJsonClass(PositionsDatabase db)
        {
            JSONPositionDatabase res = new JSONPositionDatabase();

            res.CurrentZone = ToJsonClass(db.CurrentZone);
            res.BaseDistance = db.BaseDistance;
            res.BaseHeight = db.BaseHeight;
            res.DistanceTolerance = db.DistanceTolerance;
            res.UseClusters = db.UseClusters;
            res.UseMaxIndices = db.UseMaxIndices;
            res.MaxIndices = db.MaxIndices;
            res.ReferenceID = StaticTransform.ReferencePositionID;

            res.AreaRenaming = DictToJSON<int, int>(db.AreaRenamingLookup);

            return res;
        }



        // ===== FROM JSON FEATURE ===== //

        public void FromJsonClass(JSONWaypoint jwp, PositionDatabaseWaypoint wp)
        {
            wp.AreaRadius = jwp.AreaRadius;
            wp.AreaCenter = JSONToVector3(jwp.AreaCenter);
            wp.setFirstAreaCenter(JSONToVector3(jwp.FirstAreaCenter));
            wp.Description = jwp.Description;
            wp.setPositionID(jwp.PositionID);
            DateTime.TryParse(jwp.CreatedAt, out wp.Timestamp);
            wp.AreaIndex = jwp.AreaIndex;
        }

        public void FromJsonClass(JSONPositionDatabase jdb, PositionsDatabase db)
        {
            db.BaseDistance = jdb.BaseDistance;
            db.BaseHeight = jdb.BaseHeight;
            db.DistanceTolerance = jdb.DistanceTolerance;
            db.UseClusters = jdb.UseClusters;
            db.UseMaxIndices = jdb.UseMaxIndices;
            db.MaxIndices = jdb.MaxIndices;
        }



        // ===== UTILITIES ===== //

        public static List<float> Vector3ToJSON(Vector3 v, bool applyReferenceTransform = true)
        {
            if (applyReferenceTransform)
                v = StaticTransform.ToRefPoint(v);
            
            return new List<float> { v.x, v.y, v.z };
        }

        public static Vector3 JSONToVector3(List<float> vl, bool applyReferenceTransform = true)
        {
            Vector3 v = new Vector3(vl[0], vl[1], vl[2]);

            if (applyReferenceTransform)
                v = StaticTransform.ToAppPoint(v);
            
            return v;
        }

        public static JSONTupleList<Tkey, Tvalue> DictToJSON<Tkey, Tvalue>(Dictionary<Tkey, Tvalue> dict)
        {
            JSONTupleList<Tkey, Tvalue> res = new JSONTupleList<Tkey, Tvalue>();

            foreach (var tup in dict)
            {
                JSONTuple<Tkey, Tvalue> record = new JSONTuple<Tkey, Tvalue>();
                record.key = tup.Key;
                record.value = tup.Value;
                res.Items.Add(record);
            }

            return res;
        }

        public static Dictionary<Tkey, Tvalue> JSONToDict<Tkey, Tvalue>(JSONTupleList<Tkey, Tvalue> jdict)
        {
            Dictionary<Tkey, Tvalue> dict = new Dictionary<Tkey, Tvalue>();

            foreach (var item in jdict.Items)
                dict.Add(item.key, item.value);

            return dict;
        }
    }
}
