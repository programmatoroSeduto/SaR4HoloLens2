using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Glue : MonoBehaviour
{
    // === GUI ===

    [Tooltip("The reference object (it shall have always the same relative position of the other object)")]
    public GameObject Reference = null;

    public bool SetPosition = true;
    public bool SetOrientation = false;
    public bool SetScale = false;



    // === Unity Callbacks ===

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Reference == null) return;

        if (SetPosition)
            gameObject.transform.position = Reference.transform.position;
        if (SetOrientation)
            gameObject.transform.rotation = Reference.transform.rotation;
        if (SetScale)
            gameObject.transform.localScale = Reference.transform.localScale;
    }
}
