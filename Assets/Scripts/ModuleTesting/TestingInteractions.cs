using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;

public class TestingInteractions : MonoBehaviour
{
    public void EVENT_PlaceCubeInFrontOfUser(float distance)
    {
        float yaw = Mathf.Deg2Rad * Camera.main.transform.rotation.eulerAngles.y;
        Debug.Log($"Yaw={yaw}");
        Vector3 pos = Camera.main.transform.position + distance * (Mathf.Sin(yaw) * Vector3.right + Mathf.Cos(yaw) * Vector3.forward) - 0.25f * Vector3.up;
        PlaceObjectInPlace(PrimitiveType.Cube, pos);
    }
    public void EVENT_PlaceSphereInFrontOfUser(float distance)
    {
        float yaw = Mathf.Deg2Rad * Camera.main.transform.rotation.eulerAngles.y;
        Debug.Log($"Yaw={yaw}");
        Vector3 pos = Camera.main.transform.position + distance * (Mathf.Sin(yaw) * Vector3.right + Mathf.Cos(yaw) * Vector3.forward) - 0.25f * Vector3.up;
        PlaceObjectInPlace(PrimitiveType.Sphere, pos);
    }
    public void EVENT_PlaceCapsuleInFrontOfUser(float distance)
    {
        float yaw = Mathf.Deg2Rad * Camera.main.transform.rotation.eulerAngles.y;
        Debug.Log($"Yaw={yaw}");
        Vector3 pos = Camera.main.transform.position + distance * (Mathf.Sin(yaw) * Vector3.right + Mathf.Cos(yaw) * Vector3.forward) - 0.25f * Vector3.up;
        PlaceObjectInPlace(PrimitiveType.Capsule, pos);
    }

    private void PlaceObjectInPlace(PrimitiveType type, Vector3 pos)
    {
        Debug.Log($"camera: {Camera.main.transform.position} -- place: {pos}");
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.transform.localScale *= 0.1f;
        obj.transform.position = pos;
        obj.AddComponent<ObjectManipulator>();
        obj.AddComponent<NearInteractionGrabbable>();
    }
}
