using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SaR4Hololens2.Scripts.ModuleTesting
{
    public class PrintSomething : MonoBehaviour
    {
        public int DelaySeconds = 3;

        [TextArea(15, 15)]
        public string PrintThis = "";

        private Coroutine c = null;

        void Start()
        {
            c = StartCoroutine(ORCOR_PrintSomething());
        }

        private IEnumerator ORCOR_PrintSomething()
        {
            yield return new WaitForSecondsRealtime((float)DelaySeconds);
            Debug.Log(PrintThis);
        }
    }
}

