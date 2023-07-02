using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Packages.ExplorationServices.Components
{
    public class TestVisualAllocationHandle : MonoBehaviour
    {
        public GameObject AllocationRootObject = null;
        public GameObject TemplateObject = null;

        private bool active = false;
        private Dictionary<int, GameObject> Buffer = new Dictionary<int, GameObject>();
        
        void Start()
        {
            if(AllocationRootObject == null)
            {
                Debug.LogWarning("ERROR: 'AllocationRootObject' is null!");
                return;
            }
            if(TemplateObject == null)
            {
                Debug.LogWarning("ERROR: 'TemplateObject' is null! Nothing to replicate");
                return;
            }

            active = true;
        }

        public GameObject Allocate(int index, string label, Vector3 position, Quaternion orientation)
        {
            GameObject go = AccessObject(index);
            if(go == null)
            {
                go = Instantiate(TemplateObject, position, orientation);
                go.transform.SetParent(AllocationRootObject.transform);
                Buffer.Add(index, go);
            }

            go.SetActive(true);
            if(label != "") go.name = label;

            return go;
        }

        public void DeallocateAll(bool destroy = false)
        {
            foreach (KeyValuePair<int, GameObject> kp in Buffer)
                Deallocate(kp.Key, destroy);
        }

        public GameObject Deallocate(int index, bool destroy = false)
        {
            GameObject go = AccessObject(index);
            if (go == null) return null;

            go.SetActive(false);
            if(destroy)
            {
                Destroy(go);
                Buffer.Remove(index);

                return null;
            }

            return go;
        }

        private GameObject AccessObject(int index)
        {
            GameObject go = null;
            Buffer.TryGetValue(index, out go);

            return go;
        }
    }
}
