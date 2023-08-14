using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Packages.PositionDatabase.Components;

namespace Packages.PositionDatabase.Utils
{
    [Serializable]
    public class JSONTuple<Tkey, Tvalue>
    {
        public Tkey key;
        public Tvalue value;
    }
}