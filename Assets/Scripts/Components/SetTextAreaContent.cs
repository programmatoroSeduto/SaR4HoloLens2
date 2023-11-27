using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Project.Scripts.Utils;
using TMPro;

namespace Project.Scripts.Components
{
    public class SetTextAreaContent : ProjectMonoBehaviour
    {
        public bool SetOnStart = true;
        public bool KeepUpdated = false;
        public List<ProjectMonoBehaviour> Sources = new List<ProjectMonoBehaviour>();
        public List<TextMeshPro> Destinations = new List<TextMeshPro>();

        void Start()
        {
            if(SetOnStart)
            {
                string ss = "";
                foreach(ProjectMonoBehaviour pmb in Sources)
                    ss += pmb.ComponentInfos + "\n";
                foreach (TextMeshPro tmp in Destinations)
                    tmp.text = ss;
            }

            if (!KeepUpdated) this.enabled = false;
        }

        void Update()
        {
            if (KeepUpdated)
            {
                string ss = "";
                foreach (ProjectMonoBehaviour pmb in Sources)
                    ss += pmb.ComponentInfos + "\n";
                foreach (TextMeshPro tmp in Destinations)
                    tmp.text = ss;
            }
        }
    }
}
