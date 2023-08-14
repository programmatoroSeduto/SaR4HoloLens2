using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Packages.PositionDatabase.Components;

namespace Packages.PositionDatabase.Utils
{
    [Serializable]
    public class JSONTupleList<Tkey, Tvalue>
    {
        public List<JSONTuple<Tkey, Tvalue>> Items = new List<JSONTuple<Tkey, Tvalue>>();
    }
}

