using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextReaderType : MonoBehaviour
{
    public virtual bool EVENT_ReadText(string txt)
    {
        Debug.LogError("IMPLEMENT ME! EVENT_ReadText(string txt)");
        return false;
    }
}
