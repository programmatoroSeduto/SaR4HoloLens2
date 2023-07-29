
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
    public class StorageHubOneShot : MonoBehaviour
    {
        // ====== PUBLIC ===== //

        public bool FileReadSuccess
        {
            get => !readError;
        }

        public bool FileReadInProgress
        {
            get => isReadingFile;
        }

        public string FileContent
        {
            get => readContent;
        }



        // ====== PRIVATE ===== //

        // either the class has the authorization to write or not (UWP only)
        private bool init = false;
        // list of opened files
#if WINDOWS_UWP
        // the folder containing the file o write into
        private StorageFolder sf;
        // list of opened references to files
        private StorageFile fileRef;
#else
        // list of opened references to files
        private StreamWriter fileRef;
#endif
        // the last content read from the file
        private string readContent = "";
        // it indicates when the class is reading a file
        private bool isReadingFile = false;
        private bool readError = false;



        // ====== UNITY CALLBACKS ===== //

        private void Start()
        {
            if (!CheckAuthorization())
            {
                Debug.LogWarning("[StorageWriterHubBase] ERROR: unauthorized!");
                return;
            }
        }

        // close all the files when the component is destroyed
        private void OnDestroy()
        {
            
        }



        // ====== CLASS INITIALIZATION ===== //

        // try to init the class 
        public bool CheckAuthorization()
        {
#if WINDOWS_UWP
            if (!init)
                init = UWP_CheckAuthorization();
#else
            init = true;
#endif

            return init;
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
                Debug.LogWarning($"[StorageWriterHubBase] ERROR: Unauthorized to access folder '{sf.ToString()}'");
                return false;
            }

            return true;
#else
            Debug.LogWarning($"[StorageWriterHubBase] WARNING: calling UWP function from non-UWP environment");
            return true;
#endif
        }



        // ====== WRITE ON FILE ===== //

        // one shot file writing on storage
        public void WriteOneShot(string fileName, string fileFormat, string content, bool useTimestamp=true)
        {
            // Debug.Log("Starting Coroutine ... ");
            StartCoroutine(BSCOR_WriteOneShot(fileName, fileFormat, content, useTimestamp));
        }

        // coroutine for writing the file
        private IEnumerator BSCOR_WriteOneShot(string fileName, string fileFormat, string content, bool useTimestamp = true)
        {
            yield return null;

            DateTime ts = DateTime.Now;
            string fname = "";
            string format = "";
            if (useTimestamp)
                fname = $"{fileName}_{ts.Year:0000}{ts.Month:00}{ts.Day:00}_{ts.Hour:00}{ts.Minute:00}{ts.Second:00}.{fileFormat}";
            else
                fname = $"{fileName}.{fileFormat}";
            format = fileFormat;

            // Debug.Log($"Writing data on {fname} ... ");

#if WINDOWS_UWP
            Task<StorageFile> newFileTask = sf.CreateFileAsync(fname, CreationCollisionOption.ReplaceExisting).AsTask();
            while (!newFileTask.IsCompleted)
                yield return new WaitForEndOfFrame();
            fileRef = newFileTask.Result;
            Task writeOnFileIO = FileIO.AppendTextAsync(fileRef, content).AsTask();
            while (!writeOnFileIO.IsCompleted)
                yield return new WaitForEndOfFrame();
#else
            fileRef = new StreamWriter($"C:\\shared\\{fname}", false);
            fileRef.Write(content);
            fileRef.Close();
#endif

            // Debug.Log("Done.");
        }



        // ====== READ FROM FILE ===== //

        public void EVENT_ReadOneShot(string fileName)
        {
            StartCoroutine(ReadOneShot(fileName));
        }

        public IEnumerator ReadOneShot(string fileName)
        {
            isReadingFile = true;
            readError = false;
            readContent = "";

            if (fileName == "")
                yield break;

            yield return null;

#if WINDOWS_UWP
            Task<StorageFile> readFileRef = sf.GetFileAsync(fileName).AsTask<StorageFile>();
            while (!readFileRef.IsCompleted)
                yield return new WaitForEndOfFrame();
            if(readFileRef.IsFaulted)
            {
                Debug.LogError($"cannot read file {fileName}");

                readError = true;
                isReadingFile = false;
                yield break;
            }
            fileRef = readFileRef.Result;
            Task<string> readContentTask = FileIO.ReadTextAsync(fileRef).AsTask<string>();
            while (!readContentTask.IsCompleted)
                yield return new WaitForEndOfFrame();
            readContent = readContentTask.Result;
#else
            StreamReader sr = null;
            try
            {
                sr = new StreamReader($"C:\\shared\\{fileName}");
            }
            catch(IOException)
            {
                Debug.LogError($"cannot read file {fileName}");

                readError = true;
                isReadingFile = false;
                yield break;
            }
            readContent = sr.ReadToEnd();
            sr.Close();
#endif
            readError = false;
            isReadingFile = false;
        }
    }
}
