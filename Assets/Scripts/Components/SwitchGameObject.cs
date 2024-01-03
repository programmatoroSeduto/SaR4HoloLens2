using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Project.Scripts.Utils;

namespace Project.Scripts.Components
{
    public class SwitchGameObject : ProjectMonoBehaviour
    {
        [Tooltip("First active state")]
        public bool Status = true;

        private void Start()
        {
            this.gameObject.SetActive(Status);
        }

        public void EVENT_SwitchGameObject()
        {
            Status = !Status;
            this.gameObject.SetActive(Status);
        }
    }
}
