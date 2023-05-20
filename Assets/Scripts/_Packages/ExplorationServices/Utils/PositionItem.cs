using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Packages.ExplorationServices.Utils
{
    public class PositionItem
    {
        public int ItemIndex = -1;
        public Vector3 uP = Vector3.zero;
        public DateTime Timestamp;
        public PositionItemType Type = PositionItemType.Base;
        
        // per poter tenere traccia anche di altri record senza bisogno di ordinarli
        public List<PositionItem> ZoneItems = new List<PositionItem>();

        // per tenere traccia di archi che partono da questo nodo
        public List<PositionLink> Links = new List<PositionLink>();

        public PositionItem(Vector3 uP, int idx=-1, PositionItemType type = PositionItemType.Base)
        {
            ItemIndex = idx;
            this.uP = uP;
            Timestamp = DateTime.Now;
            Type = type;
        }

        public float DistFromUser(Vector3 currentPos)
        {
            return Vector3.Distance(uP, currentPos);
        }
    }
}