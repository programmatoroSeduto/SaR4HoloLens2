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
    public class CSVFileWriter : CSVLineReaderType
    {
        [Tooltip("Name of the CSV file to write on")]
        public string FileName = "newfile";

        [Tooltip("Create file on start")]
        public bool CreateFileOnStart = false;

        [Tooltip("A list of fiels making the header of the CSV to read")]
        public List<string> CSVFields = new List<string>();

        [Tooltip("A column is automatically added as a message is received, filled with the timestamp")]
        public bool ApplyTimestampColumn = true;

        [Tooltip("It adds another column with the timespan between the first message and the most recent one")]
        public bool ApplyDurationColumn = true;

        [Tooltip("It adds a column with a counter for the measurements")]
        public bool ApplyCounter = false;

        [Tooltip("Test Mode: a new line is written every 2 seconds, re-using the header")]
        public bool TestMode = false;



        private bool authorized = false;
        private bool fileCreated = false;
        private bool fileWriting = false;
        private string fname = "";
        private Coroutine COR_fileCreation = null;
        private Coroutine COR_fileOutput = null;
        private Coroutine COR_testModule = null;

        private int csvLineCounter = 0;
        private DateTime csvFirstRecordDate;

#if WINDOWS_UWP
        private StorageFolder sf;
        private StorageFile fil;
#endif


        private void Start()
        {
            if (CreateFileOnStart) EVENT_CreateFile();
        }

        public override bool EVENT_ReadCSVRow(List<string> ls)
        {
            if(ls.Count != CSVFields.Count)
            {
                Debug.LogWarning($"ERROR: line must have the same number of arguments of the header! Header len: {CSVFields.Count} , line len: {ls.Count}");
                return false;
            }

            if (!(fileCreated && !fileWriting))
            {
                Debug.LogWarning("ERROR: CSV writer is not ready to write the line");
                return false;
            }

            string csvln = ToCSV(ls, ApplyCounter, ApplyTimestampColumn, ApplyDurationColumn);
            COR_fileOutput = StartCoroutine(BSCOR_WriteCSV(csvln));
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

        public void EVENT_CreateFile()
        {
            if (!fileCreated)
            {
                // check access to folder
                authorized = UWP_CheckAuthorization();
                if (!authorized) return;
                else Debug.Log("CSV File Writer Authorized");

                if (CSVFields.Count == 0)
                {
                    Debug.LogWarning("ERROR: no field provided as header of the CSV file! Closing...");
                    return;
                }
                COR_fileCreation = StartCoroutine(ORCOR_StorageSetup());
            }
        }






        private IEnumerator ORCOR_StorageSetup()
        {

            yield return BSCOR_NewFile();
            if (!fileCreated) yield break;

            yield return BSCOR_WriteCSV(GetHeader(CSVFields, ApplyCounter, ApplyTimestampColumn, ApplyDurationColumn));

            if (TestMode)
                COR_testModule = StartCoroutine(BSCOR_Test());
        }




        private IEnumerator BSCOR_Test()
        {
            while(true)
            {
                yield return new WaitForSecondsRealtime(2.0f);
                bool res = EVENT_ReadCSVRow(CSVFields);
                if(!res)
                {
                    Debug.LogWarning("ERROR: CSV writing failed!");
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
            fname = $"{FileName.Replace(" ", "-")}_{ts.Year:0000}{ts.Month:00}{ts.Day:00}_{ts.Hour:00}{ts.Minute:00}{ts.Second:00}.csv";
            
            Task<StorageFile> newFileTask = sf.CreateFileAsync(fname, CreationCollisionOption.ReplaceExisting).AsTask();
            while (!newFileTask.IsCompleted)
                yield return new WaitForEndOfFrame();
            fil = newFileTask.Result;

            Debug.Log($"new CSV file : '{fname}'");
            
            fileCreated = true;
#endif
        }

        private IEnumerator BSCOR_WriteCSV( string line )
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

        private string ToCSV(List<string> listline, bool withCount = false, bool withTimestamp = false, bool withDuration = false)
        {
            if (listline.Count == 0) return "";

            string s = $"\"{listline[0]}\"";
            DateTime ts = DateTime.Now;
            if (csvLineCounter == 0) csvFirstRecordDate = ts;

            // foreach( string ss in listline )
            for( int i=1; i<listline.Count; ++i )
                s = s += "," + $"\"{listline[i]}\"";

            if(withCount) s = s += "," + $"\"{csvLineCounter}\"";
            ++csvLineCounter; // ALWAYS increment

            if(withTimestamp) s = s += "," + $"\"{ts.ToString()}\"";

            if(withDuration)
            {
                TimeSpan delta = ts - csvFirstRecordDate;
                s = s += "," + $"\"{delta.TotalMilliseconds}\"";
            }

            return (s + "\n");
        }


        private string GetHeader(List<string> fields, bool withCount = false, bool withTimestamp = false, bool withDuration = false)
        {
            if (fields.Count == 0) return "";

            string s = $"{fields[0]}";
            // foreach (string ss in fields)
            for (int i = 1; i < fields.Count; ++i)
                s = s += "," + $"{fields[i]}";

            if (withCount) s = s += ",COUNTER";
            if (withTimestamp) s = s += ",TIMESTAMP";
            if (withDuration) s = s += ",TIMESPAN";

            return (s + "\n");
        }
    }

}