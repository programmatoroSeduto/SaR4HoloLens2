
//define WINDOWS_UWP

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;

// using Packages.DiskStorageServices.Types;

#if WINDOWS_UWP
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
#endif

namespace Packages.DiskStorageServices.Components
{
    public class StorageWriterBase : MonoBehaviour
    {
        // ====== GUI ===== //

        [Header("Storage Base Settings")]
        [Tooltip("Name of the text file to write on")]
        public string FileName = "newfile";
    }
}

