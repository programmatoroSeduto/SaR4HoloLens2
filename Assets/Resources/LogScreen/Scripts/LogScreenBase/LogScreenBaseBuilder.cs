using System.Collections;
using System.Collections.Generic;

using UnityEngine;






namespace Packages.VisualItems.LogScreen.Components
{
    public class LogScreenBaseBuilder : MonoBehaviour
    {
        public static readonly string PrefabPath = "LogScreen/LogScreenBase8x57";





        [Header("LogScreen settings")]
        [Tooltip("Name of the Log Screen")]
        public string InitName = "LogScreenBase8x57";

        [Tooltip("Initial Log Screen title")]
        public string InitTitle = "";

        [Tooltip("Initial content")]
        [TextArea(8, 57)]
        public string InitContent = "";

        [Tooltip("Initial offset value")]
        public Vector3 InitLocalOffset;


        [Header("Other settings")]
        [Tooltip("Spawn on start")]
        public bool SpawnOnStart = false;
        
        [Tooltip("Hide on start")]
        public bool HideOnStart = false;

        [Tooltip("Spawn under a given GameObject")]
        public GameObject SpawnUnderObject = null;





        // Start is called before the first frame update
        void Start()
        {
            if (SpawnOnStart)
                this.Build(HideOnStart);
        }

        // Update is called once per frame
        void Update()
        {

        }



        public void EVENT_Build(bool hidden = false) => this.Build(hidden);

        public LogScreenBaseHandle Build(bool hidden = false)
        {
            GameObject prefab = Resources.Load(PrefabPath) as GameObject;
            if (prefab == null)
            {
                Debug.LogError($"[LogScreenBaseBuilder] ERROR: cannot find prefab '{PrefabPath}'");
                return null;
            }

            GameObject obj = Instantiate(prefab);
            obj.name = InitName;
            LogScreenBaseHandle logHandle = obj.GetComponent<LogScreenBaseHandle>();
            if (hidden)
                logHandle.EVENT_Hide();

            // unordered events calling issue!
            logHandle.EVENT_LogTitle(InitTitle);
            logHandle.EVENT_LogContent(InitContent);
            logHandle.EVENT_SolverOffset(InitLocalOffset);

            if (SpawnUnderObject != null)
                logHandle.transform.SetParent(SpawnUnderObject.transform);

            return logHandle;
        }
    }
}

