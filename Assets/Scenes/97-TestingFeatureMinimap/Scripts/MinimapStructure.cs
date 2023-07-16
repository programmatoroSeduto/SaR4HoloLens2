using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SaR4Hololens2.Scenes.TestingFeatureMinimap.Scripts
{
    // it shall be applied on the object containing the minimap (just tracking, no geometrical changes are performed by this script)
    public class MinimapStructure : MonoBehaviour
    {
        // ===== GUI&PUBLICS ===== //

        [Header("Main settings")]
        [Tooltip("Enable the object immediately after the visualization. If this option is disabled, the object is deactivated as the insert is performed.")]
        public bool VisualizeOnInsert = true;



        // ===== PRIVATE ===== //

        // the type of the structure (default: ordered)
        private bool isOrdered = true;
        // the main data structure
        private List<MinimapStructureEntry> TrackingList = new List<MinimapStructureEntry>();
        // A set of tracked game objects
        private HashSet<GameObject> TrackingSetGo = new HashSet<GameObject>();
        // cache of the currently enabled elemements
        private HashSet<GameObject> VisualizationList = new HashSet<GameObject>();
        // indexing by tag
        private Dictionary<string, MinimapStructureEntry> TrackingListByTag = new Dictionary<string, MinimapStructureEntry>();

        private float MinOrderCriterion = float.MaxValue;
        private float MaxOrderCriterion = float.MinValue;

        private class MinimapStructureEntry
        {
            public GameObject Object = null;
            public float OrderCriterion = float.NaN;
            public string ObjectTag = null;

            public bool HasTag
            {
                get => ObjectTag != null;
            }

            public MinimapStructureEntry(GameObject go = null, float hg = float.NaN, string tag = null)
            {
                this.Object = go;
                this.OrderCriterion = hg;
                this.ObjectTag = tag;
            }
        }



        // ===== UNITY CALLBACKS ===== //


        void OnDisable()
        {
            /* TODO : (OnDisable) Definire delle opzioni per dare il comportamento delle callbacks OnEnable e OnDisable */
            foreach (MinimapStructureEntry item in TrackingList)
                item.Object.SetActive(true);
        }

        void OnEnable()
        {
            /* TODO : (OnEnable) vedi OnDisable */
            foreach (MinimapStructureEntry item in TrackingList)
                if (!VisualizationList.Contains(item.Object))
                    item.Object.SetActive(false);
                else
                    item.Object.SetActive(true);
        }



        // ===== FEATURE TRACK UNTRACK ===== //

        /*
         * Per poter impostare la struttura come unordered, occorre che questa sia vuota. Altrimenti, non è possibile. 
         * 
         * con questo metodo di chiamata, la funzione potrebbe fallire
         * SetOrderedStructure(false);
         * 
         * la seguente, per settare la struttura a unordered, (non fallisce mai)
         * SetOrderedStructure(false, clearBefore: true|false)
         * 
         * equivale a (e non fallisce mai)
         * UntrackAll();
         * SetOrderedStructure(false);
         * 
         * ci possono essere casi in cui la struttura conteneva degli oggetti che non voglio che vengano disattivati; c'è un parametro per questo:
         * SetOrderedStructure(false, clearBefore: true, visualize: true)
         * */
        public bool SetOrderedStructure(bool opt = true, bool clearBefore = false, Nullable<bool> visualize = null)
        {
            if (clearBefore)
                UntrackAll(visualize);
                
            if (TrackingList.Count > 0)
                return false;

            isOrdered = opt;
            return true;
        }

        // ordered insert using a custom criterion
        /*
         * Tracking con criterio di ordinamento:
         * TrackGameObject(go, "thisobject", orderCriterion: 0.64, visualize:true|false);
         * 
         * Tracking usando come criterio la y rispetto al local frame:
         * TrackGameObject(go, "thisobject", visualize:true|false);
         * 
         * Tracking senza criterio di ordinamento:
         * TrackGameObject(go, "thisobject", ignoreOrderCriterion:true|false, visualize:true|false);
         * */
        public bool TrackGameObject(GameObject newGo, string goTag, float orderCriterion = float.NaN, Nullable<bool> visualize = null, bool ignoreOrderCriterion = false)
        {
            if (!this.isActiveAndEnabled) return false;
            if (newGo == null || goTag == "") return false;
            if (!ignoreOrderCriterion && float.IsNaN(orderCriterion)) return false;
            if (ignoreOrderCriterion && isOrdered && !SetOrderedStructure(false)) return false;

            if (TrackingListByTag.ContainsKey(goTag)) return false;

            float hg = (orderCriterion == float.NaN && !ignoreOrderCriterion? newGo.transform.localPosition.y : orderCriterion);
            MinimapStructureEntry toInsert = new MinimapStructureEntry(newGo, orderCriterion, goTag);

            if(ignoreOrderCriterion || !isOrdered)
                StartTrackItem(toInsert, append: true);
            else
            {
                // insert ordered by hg increasing
                if (TrackingList.Count == 0)
                {
                    StartTrackItem(toInsert, append:true);
                    MinOrderCriterion = hg;
                    MaxOrderCriterion = hg;
                }
                else
                {
                    for (int i = 0; i < TrackingList.Count; ++i)
                    {
                        MinimapStructureEntry item = TrackingList[i];
                        bool found = false;
                        if (orderCriterion <= item.OrderCriterion)
                        {
                            StartTrackItem(toInsert, at: i);
                            found = true;
                        }
                        
                        if (found) break;
                        else if (i == TrackingList.Count - 1)
                            StartTrackItem(toInsert, append: true);
                    }

                    if (orderCriterion < MinOrderCriterion)
                        MinOrderCriterion = orderCriterion;
                    else if (orderCriterion > MaxOrderCriterion)
                        MaxOrderCriterion = orderCriterion;
                }
            }

            // visualization
            visualize = (visualize == null ? VisualizeOnInsert : visualize);
            SetVisualizationItem(goTag, opt: (bool)visualize || VisualizeOnInsert);

            return true;
        }

        /*
         * inserimento in coda:
         * StartTrackItem(toInsert, append:true);
         * 
         * inserimento in testa:
         * StartTrackItem(toInsert, append:false);
         * 
         * inserimento con indice esplicito:
         * StartTrackItem(toInsert, 24);
         * */
        private void StartTrackItem(MinimapStructureEntry toInsert, int at = 0, bool append = false)
        {
            TrackingSetGo.Add(toInsert.Object);

            if (append)
                TrackingList.Add(toInsert);
            else
                TrackingList.Insert(at, toInsert);

            if (toInsert.HasTag) 
                TrackingListByTag.Add(toInsert.ObjectTag, toInsert);
        }

        /*
         * uso generale:
         * UntrackGameObject("thisobject", visualize:true|false)
         * 
         * dando il suggerimento del criterio di ordinamento:
         * UntrackGameObject("thisobject", orderCriterion: 50.6, visualize:true|false)
         * */
        public bool UntrackGameObject(string goTag, float orderCriterion = float.NaN, Nullable<bool> visualize = null, bool destroy = false)
        {
            if (!this.isActiveAndEnabled) return false;
            if (goTag == "") return false;

            if (!TrackingListByTag.ContainsKey(goTag)) 
                return false;
            else
            {
                return UntrackGameObject(TrackingListByTag[goTag].Object, orderCriterion, visualize, destroy);
            }
        }

        /*
         * uso generale:
         * UntrackGameObject(GameObject(), visualize:true|false)
         * 
         * dando il suggerimento del criterio di ordinamento: (il DB comprende attualmente un criterio di ordinamento?)
         * UntrackGameObject(GameObject(), orderCriterion: 50.6, visualize:true|false)
         * */
        private bool UntrackGameObject(GameObject go, float orderCriterion = float.NaN, Nullable<bool> visualize = null, bool destroy = false)
        {
            if (!float.IsNaN(orderCriterion) && (orderCriterion < MinOrderCriterion || orderCriterion > MaxOrderCriterion))
                return false;

            foreach (MinimapStructureEntry it in TrackingList)
                if (it.Object == go)
                {
                    TrackingList.Remove(it);
                    TrackingSetGo.Remove(it.Object);
                    if(it.HasTag)
                        TrackingListByTag.Remove(it.ObjectTag);

                    HideItem(go, dontTurnOff: (visualize != null ? (bool)visualize : false));

                    if (destroy) GameObject.DestroyImmediate(go);

                    return true;
                }

            return false;
        }

        public bool UntrackAll(Nullable<bool> visualize = null, bool destroy = false)
        {
            if (!this.isActiveAndEnabled) return false;

            // foreach (GameObject go in TrackingSetGo)
            while (TrackingList.Count > 0)
                UntrackGameObject(TrackingList[0].Object, visualize: visualize, destroy: destroy);

            // useless, BUT, since the Untrack could return False...
            if (TrackingSetGo.Count == 0 && TrackingList.Count == 0 && TrackingListByTag.Count == 0)
                return true;
            else
                return false;
        }

        public bool IsTrackedItem(string goTag)
        {
            return TrackingListByTag.ContainsKey(goTag);
        }

        public GameObject TryGetItemGameObject(string goTag)
        {
            if (IsTrackedItem(goTag))
                return TrackingListByTag[goTag].Object;
            else
                return null;
        }

        public float TryGetItemOrderCriterion(string goTag)
        {
            if (IsTrackedItem(goTag))
                return TrackingListByTag[goTag].OrderCriterion;
            else
                return float.NaN;
        }




        // ===== FEATURE SHOW HIDE BASIC ===== //

        // visualize one particular game object
        /*
         * in generale:
         * ShowItem(GameObject());
         * 
         * con l'opzione quick, il sistema si risparmia il check di esistenza:
         * ShowItem(GameObject(), quick: true|false);
         * */
        private bool ShowItem(GameObject go, bool quick = false)
        {
            if (!quick && (!TrackingSetGo.Contains(go)|| VisualizationList.Contains(go))) return false;

            VisualizationList.Add(go);
            go.SetActive(true);

            return true;
        }

        /*
         * in generale:
         * ShowItem("thistag");
         * 
         * con l'opzione quick, il sistema si risparmia il check di esistenza:
         * ShowItem("thistag", quick: true|false);
         * */
        public bool ShowItem(string goTag, bool quick = false)
        {
            if (!this.isActiveAndEnabled) return false;
            if (!TrackingListByTag.ContainsKey(goTag)) return false;

            return ShowItem(TrackingListByTag[goTag].Object, quick);
        }

        // visualize one particular game object
        /*
         * Chiamata da fare nel 99.9% dei casi:
         * HideItem(GameObject());
         * 
         * Se l'oggetto non è stato tracciato, allora è possibile anche fare questo (inutile se al di fuori del contesto di questa classe):
         * HideItem(GameObject(), dontTurnOff: true|false);
         * false: (default) evita che l'oggetto venga disattivato
         * true: lascia lo stato dell'oggetto invariato, non forzare a disattivato
         * l'oggetto non può essere lasciato attivo dopo essere uscito dalla lista di visualizzazione se è ancora tracciato dalla classe
         * */
        private bool HideItem(GameObject go, bool dontTurnOff = false)
        {
            if(!VisualizationList.Remove(go)) 
                return false;
            
            if(!dontTurnOff && TrackingSetGo.Contains(go))
                go.SetActive(false);
            
            return true;
        }

        // qui non è necessario il parametro dontTurnOff
        public bool HideItem(string goTag)
        {
            if (!this.isActiveAndEnabled) return false;
            if (!TrackingListByTag.ContainsKey(goTag)) return false;

            return HideItem(TrackingListByTag[goTag].Object);
        }



        // ===== FEATURE SHOW HIDE MASSIVE ===== //

        public bool SetVisualizationItem(string goTag, bool opt = true)
        {
            if (!this.isActiveAndEnabled) return false;
            if (!TrackingListByTag.ContainsKey(goTag)) return false;

            return (opt ? ShowItem(TrackingListByTag[goTag].Object) : HideItem(TrackingListByTag[goTag].Object));
        }

        // hide all or show all
        public void SetVisualizationAll(bool opt = true)
        {
            if (!this.isActiveAndEnabled) return;

            foreach (MinimapStructureEntry it in TrackingList)
            {
                if (opt)
                    ShowItem(it.Object, quick: true);
                else
                    HideItem(it.Object);
            }
        }

        // hide or show items in a given interval for the order criterion
        public bool ShowItemsInRange(float MinHG, float deltaHG, bool hideAllBeforeStarting = false)
        {
            if (!this.isActiveAndEnabled) return false;
            if (!isOrdered) return false;
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

            if (hideAllBeforeStarting) SetVisualizationAll(false);

            bool quick = hideAllBeforeStarting;
            int i = startIdx;
            while ((startHG < MinHG + deltaHG) && (i < TrackingList.Count))
            {
                ShowItem(((MinimapStructureEntry)TrackingList[i]).Object, quick);
                startHG = ((MinimapStructureEntry)TrackingList[i]).OrderCriterion;
                ++i;
            }

            return true;
        }
    }

}