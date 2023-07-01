using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// it shall be applied on the object containing the minimap (just tracking, no geometrical changes are performed by this script)
public class MinimapStructure : MonoBehaviour
{
    public bool VisualizeOnInsert = true;

    private List<MinimapStructureEntry> TrackingList = new List<MinimapStructureEntry>();      // MinimapStructureEntry
    private List<GameObject> VisualizationList = new List<GameObject>(); // GameObject

    private float MinOrderCriterion = float.MaxValue;
    private float MaxOrderCriterion = float.MinValue;

    private class MinimapStructureEntry
    {
        public GameObject Object = null;
        public float OrderCriterion = float.NaN;

        public MinimapStructureEntry(GameObject go = null, float hg = float.NaN)
        {
            this.Object = go;
            this.OrderCriterion = hg;
        }
    }



    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnDisable()
    {
        foreach (MinimapStructureEntry item in TrackingList)
            item.Object.SetActive(true);
    }

    void OnEnable()
    {
        foreach (MinimapStructureEntry item in TrackingList)
            if(!VisualizationList.Contains(item.Object))
                item.Object.SetActive(false);
            else
                item.Object.SetActive(true);
    }


    // ordered insert using a custom criterion
    public int TrackGameObject(GameObject newGo, float orderCriterion = float.NaN, Nullable<bool> visualize = null)
    {
        if (newGo == null) return -1;
        if (!this.isActiveAndEnabled) return -1;

        float hg = (orderCriterion == float.NaN ? newGo.transform.localPosition.y : orderCriterion);
        MinimapStructureEntry toInsert = new MinimapStructureEntry(newGo, orderCriterion);

        int at = -1;

        // insert ordered by hg increasing
        if (TrackingList.Count == 0)
        {
            TrackingList.Add( toInsert );
            at = 0;
            MinOrderCriterion = hg;
            MaxOrderCriterion = hg;
        }
        else
        {
            for(int i=0; i<TrackingList.Count; ++i)
            {
                MinimapStructureEntry item = TrackingList[i] as MinimapStructureEntry;
                bool found = false;
                if(orderCriterion <= item.OrderCriterion)
                {
                    TrackingList.Insert(i, toInsert);
                    found = true;
                    at = i;
                }
                if (found) 
                    break;
                else if (i == TrackingList.Count - 1)
                {
                    TrackingList.Add(toInsert);
                    at = i+1;
                }
                
            }

            if (orderCriterion < MinOrderCriterion)
                MinOrderCriterion = orderCriterion;
            else if (orderCriterion > MaxOrderCriterion)
                MaxOrderCriterion = orderCriterion;
        }

        // visualization
        ToggleVisualizationItem(newGo, opt: (bool)visualize || VisualizeOnInsert);

        return at;
    }

    public void UntrackGameObject(GameObject go, float orderCriterion = float.NaN)
    {
        if (go == null) return;
        if (!this.isActiveAndEnabled) return;

        if (!float.IsNaN(orderCriterion) && (orderCriterion < MinOrderCriterion || orderCriterion > MaxOrderCriterion))
            return;

        foreach (MinimapStructureEntry it in TrackingList)
            if(it.Object == go)
            {
                TrackingList.Remove(it);
                return;
            }
    }


    // visualize one particular game object
    public void ShowItem(GameObject go, bool quick = false)
    {
        if (!this.isActiveAndEnabled) return;
        if (!quick && VisualizationList.Contains(go)) return;
        
        VisualizationList.Add(go);
        go.SetActive(true);
    }

    // visualize one particular game object
    public void HideItem(GameObject go)
    {
        if (!this.isActiveAndEnabled) return;
        VisualizationList.Remove(go);
        go.SetActive(false);
    }

    public void ToggleVisualizationItem(GameObject go, bool opt = true)
    {
        if (!this.isActiveAndEnabled) return;
        if (opt)
            ShowItem(go);
        else
            HideItem(go);
    }

    // hide all or show all
    public void ToggleVisualizationAll(bool opt = true)
    {
        if (!this.isActiveAndEnabled) return;
        if (opt)
            foreach (MinimapStructureEntry it in TrackingList)
                ShowItem(it.Object);
        else
            HideItemsInVisualizationList();
    }

    // hide elements in visualization list
    public void HideItemsInVisualizationList()
    {
        if (!this.isActiveAndEnabled) return;
        while (VisualizationList.Count > 0) HideItem(VisualizationList[0]);
    }

    // hide or show items in a given interval for the order criterion
    public void ShowItemsInRange(float MinHG, float deltaHG, bool hideAllBeforeStarting = false)
    {
        if (!this.isActiveAndEnabled) return;
        MinHG = Mathf.Max(new float[] { MinHG, MinOrderCriterion });

        int startIdx = Mathf.FloorToInt(
            (MinHG - MinOrderCriterion) / (MaxOrderCriterion - MinOrderCriterion) * TrackingList.Count
        );
        float startHG = ((MinimapStructureEntry)TrackingList[startIdx]).OrderCriterion;

        int dir = (startHG > MinHG ? -1 : +1);
        while (startHG != MinHG)
        {
            int newStartIdx = startIdx + dir;
            if (newStartIdx < 0) break; // can't improve further
            float newStartHG = ((MinimapStructureEntry)TrackingList[newStartIdx]).OrderCriterion;

            if ((dir > 0) && (startHG > MinHG)) break;
            else if ((dir < 0) && (startHG < MinHG)) break;

            startIdx = newStartIdx;
            startHG = newStartHG;
        }

        bool quick = hideAllBeforeStarting;
        if (hideAllBeforeStarting) HideItemsInVisualizationList();

        int i = startIdx;
        while((startHG < MinHG + deltaHG) && (i < TrackingList.Count))
        {
            ShowItem(((MinimapStructureEntry)TrackingList[i]).Object, quick);
            startHG = ((MinimapStructureEntry)TrackingList[i]).OrderCriterion;
            ++i;
        }
    }
}
