using System.Collections;

using System.Collections.Generic;
using UnityEngine;

namespace SaR4Hololens2.Scenes.TestingFeatureMinimap.Scripts
{
    public class SliderTool : MonoBehaviour
    {
        // === GUI ===

        [Header("General Tool Settings")]
        [Tooltip("The object controlling the minimap")]
        public MinimapStructure MinimapDriver = null;
        [Tooltip("Prefix of the name of the element under the minimap root to track (empty if not userd)")]
        public string ItemsPrefix = "";



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
        // used for counting the objects
        private int counter = 0;


        // === UNITY CALLBACKS ===

        // Start is called before the first frame update
        void Start()
        {
            if (MinimapDriver == null)
            {
                Debug.LogError("(Slider) ERROR: no Minimap Structure provided!");
                return;
            }
            ItemsPrefix = ItemsPrefix.Trim();

            minimapRoot = MinimapDriver.gameObject;
            slider = gameObject;
            init = true;
        }

        // Update is called once per frame
        void Update()
        {
            if (!this.isActiveAndEnabled) return;
            if (!init) return;

            if (!tracked)
            {
                foreach (Transform child in minimapRoot.transform) // https://discussions.unity.com/t/get-all-children-gameobjects/89443/3
                {
                    if (ItemsPrefix == "" || child.gameObject.name.StartsWith(ItemsPrefix))
                        MinimapDriver.TrackGameObject(child.gameObject, "OBJ" + counter.ToString("0000"), orderCriterion: child.gameObject.transform.localPosition.y, visualize: false);
                }

                tracked = true;
            }

            delta = slider.transform.localScale.y;
            float newystart = slider.transform.localPosition.y - delta / 2.0f;

            if (newystart != ystart && MinimapDriver.isActiveAndEnabled)
            {
                ystart = newystart;
                MinimapDriver.ShowItemsInRange(ystart, delta, true);
            }
        }

        private void OnEnable()
        {

        }

        private void OnDisable()
        {

        }
    }

}