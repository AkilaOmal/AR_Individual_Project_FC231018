using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class PlacementController : MonoBehaviour
{
    [SerializeField] ARRaycastManager raycastManager;
    [SerializeField] ARPlaneManager planeManager;
    public System.Action<Pose> OnPlacePoseChosen;

    static readonly List<ARRaycastHit> hits = new();
    public bool PlacementEnabled { get; set; } = true;

    void Update()
    {
        if (!PlacementEnabled || Input.touchCount == 0) return;
        var touch = Input.GetTouch(0);
        if (touch.phase != TouchPhase.Began) return;
        if (EventSystem.current.IsPointerOverGameObject(touch.fingerId)) return; // ignore UI taps

        if (raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
            OnPlacePoseChosen?.Invoke(hits[0].pose);
    }

    public void HidePlanes()
    {
        foreach (var p in planeManager.trackables) p.gameObject.SetActive(false);
        planeManager.enabled = false;
    }
}