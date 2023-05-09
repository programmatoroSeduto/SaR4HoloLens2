using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SaR4Hololens2.Scripts.Components
{
    public class ChangeSceneTimed : MonoBehaviour
    {
        public int DelaySeconds = 5;

        public string NextScene = "BuildingGeolocation";

        private EntryPoint ep = null;

        private Coroutine c = null;

        // Start is called before the first frame update
        void Start()
        {
            Debug.Log("Starting ChangeSceneTimed");
            GameObject go = GameObject.FindGameObjectWithTag("EntryPoint");
            if (go == null)
            {
                Debug.LogWarning("ERROR: cannot find GameObject with tag 'EntryPoint'!");
                return;
            }
            else
                Debug.Log("Found object with tag 'EntryPoint'");
            
            ep = go.GetComponent<EntryPoint>();
            if (ep == null)
            {
                Debug.LogWarning("ERROR: cannot find EntryPoint component inside the gambeObject with tag 'EntryPoint'!");
                return;
            }
            else
                Debug.Log("Found component EntryPoint");

            c = StartCoroutine(ORCOR_ChangeScene());
        }

        private IEnumerator ORCOR_ChangeScene()
        {
            yield return null;

            Debug.Log("Start waiting ...");
            yield return new WaitForSecondsRealtime((float)DelaySeconds);

            Debug.Log("Changing scene ...");
            ep.EVENT_ChangeScene(NextScene);

            yield return null;
        }
    }
}
