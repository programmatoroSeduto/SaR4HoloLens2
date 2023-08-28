using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnableMe : MonoBehaviour
{

    private void OnEnable()
    {
        Debug.Log("OnEnable!");
    }

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Start!");
    }
}
