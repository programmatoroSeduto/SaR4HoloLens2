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
    public class CSVFileWriter : StorageWriterBase
    {
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



        private int csvLineCounter = 0;
        private DateTime csvFirstRecordDate;

        protected override void Start()
        {
            format = "csv";
            qlines.Enqueue(GetHeader(CSVFields, ApplyCounter, ApplyTimestampColumn, ApplyDurationColumn));

            if (CSVFields.Count == 0)
            {
                Debug.LogWarning("[CSVFileWriter] ERROR: no field provided as header of the CSV file! Closing...");
                return;
            }

            if (!EVENT_CheckAuthorization())
            {
                Debug.LogWarning("[CSVFileWriter] ERROR: unauthorized!");
                return;
            }

            if (CreateFileOnStart)
                COR_StorageSetup = StartCoroutine(ORCOR_StorageSetup());
        }

        public bool EVENT_WriteCsv(List<string> ls, bool print= false)
        {
            if(ls.Count != CSVFields.Count)
            {
                Debug.LogWarning($"[CSVFileWriter] ERROR: line must have the same number of arguments of the header! Header len: {CSVFields.Count} , line len: {ls.Count}");
                return false;
            }
            string csvln = ToCSV(ls, ApplyCounter, ApplyTimestampColumn, ApplyDurationColumn);
            if (print) Debug.Log(csvln);

            if (!fileCreated)
            {
                Debug.LogWarning("[CSVFileWriter] ERROR: CSV writer is not ready to write the line");
                return false;
            }

            return EVENT_Write(csvln, false);
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