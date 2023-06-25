using System;
using System.Collections;
using UnityEngine;

// it shall be applied on the object containing the minimap (just tracking, no geometrical changes are performed by this script)
public class MinimapStructure : MonoBehaviour
{
    public bool VisualizeOnInsert = true;

    private ArrayList TrackingList = new ArrayList();      // MinimapStructureEntry
    private ArrayList VisualizationList = new ArrayList(); // GameObject

    private float MinOrderCriterion = float.MaxValue;
    private float MaxOrderCriterion = float.NaN;

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


    // ordered insert using a custom criterion
    public int TrackGameObject(GameObject newGo, float orderCriterion = float.NaN, Nullable<bool> visualize = null)
    {
        if (newGo == null) return -1;

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
        if (!float.IsNaN(orderCriterion) && (orderCriterion < MinOrderCriterion || orderCriterion > MaxOrderCriterion))
            return;

        foreach (MinimapStructureEntry it in TrackingList)
            if(it.Object == go)
            {
                TrackingList.Remove(go);
                return;
            }
    }


    // visualize one particular game object
    public void ShowItem(GameObject go, bool quick = false)
    {
        if (!quick && VisualizationList.Contains(go)) return;
        
        VisualizationList.Add(go);
        go.SetActive(true);
    }

    // visualize one particular game object
    public void HideItem(GameObject go)
    {
        VisualizationList.Remove(go);
        go.SetActive(false);
    }

    public void ToggleVisualizationItem(GameObject go, bool opt = true)
    {
        if (opt)
            ShowItem(go);
        else
            HideItem(go);
    }

    // hide all or show all
    public void ToggleVisualizationAll(bool opt = true)
    {
        if (opt)
            foreach (MinimapStructureEntry it in TrackingList)
                ShowItem(it.Object);
        else
            HideItemsInVisualizationList();
    }

    // hide elements in visualization list
    public void HideItemsInVisualizationList()
    {
        foreach (MinimapStructureEntry it in VisualizationList)
            HideItem(it.Object);
    }

    // hide or show items in a given interval for the order criterion
    public void ShowItemsInRange(bool opt = true, float MinHG = float.NaN, float MaxHG = float.NaN, bool hideAllBeforeStarting = false )
    {
        if (MinHG > MaxHG)
        {
            ShowItemsInRange(opt, MaxHG, MinHG, hideAllBeforeStarting);
            return;
        }
        else if ((MinHG < MinOrderCriterion || MaxHG < MinOrderCriterion) || (MinHG > MaxOrderCriterion || MaxHG > MaxOrderCriterion))
            return;

        MinHG = (float.IsNaN(MinHG) ? MinOrderCriterion : MinHG);
        MaxHG = (float.IsNaN(MaxHG) ? MinOrderCriterion : MaxHG);
        float delta = MaxOrderCriterion - MinOrderCriterion;
        float reqDelta = MaxHG - MinHG;

        int startIdx = Mathf.FloorToInt(((MinHG - MinOrderCriterion)/ delta) *TrackingList.Count);
        int endIdx = Mathf.Max(new int[] { 
            Mathf.CeilToInt((1 - (MaxHG - MinOrderCriterion) / delta) * TrackingList.Count), 
            TrackingList.Count - 1 
        });

        float startHG = ((MinimapStructureEntry)TrackingList[startIdx]).OrderCriterion;
        float endHG = ((MinimapStructureEntry)TrackingList[endIdx]).OrderCriterion;
        float tempDelta = endHG - startHG;

        int startDir = (startHG > MinHG ? -1 : +1);
        while(startHG != MinHG) // beginning index improvement
        {
            int newStartIdx = startIdx + startDir;
            if (newStartIdx < 0 || newStartIdx >= endIdx) break; // can't improve further

            float newStartHG = ((MinimapStructureEntry)TrackingList[newStartIdx]).OrderCriterion;
            float newStartError = Mathf.Abs(reqDelta - (endHG - newStartHG));

            if (newStartError < Mathf.Abs(reqDelta - tempDelta))
            {
                if (((newStartHG < MinHG) && (startDir > 0)) || ((newStartHG > MinHG) && (startDir < 0)))
                {
                    startIdx = newStartIdx;
                    startHG = ((MinimapStructureEntry)TrackingList[startIdx]).OrderCriterion;
                    tempDelta = endHG - startHG;
                }
                else
                    break;
            }
            else break;
        }

        int endDir = (endHG > MaxHG ? -1 : +1);
        while (endHG != MaxHG) // final index improvement
        {
            int newEndIdx = endIdx + endDir;
            if (newEndIdx == TrackingList.Count || newEndIdx <= startIdx) break; // can't improve further

            float newEndtHG = ((MinimapStructureEntry)TrackingList[newEndIdx]).OrderCriterion;
            float newEndError = Mathf.Abs(reqDelta - (newEndtHG - startHG));

            if (newEndError < Mathf.Abs(reqDelta - tempDelta))
            {
                if (((newEndtHG > MaxHG) && (startDir < 0)) || ((newEndtHG < MaxHG) && (startDir > 0)))
                {
                    endIdx = newEndIdx;
                    endHG = ((MinimapStructureEntry)TrackingList[endIdx]).OrderCriterion;
                    tempDelta = endHG - startHG;
                }
                else
                    break;
            }
            else break;
        }

        bool quick = hideAllBeforeStarting;
        if (hideAllBeforeStarting) HideItemsInVisualizationList();

        for (int i = startIdx; i <= endIdx; ++i)
            ShowItem(((MinimapStructureEntry)TrackingList[endIdx]).Object, quick);
    }
}
