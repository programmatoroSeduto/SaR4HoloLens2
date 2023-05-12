//define WINDOWS_UWP

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;

using Packages.StorageUtilities.Types;

#if WINDOWS_UWP
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
#endif

namespace Packages.StorageUtilities.Components
{
    public class TextFileWriter : StorageWriterBase
    {
        
    }

}
