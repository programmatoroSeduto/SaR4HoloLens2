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



namespace Packages.StorageUtilities.Types
{
    public class StorageWriterBase : MonoBehaviour
    {
        [Header("Storage Base Settings")]
        [Tooltip("Name of the text file to write on")]
        public string FileName = "newfile";

        [Tooltip("Add timestamp at the end of the file")]
        public bool UseTimestamp = false;

        [Tooltip("Create file on start")]
        public bool CreateFileOnStart = false;




        protected bool authorized = false;
        protected bool fileCreated = false;

        protected string fname = "";
        protected string format = "txt";

        protected Queue qlines = new Queue();
        protected object MUTEX_qlines = new object();
        /*
         * lock(MUTEX_qlines) qlines.Enqueue("ciao");
         * lock(MUTEX_qlines) string c = (string)qlines.Dequeue();
         * */

        protected Coroutine COR_StorageSetup = null;
        protected Coroutine COR_FileCreation = null;
        protected Task TASK_StorageOutput = null;

#if WINDOWS_UWP
        private StorageFolder sf;
        private StorageFile fil;
#endif



        // Start is called before the first frame update
        protected virtual void Start()
        {
            if (!EVENT_CheckAuthorization())
            {
                Debug.LogWarning("[StorageWriterBase] ERROR: unauthorized!");
                return;
            }

            if (CreateFileOnStart)
                COR_StorageSetup = StartCoroutine(ORCOR_StorageSetup());
        }

        // Update is called once per frame
        protected virtual void Update()
        {

        }

        public virtual bool EVENT_CheckAuthorization()
        {
            if (!authorized)
                authorized = UWP_CheckAuthorization();

            return authorized;
        }

        public virtual bool EVENT_IsEnabled()
        {
            return authorized && fileCreated;
        }

        public virtual void EVENT_CreateFile()
        {
            if (!fileCreated)
                COR_StorageSetup = StartCoroutine(ORCOR_StorageSetup());
        }

        public virtual bool EVENT_Write(string line, bool endline = true)
        {
            if (!EVENT_IsEnabled())
                return false;

            lock (MUTEX_qlines)
                qlines.Enqueue(line + (endline ? "\n" : ""));

            return true;
        }

        protected IEnumerator ORCOR_StorageSetup()
        {
            yield return null;

            if (!EVENT_CheckAuthorization())
            {
                Debug.LogWarning("[StorageWriterBase] ERROR: unauthorized!");
                yield break;
            }
            
            yield return BSCOR_NewFile(FileName, format);

#if WINDOWS_UWP
            TASK_StorageOutput = Task.Run(() =>
            {
                while (true)
                {
                    BSTASK_StorageOutputBackground();
                    Task.Delay(1000);
                }
            });
#endif
        }

        private IEnumerator BSCOR_NewFile(string fileName, string fileFormat = "txt")
        {
            yield return null;

#if WINDOWS_UWP
            DateTime ts = DateTime.Now;
            if (UseTimestamp)
                fname = $"{fileName}_{ts.Year:0000}{ts.Month:00}{ts.Day:00}_{ts.Hour:00}{ts.Minute:00}{ts.Second:00}.{fileFormat}";
            else
                fname = $"{fileName}.{fileFormat}";
            format = fileFormat;

            Task<StorageFile> newFileTask = sf.CreateFileAsync(fname, CreationCollisionOption.ReplaceExisting).AsTask();
            while (!newFileTask.IsCompleted)
                yield return new WaitForEndOfFrame();
            fil = newFileTask.Result;

            Debug.Log($"new CSV file : '{fname}'");

            fileCreated = true;
#else
        fileCreated = true;
#endif
        }

        private void BSTASK_StorageOutputBackground()
        {
#if WINDOWS_UWP
            while (qlines.Count > 0)
                lock (MUTEX_qlines)
                    FileIO.AppendTextAsync(fil, (string)qlines.Dequeue()).AsTask().GetAwaiter().GetResult();
#endif
        }

        protected bool UWP_CheckAuthorization()
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