using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlaceOnPlane : MonoBehaviour
{
    public GameObject cityPrefab;

    private ARRaycastManager raycastManager;

    private GameObject spawnedCity;

    private static List<ARRaycastHit> hits =
    new List<ARRaycastHit>();

    void Awake()
    {
        raycastManager = GetComponent<ARRaycastManager>();
    }

    void Update()
    {
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

            if (spawnedCity == null)
            {
                spawnedCity = Instantiate(
                cityPrefab,
                hitPose.position,
                Quaternion.identity
                );
            }
            else
            {
                spawnedCity.transform.SetPositionAndRotation(
                hitPose.position,
                Quaternion.identity
                );
            }
        }
    }
}