using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Packages.StorageUtilities.Types
{
    public class CSVLineReaderType : MonoBehaviour
    {
        public virtual bool EVENT_ReadCSVRow( List<string> ls )
        {
            return true;
        }
    }
}
