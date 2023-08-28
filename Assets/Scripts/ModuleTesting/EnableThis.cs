using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnableThis : MonoBehaviour
{
    public EnableMe toEnable = null;
    public float EnableDelay = 5.0f;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(COR_EnableThat());
    }

    // Update is called once per frame
    IEnumerator COR_EnableThat()
    {
        Debug.Log("Waiting ...");
        yield return new WaitForSecondsRealtime(EnableDelay);
        Debug.Log("Waiting ... ENABLE");
        toEnable.enabled = true;
    }
}
