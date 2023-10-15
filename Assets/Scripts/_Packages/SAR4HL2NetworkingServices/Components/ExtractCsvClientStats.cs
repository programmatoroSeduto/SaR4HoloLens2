using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Project.Scripts.Utils;
using Packages.DiskStorageServices.Components;
using Packages.SAR4HL2NetworkingServices.Utils;

namespace Packages.SAR4HL2NetworkingServices.Components
{
    public class ExtractCsvClientStats : ProjectMonoBehaviour
    {
        // ===== GUI ===== //

        [Header("Basic Settings")]
        public CsvWriter CsvComponent = null;
        public float WaitCycle = 5.0f;




        // ===== PRIVATE ===== //

        private Coroutine COR_WriteToCsv = null;
        private int lastCount = 0;




        // ===== UNITY CALLBACKS ===== //

        private void Start()
        {
            if(CsvComponent == null)
            {
                StaticLogger.Err(this, "CSV component cannot be null!");
                return;
            }

            COR_WriteToCsv = StartCoroutine(ORCOR_WriteCsv());
        }




        // ===== WRITE STATS ON CSV FILE ===== //

        private IEnumerator ORCOR_WriteCsv()
        {
            while(true)
            {
                yield return new WaitForSecondsRealtime(WaitCycle);
                if (SarAPI.InProgress)
                    continue;

                List<ClientStatistics> stats = SarAPI.Statistics;
                if(stats.Count != lastCount)
                {
                    int idx = lastCount;
                    lastCount = stats.Count;
                    for(int i = idx; i<lastCount; ++i)
                        CsvComponent.EVENT_WriteCsv(stats[i].ToCsvList());
                }
            }
            
        }
    }

}

