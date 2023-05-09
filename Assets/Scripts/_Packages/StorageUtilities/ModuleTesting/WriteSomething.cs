using System;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

#if WINDOWS_UWP
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
#endif

namespace Packages.StorageUtilities.ModuleTesting
{
    public class WriteSomething : MonoBehaviour
    {
        public int DelaySeconds = 3;

        public string FileName = "newfile";
        public string FileFormat = "txt";

        public bool ApplyTimestamp = true;

        [TextArea(15, 15)]
        public string WriteThis = "";

        private Coroutine c = null;
        private bool authorized = false;
        private string fname = "";

#if WINDOWS_UWP
        private StorageFolder sf;
        private StorageFile fil;
#endif

        void Start()
        {
            // check access to folder
            authorized = UWP_CheckAuthorization();
            if (!authorized) return;

            c = StartCoroutine(ORCOR_StorageOutput());
        }

        private IEnumerator ORCOR_StorageOutput()
        {
            yield return new WaitForSecondsRealtime((float) DelaySeconds);

            // new file
            yield return BSCOR_StorageOutputNewFile();

            // write on file
            yield return BSCOR_StorageOutputWriteOnFile();
        }



        private IEnumerator BSCOR_StorageOutputNewFile()
        {
            yield return null;
#if WINDOWS_UWP
            // final file name
            if (ApplyTimestamp)
            {
                DateTime ts = DateTime.Now;
                fname = $"{FileName.Replace(" ", "-")}_{ts.Year:0000}{ts.Month:00}{ts.Day:00}_{ts.Hour:00}{ts.Minute:00}{ts.Second:00}.{FileFormat}";
            }
            else
                fname = $"{FileName}.{FileFormat}";

            Debug.Log($"Creating new file with name: '{fname}' ... ");

            //UWP
            Task<StorageFile> newFileTask = sf.CreateFileAsync(fname, CreationCollisionOption.ReplaceExisting).AsTask();
            while (!newFileTask.IsCompleted)
                yield return new WaitForEndOfFrame();
            fil = newFileTask.Result;
            //UWP

            Debug.Log($"OK! File created with name '{fname}'");
#endif
        }

        private IEnumerator BSCOR_StorageOutputWriteOnFile()
        {
            yield return null;
#if WINDOWS_UWP
            Debug.Log("Requiring channel ...");

            //UWP
            Task<IRandomAccessStream> streamTask = fil.OpenAsync(FileAccessMode.ReadWrite).AsTask();
            while(!streamTask.IsCompleted)
                yield return new WaitForEndOfFrame();
            IRandomAccessStream stream = streamTask.Result;
            IOutputStream outputStream = stream.GetOutputStreamAt(0);
            DataWriter wr = new DataWriter(outputStream);
            //UWP

            Debug.Log("Writing on file...");

            //UWP
            wr.WriteString(WriteThis);
            var jobStore = wr.StoreAsync().AsTask();
            while(!jobStore.IsCompleted)
                yield return new WaitForEndOfFrame();
            var jobFlush = outputStream.FlushAsync().AsTask();
            while (!jobFlush.IsCompleted)
                yield return new WaitForEndOfFrame();
            //UWP

            Debug.Log("Closing streams ... ");

            //UWP
            outputStream.Dispose();
            stream.Dispose();
            //UWP
#endif
        }

        private bool UWP_CheckAuthorization()
        {
#if WINDOWS_UWP
            try
            {
                sf = ApplicationData.Current.LocalFolder;
            }
            catch (System.UnauthorizedAccessException)
            {
                Debug.LogWarning($"ERROR: Unauthorized to access folder '{sf.ToString()}'");
                return false;
            }

            return true;
#else
            return false;
#endif
        }
    }
}
