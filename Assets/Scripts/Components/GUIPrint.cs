using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Project.Scripts.Utils;

namespace Project.Scripts.Components
{
    public class GUIPrint : MonoBehaviour
    {
        // Start is called before the first frame update
        public void EVENT_Info(string msg)
        {
            StaticLogger.Info(this.gameObject, msg, logLayer: 0);
        }

        public void EVENT_Warn(string msg)
        {
            StaticLogger.Warn(this.gameObject, msg, logLayer: 0);
        }

        public void EVENT_Err(string msg)
        {
            StaticLogger.Err(this.gameObject, msg, logLayer: 0);
        }
    }
}
