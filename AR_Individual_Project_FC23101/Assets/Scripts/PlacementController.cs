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

//    private readonly List<ARRaycastHit> hits =
//    new List<ARRaycastHit>();

//    private void Awake()
//    {
//        if (raycastManager == null)
//        {
//            raycastManager =
//            FindFirstObjectByType<ARRaycastManager>();
//        }

//        if (raycastManager == null)
//        {
//            Debug.LogError(
//            "PlacementController: ARRaycastManager not found."
//            );
//        }
//    }

//    private void Update()
//    {
//        if (!PlacementEnabled)
//            return;

//        if (Input.touchCount == 0)
//            return;

//        Touch touch = Input.GetTouch(0);

//        if (touch.phase != TouchPhase.Began)
//            return;

//        // Ignore touches on UI buttons.
//        if (EventSystem.current != null &&
//        EventSystem.current.IsPointerOverGameObject(
//        touch.fingerId))
//        {
//            return;
//        }

//        if (raycastManager == null)
//            return;

//        // Detect a real-world AR plane.
//        if (raycastManager.Raycast(
//        touch.position,
//        hits,
//        TrackableType.PlaneWithinPolygon))
//        {
//            Pose pose = hits[0].pose;

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
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);
        if (touch.phase != TouchPhase.Began) return;

        // Ignore touches on UI buttons
        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            return;

        if (raycastManager == null) return;

        if (raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
        {
            Pose pose = hits[0].pose;

            // Lock BEFORE firing the event: only one placement per selection
            PlacementEnabled = false;
            OnPlacePoseChosen?.Invoke(pose);
        }
    }
}