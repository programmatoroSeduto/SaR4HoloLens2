//define WINDOWS_UWP

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Packages.DiskStorageServices.Utils;
using Project.Scripts.Utils;

using UnityEngine;

#if WINDOWS_UWP
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
#endif

namespace Packages.DiskStorageServices.Components
{
    public class CsvWriter : StorageWriterBase
    {
        // ====== GUI ===== //

        [Header("CSV File Writer Settings")]
        [Tooltip("A list of fiels making the header of the CSV to read")]
        public List<string> CSVFields = new List<string>();
        [Tooltip("A column is automatically added as a message is received, filled with the timestamp")]
        public bool ApplyTimestampColumn = true;
        [Tooltip("It adds another column with the timespan between the first message and the most recent one")]
        public bool ApplyDurationColumn = true;
        [Tooltip("It adds a column with a counter for the measurements")]
        public bool ApplyCounter = false;
        [Tooltip("Separator to use when writing the file out (comma is used anyway if the separator is empty)")]
        public string Separator = ",";
        [Tooltip("Use JSON-defined schema instead of a C# list")]
        public bool UseJsonDefinition = false;
        [Tooltip("JSON definitio of the CSV schema (a list of fields under 'schema' identifier)")]
        [TextArea(5, 10)]
        public string JsonDefinition = "{\n\"scheme\" : [\n\"column1\",\n\"column2\",\n\"column3\"\n]\n}";



        // ====== PRIVATE ===== //

        // used for generating the index column
        private int csvLineCounter = 0;
        // used for calculating the duration column
        private DateTime csvFirstRecordDate;
        // a unmutable copy of the separator
        private string sep = ",";



        // ====== UNITY CALLBACKS ===== //

        protected override void Start()
        {
            if (Separator != "")
                sep = Separator;

            if (UseJsonDefinition)
            {
                CSVFields = JsonUtility.FromJson<CsvScheme>(JsonDefinition).scheme;
                StaticLogger.Info(this.gameObject, $"CSV lines from JSON: {CSVFields.Count}", logLayer: 1);
            }

            format = "csv";
            string csv_header = GetHeader(CSVFields, ApplyCounter, ApplyTimestampColumn, ApplyDurationColumn);
            // Debug.Log(csv_header);
            qlines.Enqueue(csv_header);

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



        // ====== WRITE FILE ON STORAGE ===== //

        public bool EVENT_WriteCsv(List<string> ls, bool print = false)
        {
            string csvln = ToCSV(ls, ApplyCounter, ApplyTimestampColumn, ApplyDurationColumn);
            if (print) Debug.Log(csvln);

            if (ls.Count != CSVFields.Count)
            {
                string headerStr = String.Join(",", CSVFields);
                Debug.LogError($"[CSVFileWriter] ERROR: line must have the same number of arguments of the header! Header len: {CSVFields.Count} , line len: {ls.Count}\nHEADER: {headerStr}\nLINE: {csvln}");
                return false;
            }

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

            for (int i = 1; i < listline.Count; ++i)
                s = s += sep + $"\"{listline[i]}\"";

            if (withCount) s = s += sep + $"\"{csvLineCounter}\"";
            ++csvLineCounter; // ALWAYS increment

            if (withTimestamp) s = s += sep + $"\"{ts.ToString()}\"";

            if (withDuration)
            {
                TimeSpan delta = ts - csvFirstRecordDate;
                s = s += sep + $"\"{delta.TotalMilliseconds}\"";
            }

            return (s + "\n");
        }

        private string GetHeader(List<string> fields, bool withCount = false, bool withTimestamp = false, bool withDuration = false)
        {
            if (fields.Count == 0) return "";

            string s = $"{fields[0]}";
            for (int i = 1; i < fields.Count; ++i)
                s = s += sep + $"{fields[i]}";

            if (withCount) s = s += sep + "COUNTER";
            if (withTimestamp) s = s += sep + "TIMESTAMP";
            if (withDuration) s = s += sep + "TIMESPAN";

            return (s + "\n");
        }
    }
}
