using System.Collections;

using System.Collections.Generic;
using UnityEngine;

public class SliderTool : MonoBehaviour
{
    // === GUI ===

    [Header("General Tool Settings")]
    [Tooltip("The object controlling the minimap")]
    public MinimapStructure MinimapDriver = null;



    // === PRIVATE ===

    // init done
    private bool init = false;
    // after tracked everything under the root of the minimap
    private bool tracked = false;
    // root of the minimap
    private GameObject minimapRoot = null;
    // reference to the slider
    private GameObject slider;
    // slider data
    float ystart = float.MaxValue;
    float delta = 0.0f;


    // === UNITY CALLBACKS ===

    // Start is called before the first frame update
    void Start()
    {
        if (MinimapDriver == null)
        {
            Debug.LogError("no Minimap Structure provided!");
            return;
        }

        minimapRoot = MinimapDriver.gameObject;
        slider = gameObject;
        init = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (!init) return;

        if (!tracked)
        {
            foreach (Transform child in minimapRoot.transform) // https://discussions.unity.com/t/get-all-children-gameobjects/89443/3
            {
                MinimapDriver.TrackGameObject(child.gameObject, child.gameObject.transform.localPosition.y, visualize: false);
            }
                
            tracked = true;
        }
        
        float newystart = slider.transform.localPosition.y - slider.transform.localScale.y / 2.0f;

        if(newystart != ystart)
        {
            ystart = newystart;
            delta = slider.transform.localScale.y;

            MinimapDriver.ShowItemsInRange(ystart, delta, true);
        }
    }
}
