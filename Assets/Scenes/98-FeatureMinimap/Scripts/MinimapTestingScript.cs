using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.UI.BoundsControl;

public class MinimapTestingScript : MonoBehaviour
{
    // === GUI ===

    [Header("Component Functions")]
    [Tooltip("Randomly populate the root object")]
    public bool FunctionInstanciateRandom = false;
    [Tooltip("Create the minimap")]
    public bool FunctionCreateFrameMinimap = false;
    [Tooltip("Instanciate the plane visual tool for the minimap")]
    public bool FunctionCreatePlaneTool = false;

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

    [Header("Function: create plane tool")]
    [Tooltip("Root G.O. for the minimap asset")]
    public GameObject AssetRoot = null;
    [Tooltip("Material for the slider tool")]
    public Material ToolMaterial = null;



    // === PRIVATE ===

    // for one-shot script
    private bool done = false;
    // the root object to use for generating the minimap (the script beholding this component)
    private GameObject GoRoot = null;
    // default for NumOfObjects
    private int NumOfObjectsDefault = 5;
    // default for Scale
    private float ScaleDefault = 0.05f;
    // root of the minimap asset
    private GameObject GoAssetRoot = null;
    // minimap gameobject
    private GameObject goMinimap = null;
    // minimap structure component in the minimap
    private MinimapStructure coMinimapStruct = null;
    // tools root for the minimap
    private GameObject goTools = null;



    // === UNITY CALLBACKS ===

    // Start is called before the first frame update
    void Start()
    {
        GoRoot = (ComponentRoot == null ? gameObject : ComponentRoot);
        GoAssetRoot = (FunctionCreatePlaneTool ? (AssetRoot == null ? GoRoot.transform.parent.gameObject : AssetRoot) : null);

        if (FunctionInstanciateRandom)
            WrapperFunctionInstanciateRandom();
    }

    // Update is called once per frame
    void Update()
    {
        if (done) return;

        if (FunctionCreateFrameMinimap)
            WrapperFunctionCreateFrameMinimap();
        else if (FunctionCreatePlaneTool)
            WrapperFunctionCreatePlaneTool();
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



    // === FEATURE PLANE TOOL ===

    private void WrapperFunctionCreatePlaneTool()
    {
        if (CheckMinimapAsset())
            CreatePlaneTool();
        done = true;
    }

    private bool CheckMinimapAsset()
    {
        // find minimap
        Transform trMinimap = GoAssetRoot.transform.Find("Minimap");
        if (trMinimap == null)
        {
            Debug.LogWarning("ERROR: cannot create tool (Minimap not found)");
            return false;
        }
        goMinimap = trMinimap.gameObject;

        // check minimap type
        coMinimapStruct = goMinimap.GetComponent<MinimapStructure>();
        if (coMinimapStruct == null)
        {
            Debug.LogWarning("ERROR: cannot create tool (Minimap is not a minimap)");
            return false;
        }

        // check or create _tools gambe object
        Transform trTools = GoAssetRoot.transform.Find("_tools");
        if (trTools == null)
        {
            Debug.LogWarning("ERROR: cannot create tool (object _tools not found)");
            return false;
        }
        goTools = trTools.gameObject;

        // check material reference
        if(ToolMaterial == null)
        {
            Debug.LogWarning("ERROR: tool material for the slider tool has not been set!");
            return false;
        }

        return true;
    }

    private void CreatePlaneTool()
    {
        GameObject goPlaneTool = GameObject.CreatePrimitive(PrimitiveType.Cube);
        goPlaneTool.name = "MinimapSliderTool";
        goPlaneTool.transform.SetParent(goTools.transform);
        goPlaneTool.transform.localPosition = new Vector3(0.5f, 0.0f, 0.5f);
        goPlaneTool.transform.localScale = new Vector3(1.2f, 0.1f, 1.2f);
        goPlaneTool.GetComponent<Renderer>().material = ToolMaterial;

        goPlaneTool.AddComponent<NearInteractionGrabbable>();
        goPlaneTool.AddComponent<ObjectManipulator>();

        ConstraintManager coConstraints = goPlaneTool.GetComponent<ConstraintManager>();

        MoveAxisConstraint coConstraintX = goPlaneTool.AddComponent<MoveAxisConstraint>();
        coConstraintX.ConstraintOnMovement = AxisFlags.XAxis;
        coConstraintX.UseLocalSpaceForConstraint = true;

        MoveAxisConstraint coConstraintZ = goPlaneTool.AddComponent<MoveAxisConstraint>();
        coConstraintZ.ConstraintOnMovement = AxisFlags.ZAxis;
        coConstraintZ.UseLocalSpaceForConstraint = true;
    }

}
