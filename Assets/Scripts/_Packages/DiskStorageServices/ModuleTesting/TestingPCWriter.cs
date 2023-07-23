using System.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Packages.DiskStorageServices.ModuleTesting
{
    public class TestingPCWriter : MonoBehaviour
    {
        // ===== PRIVATE ===== //

        [Header("Main settings")]
        [Tooltip("The path of the file")]
        public string FilePath = "C:\\Shared\\output";
        [Tooltip("The name of the file")]
        public string FileName = "output.txt";
        [Tooltip("Time between one writing and another one")]
        public float OperationPeriod = 1.0f;


        [Header("Write")]
        [Tooltip("Please don't update now! I'm still writing!")]
        public bool StillWriting = false;
        [Tooltip("Write here something to write into the file")]
        [TextArea(5,5)]
        public string WriteThis = "";

        [Header("Read (Output only)")]
        [Tooltip("Here the content of the file will appear")]
        [TextArea(20, 20)]
        public string FileContent = "";



        // ===== PRIVATE ===== //

        // read write coroutine
        private Coroutine COR_ReadWrite = null;
        // complete path 
        private string path = "";



        // ===== UNITY CALLBACKS ===== //

        // Start is called before the first frame update
        void Start()
        {
            path = FilePath + "\\" + FileName;
            writeToFile("STARTING", false);
            COR_ReadWrite = StartCoroutine(ORCOR_ReadWrite());
        }



        // ===== FEATURE READ WRITE FILE ===== //

        private IEnumerator ORCOR_ReadWrite()
        {
            while(true)
            {
                yield return new WaitForSecondsRealtime(OperationPeriod);
                if (StillWriting) continue;

                if (WriteThis != "")
                {
                    writeToFile(WriteThis, append: true);
                    readFromFile();
                }
            }
        }
        private void readFromFile()
        {
            using (StreamReader sr = new StreamReader(path))
                FileContent = sr.ReadToEnd();
        }

        private void writeToFile(string msg, bool append = true)
        {
            using (StreamWriter sw = new StreamWriter(path, append))
                sw.WriteLine($"[{DateTime.Now}] " + msg);
            WriteThis = "";
        }
    }
}
