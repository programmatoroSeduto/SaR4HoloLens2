using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SaR4Hololens2.Scripts.ModuleTesting
{
    public class TestJsonClass
    {
        public string test = "ciao";
    }

    public class TestingJsonFail : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            try
            {
                JsonUtility.FromJson<TestJsonClass>("non è un JSON valido.");
            }
            catch(System.Exception)
            {
                Debug.Log("JSON ERROR.");
            }
        }

        // Update is called once per frame
        void Update()
        {

        }
    }

}