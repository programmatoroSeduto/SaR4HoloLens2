using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Packages.ExplorationServices.Utils
{
    public class PositionLink
    {
        public PositionItem Item1;
        public PositionItem Item2;

        // è possibile raccogliere tutta una serie di altre statistiche, tipo
        public float Distance = float.MaxValue;

        public PositionLink()
        {

        }

        public PositionLink(PositionItem pi1, PositionItem pi2)
        {
            Item1 = pi1;
            Item2 = pi2;

            pi1.Links.Add(this);
            pi2.Links.Add(this);

            Distance = Vector3.Distance(Item1.uP, Item2.uP);
        }
    }
}