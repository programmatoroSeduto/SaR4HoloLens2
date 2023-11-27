using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectMonoBehaviour : MonoBehaviour
{
    public bool IsReady { get => isReady; }
    private bool isReady = false;

    [HideInInspector]
    public string ComponentInfos = "";

    public void Ready(bool disableComponent = false)
    {
        isReady = true;
        this.enabled = !disableComponent;
    }
}
