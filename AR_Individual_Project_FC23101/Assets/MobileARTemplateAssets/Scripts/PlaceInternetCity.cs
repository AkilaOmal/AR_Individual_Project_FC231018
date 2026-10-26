using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class PlaceInternetCity : MonoBehaviour
{
    [SerializeField] private GameObject internetCityPrefab;
    [SerializeField] private ARRaycastManager raycastManager;

    private GameObject spawnedCity;

    private static readonly List<ARRaycastHit> hits =
    new List<ARRaycastHit>();

    void Update()
    {
        if (spawnedCity != null)
            return;

        if (Touchscreen.current == null)
            return;

        if (!Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            return;

        Vector2 touchPosition =
        Touchscreen.current.primaryTouch.position.ReadValue();

        if (raycastManager.Raycast(
        touchPosition,
        hits,
        TrackableType.PlaneWithinPolygon))
        {
            Pose hitPose = hits[0].pose;

            spawnedCity = Instantiate(
            internetCityPrefab,
            hitPose.position,
            hitPose.rotation
            );
        }
    }
}