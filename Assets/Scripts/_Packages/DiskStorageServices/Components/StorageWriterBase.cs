
// #define WINDOWS_UWP

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

using UnityEngine;

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
        [Tooltip("Add timestamp at the end of the file")]
        public bool UseTimestamp = false;
        [Tooltip("Create file on start")]
        public bool CreateFileOnStart = false;
        [Tooltip("Apply timestamp to each line")]
        public bool UseLineTimestamp = false;



        // ====== PUBLIC ===== //

        public bool IsWriting
        {
            get
            {
                lock (MUTEX_qlines)
                    return qlines.Count > 0;
            }
        }



        // ====== PROTECTED ===== //

        // either the module is authorized to write into the storage or not
        protected bool authorized = false;
        // either the file has been created or not
        protected bool fileCreated = false;
        // name of the file in the storage (empty if not created)
        protected string fname = "";
        // format of the file ito the storage (default is txt file)
        protected string format = "txt";
        // writing queue
        protected Queue qlines = new Queue();
        // semaphore to access the queue
        protected object MUTEX_qlines = new object();
        // used for creating the file
        protected Coroutine COR_StorageSetup = null;
        // Process that stores the content of the queue inside the storage (completely background)
        protected Task TASK_StorageOutput = null;



        // ====== PRIVATE ===== //
#if WINDOWS_UWP
        // the folder containing the file o write into
        private StorageFolder sf;
        // the file to write into
        private StorageFile fil;
#else
        // system IO stream
        private StreamWriter StorageFile = null;
#endif



        // ====== UNITY CALLBACKS ===== //

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

        protected virtual void OnDestroy()
        {
#if UNITY_EDITOR
            Update();
            StorageFile.Close();
#endif
        }

        protected virtual void Update()
        {
#if UNITY_EDITOR
            if(qlines.Count > 0)
            {
                while (qlines.Count > 0)
                {
                    StorageFile.Write((string)qlines.Dequeue());
                }
                StorageFile.Flush();
            }
#endif
        }



        // ====== CLASS EVENTS ===== //

        public virtual bool EVENT_IsEnabled()
        {
            return authorized && fileCreated;
        }

        public virtual void EVENT_CreateFile()
        {
            if (!fileCreated)
                COR_StorageSetup = StartCoroutine(ORCOR_StorageSetup());
        }



        // ====== CHECK AUTHORIZATION UWP ===== //

        // check if the class is authorized to write or not into the storage
        public virtual bool EVENT_CheckAuthorization()
        {
#if WINDOWS_UWP
            if (!authorized)
                authorized = UWP_CheckAuthorization();
#else
            authorized = true;
#endif

            return authorized;
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
            Debug.LogWarning($"[StorageWriterBase] WARNING: calling UWP function from non-UWP environment");
            return true;
#endif
        }



        // ====== CREATE FILE ON STORAGE ===== //

        private IEnumerator BSCOR_NewFile(string fileName, string fileFormat = "txt")
        {
            yield return null;
            
            DateTime ts = DateTime.Now;
            if (UseTimestamp)
                fname = $"{fileName}_{ts.Year:0000}{ts.Month:00}{ts.Day:00}_{ts.Hour:00}{ts.Minute:00}{ts.Second:00}.{fileFormat}";
            else
                fname = $"{fileName}.{fileFormat}";
            format = fileFormat;

#if WINDOWS_UWP
            Task<StorageFile> newFileTask = sf.CreateFileAsync(fname, CreationCollisionOption.ReplaceExisting).AsTask();
            while (!newFileTask.IsCompleted)
                yield return new WaitForEndOfFrame();
            fil = newFileTask.Result;

            Debug.Log($"new CSV file : '{fname}'");

            fileCreated = true;
#else
            StorageFile = new StreamWriter($"C:\\shared\\{fname}", false);
            fileCreated = true;
#endif
        }



        // ====== WRITE ON STORAGE ===== //

        public virtual bool EVENT_Write(string line, bool endline = true)
        {
            if (!EVENT_IsEnabled())
                return false;

            string lineTmStp = $"[{DateTime.Now}] ";
            lock (MUTEX_qlines)
                qlines.Enqueue((UseLineTimestamp ? lineTmStp : "") + line + (endline && !line.EndsWith("\n") ? "\n" : ""));

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

        private void BSTASK_StorageOutputBackground()
        {
#if WINDOWS_UWP
            while (qlines.Count > 0)
                lock (MUTEX_qlines)
                    FileIO.AppendTextAsync(fil, (string)qlines.Dequeue()).AsTask().GetAwaiter().GetResult();
#else
            /*
            while (qlines.Count > 0)
                lock (MUTEX_qlines)
                {
                    StorageFile.Write((string)qlines.Dequeue());
                    StorageFile.Flush();
                }
            */
#endif
        }
    }
}

