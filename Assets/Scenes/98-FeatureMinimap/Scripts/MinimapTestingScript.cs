using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.UI.BoundsControl;

public class MinimapTestingScript : MonoBehaviour
{
    // === GUI ===

    [Header("Component Functions")]
    [Tooltip("Randomly populate the root object")]
    public bool FunctionInstanciateRandom = false;
    [Tooltip("Create the minimap")]
    public bool FunctionCreateFrameMinimap = false;

    [Header("General settings")]
    [Tooltip("The root of the minimap (by defaul, it is the owner of this component)")]
    public GameObject ComponentRoot = null;

    [Header("Function: random objects generation")]
    [Tooltip("Minimap Structure Object Reference (optional)")]
    public MinimapStructure MapStructure = null;
    [Tooltip("Number of objects to spawn")]
    public int NummOfObjects = 5;
    [Tooltip("max distance (local) from coordinates")]
    public Vector3 MaxDistVector = Vector3.zero;
    [Tooltip("Local scaling factor for each object inside the map")]
    public float Scale = 0.01f;
    [Tooltip("Object name (the index is added at the ed of this name)")]
    public string SpawnedObjectName = "cube";

    [Header("Function: Create Minimap")]
    [Tooltip("Box Collider center")]
    public Vector3 BoxCenter = new Vector3(0.5f, -0.5f, 0.5f);



    // === PRIVATE ===

    // for one-shot script
    private bool done = false;
    // the root object to use for generating the minimap (the script beholding this component)
    private GameObject GoRoot = null;
    // default for NumOfObjects
    private int NumOfObjectsDefault = 5;
    // default for Scale
    private float ScaleDefault = 0.05f;



    // === UNITY CALLBACKS ===

    // Start is called before the first frame update
    void Start()
    {
        GoRoot = (ComponentRoot == null ? gameObject : ComponentRoot);
    }

    // Update is called once per frame
    void Update()
    {
        if (done) return;

        if (FunctionInstanciateRandom)
            WrapperFunctionInstanciateRandom();

        else if (FunctionCreateFrameMinimap)
            WrapperFunctionCreateFrameMinimap();
    }



    // === FEATURE INSTANCIATE RANDOM ===

    private void WrapperFunctionInstanciateRandom()
    {
        if (NummOfObjects < 0)
        {
            Debug.LogWarning("Number of Objects unser the minimap cannot be negative; using default");
            NummOfObjects = NumOfObjectsDefault;
        }
        if (Scale < 0.0f)
        {
            Debug.LogWarning("Scaling factor cannot be negative; using default");
            Scale = ScaleDefault;
        }
        GenerateRandomObjectsUnderRoot();
        done = true;
    }

    private void GenerateRandomObjectsUnderRoot() // better to use a coroutine?
    {
        for( int i=0; i<NummOfObjects; ++i )
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = SpawnedObjectName + i.ToString("000");

            // just for testing minimap selection feature
            if (Random.value > 0.5f)
            {
                GameObject go2 = new GameObject();
                go2.transform.SetParent(go.transform);
                go2.transform.position = go.transform.position;
                go2.transform.rotation = go.transform.rotation;
                go2.transform.localScale = go.transform.localScale;
            }

            go.transform.SetParent(GoRoot.transform);
            go.transform.localPosition += new Vector3(Random.value * MaxDistVector.x, -Random.value * MaxDistVector.y, Random.value * MaxDistVector.z);
            go.transform.localScale = Scale * Vector3.one;

            if (MapStructure != null)
                MapStructure.TrackGameObject(go, orderCriterion: go.transform.localPosition.y);
        }
    }



    // === FEATURE SETUP MINIMAP ===

    private void WrapperFunctionCreateFrameMinimap()
    {
        SetupMinimap();
        done = true;
    }

    private void SetupMinimap()
    {
        // box collider
        BoxCollider bc = GoRoot.AddComponent<BoxCollider>();
        bc.center = BoxCenter;

        // frame
        NearInteractionGrabbable nig = GoRoot.AddComponent<NearInteractionGrabbable>();
        BoundsControl frame = GoRoot.AddComponent<BoundsControl>();
        frame.BoundsOverride = bc;
        ObjectManipulator manip = GoRoot.AddComponent<ObjectManipulator>();
    }

}
