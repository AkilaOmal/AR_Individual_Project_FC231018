//using System;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.EventSystems;
//using UnityEngine.XR.ARFoundation;
//using UnityEngine.XR.ARSubsystems;

//public class PlacementController : MonoBehaviour
//{
//    [SerializeField] private ARRaycastManager raycastManager;

//    public bool PlacementEnabled { get; set; } = false;

//    public event Action<Pose> OnPlacePoseChosen;

//    private readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();

//    private void Awake()
//    {
//        if (raycastManager == null)
//            raycastManager = FindFirstObjectByType<ARRaycastManager>();

//        if (raycastManager == null)
//            Debug.LogError("PlacementController: ARRaycastManager not found.");
//    }

//    private void Update()
//    {
//        if (!PlacementEnabled) return;
//        if (Input.touchCount == 0) return;

//        Touch touch = Input.GetTouch(0);
//        if (touch.phase != TouchPhase.Began) return;

//        // Ignore touches on UI buttons
//        if (EventSystem.current != null &&
//            EventSystem.current.IsPointerOverGameObject(touch.fingerId))
//            return;

//        if (raycastManager == null) return;

//        if (raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
//        {
//            Pose pose = hits[0].pose;

//            // Lock BEFORE firing the event: only one placement per selection
//            PlacementEnabled = false;
//            OnPlacePoseChosen?.Invoke(pose);
//        }
//    }
//}
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class PlacementController : MonoBehaviour
{
    [SerializeField] private ARRaycastManager raycastManager;

    [Header("Editor testing only (PC)")]
    [SerializeField] private float editorFloorDrop = 0.6f;     // fake floor this far below the camera
    [SerializeField] private float editorFallbackDistance = 1.0f;

    public bool PlacementEnabled { get; set; } = false;

    public event Action<Pose> OnPlacePoseChosen;

    private readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();

    private void Awake()
    {
        if (raycastManager == null)
            raycastManager = FindFirstObjectByType<ARRaycastManager>();

        if (raycastManager == null)
            Debug.LogError("PlacementController: ARRaycastManager not found.");
    }

    private void Update()
    {
        if (!PlacementEnabled) return;

#if UNITY_EDITOR
        EditorPlacement();
#else
        PhonePlacement();
#endif
    }

    // ---------- Real phone placement (AR plane) ----------
    private void PhonePlacement()
    {
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);
        if (touch.phase != TouchPhase.Began) return;

        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            return;

        if (raycastManager == null) return;

        if (raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
            Place(hits[0].pose);
    }

    // ---------- PC test placement (mouse click, no AR needed) ----------
#if UNITY_EDITOR
    private void EditorPlacement()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
            return;

        Camera cam = Camera.main;
        if (cam == null) return;

        // Imaginary floor below the camera
        Plane floor = new Plane(Vector3.up,
            new Vector3(0f, cam.transform.position.y - editorFloorDrop, 0f));

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Vector3 point;

        if (floor.Raycast(ray, out float enter) && enter < 10f)
        {
            point = ray.GetPoint(enter);
        }
        else
        {
            // Fallback: straight ahead of the camera, slightly below
            Vector3 flatForward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
            point = cam.transform.position + flatForward * editorFallbackDistance;
            point.y = cam.transform.position.y - editorFloorDrop;
        }

        Place(new Pose(point, Quaternion.identity));
    }
#endif

    private void Place(Pose pose)
    {
        // Lock first so only one module is placed per selection
        PlacementEnabled = false;
        OnPlacePoseChosen?.Invoke(pose);
    }
}