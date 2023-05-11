using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using TMPro;






namespace Packages.VisualItems.ARMarker.Components
{
    public class ARMarkerBaseHandle : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            
        }

        public void EVENT_SET_TextContent( string txt )
        {
            gameObject.transform.Find("MarkerText/MarkerTextContent").GetComponent<TextMeshPro>().text = txt;
        }
    }
}

