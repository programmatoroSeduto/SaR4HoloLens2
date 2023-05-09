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
    public class TextFileWriter : TextReaderType
    {
        [Tooltip("Name of the text file to write on")]
        public string FileName = "newfile";

        [Tooltip("Format of the text file")]
        public string FileFormat = "log";

        [Tooltip("Test Mode: a new line is written every 2 seconds, re-using the header")]
        public bool TestMode = false;


        private bool authorized = false;
        private bool fileCreated = false;
        private bool fileWriting = false;
        private string fname = "";
        private Coroutine COR_fileCreation = null;
        private Coroutine COR_fileOutput = null;
        private Coroutine COR_testModule = null;

#if WINDOWS_UWP
        private StorageFolder sf;
        private StorageFile fil;
#endif



        void Start()
        {
            if (FileFormat == "")
            {
                Debug.LogWarning("ERROR: FileFormat cannot be empty!");
                return;
            }

            // check access to folder
            authorized = UWP_CheckAuthorization();
            if (!authorized) return;

            COR_fileCreation = StartCoroutine(ORCOR_StorageSetup());
        }

        public override bool EVENT_ReadText(string txt)
        {
            if (!(fileCreated && !fileWriting))
            {
                Debug.LogWarning("ERROR: Text writer is not ready to write the line");
                return false;
            }

            COR_fileOutput = StartCoroutine(BSCOR_WriteText(txt));
            return true;
        }

        public bool EVENT_IsReadyForOutput()
        {
            return fileCreated && !fileWriting;
        }

        public bool EVENT_IsEnabled()
        {
            return authorized && fileCreated;
        }






        private IEnumerator ORCOR_StorageSetup()
        {
            yield return BSCOR_NewFile();
            if (!fileCreated) yield break;

            if (TestMode)
                COR_testModule = StartCoroutine(BSCOR_Test());
        }

        private IEnumerator BSCOR_Test()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(2.0f);
                bool res = EVENT_ReadText("bla bla bla ");
                if (!res)
                {
                    Debug.LogWarning("ERROR: textFile writing failed!");
                    Debug.LogWarning($"with IsEnabled: {EVENT_IsEnabled()}");
                    Debug.LogWarning($"with IsReadyForOutput: {EVENT_IsReadyForOutput()}");
                    break;
                }
                while (!EVENT_IsReadyForOutput())
                    yield return new WaitForEndOfFrame();
            }
        }

        private IEnumerator BSCOR_NewFile()
        {
            yield return null;
#if WINDOWS_UWP
        DateTime ts = DateTime.Now;
        fname = $"{FileName.Replace(" ", "-")}_{ts.Year:0000}{ts.Month:00}{ts.Day:00}_{ts.Hour:00}{ts.Minute:00}{ts.Second:00}.{FileFormat}";

        Task<StorageFile> newFileTask = sf.CreateFileAsync(fname, CreationCollisionOption.ReplaceExisting).AsTask();
        while (!newFileTask.IsCompleted)
            yield return new WaitForEndOfFrame();
        fil = newFileTask.Result;

        Debug.Log($"new CSV file : '{fname}'");

        fileCreated = true;
#endif
        }

        private IEnumerator BSCOR_WriteText(string line)
        {
            yield return null;
#if WINDOWS_UWP
        fileWriting = true;

        Debug.Log($"WRITE: {line}");
        Task job = FileIO.AppendTextAsync(fil, line).AsTask();
        while (!job.IsCompleted)
            yield return new WaitForEndOfFrame();

        fileWriting = false;
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
