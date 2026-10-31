//using UnityEngine;

//public class ModuleManager : MonoBehaviour
//{
//    [SerializeField] PlacementController placement;
//    [SerializeField] BaseModule[] modulePrefabs; // index 0..4
//    BaseModule current;
//    int pendingIndex = -1;

//    void OnEnable() => placement.OnPlacePoseChosen += HandlePlace;
//    void OnDisable() => placement.OnPlacePoseChosen -= HandlePlace;

//    public void SelectModule(int index)
//    {
//        if (current) { current.OnHidden(); Destroy(current.gameObject); }
//        pendingIndex = index;
//        placement.PlacementEnabled = true;   // wait for user to tap a plane
//    }

//    void HandlePlace(Pose pose)
//    {
//        if (pendingIndex < 0) return;
//        current = Instantiate(modulePrefabs[pendingIndex], pose.position, pose.rotation);
//        current.OnShown();
//        placement.PlacementEnabled = false;
//        pendingIndex = -1;
//    }

//    public void ResetCurrent() { if (current) current.ResetModule(); }
//    public void BackToMenu() { if (current) { Destroy(current.gameObject); current = null; } }
//}using System;
using UnityEngine;
using System;
using UnityEngine;

public class ModuleManager : MonoBehaviour
{
    [SerializeField] PlacementController placement;
    [SerializeField] BaseModule[] modulePrefabs;   // index 0..4

    public BaseModule Current { get; private set; } = -1;
    public event Action OnSelectionStarted;   // user picked a module, waiting for placement
    public event Action OnModulePlaced;       // module appeared in the world
    public event Action OnReturnedToMenu;

    int pendingIndex = -1;

    void Awake()
    {
        if (placement == null) placement = FindFirstObjectByType<PlacementController>();
        if (placement == null) Debug.LogError("ModuleManager: no PlacementController found.", this);
    }

    void OnEnable() { if (placement != null) placement.OnPlacePoseChosen += HandlePlace; }
    void OnDisable() { if (placement != null) placement.OnPlacePoseChosen -= HandlePlace; }

    public void SelectModule(int index)
    {
        if (index < 0 || index >= modulePrefabs.Length || modulePrefabs[index] == null)
        {
            Debug.LogWarning($"Module {index + 1} is not assigned yet.");
            return;
        }
        DestroyCurrent();
        pendingIndex = index;
        placement.PlacementEnabled = true;
        OnSelectionStarted?.Invoke();
    }

    void HandlePlace(Pose pose)
    {
        if (pendingIndex < 0) return;
        Current = Instantiate(modulePrefabs[pendingIndex], pose.position, pose.rotation);
        CurrentIndex = pendingIndex;
        Current.OnShown();
        placement.PlacementEnabled = false;
        pendingIndex = -1;
        OnModulePlaced?.Invoke();
    }

    public void ResetCurrent() { if (Current) Current.ResetModule(); }

    public void BackToMenu()
    {
        DestroyCurrent();
        pendingIndex = -1;
        placement.PlacementEnabled = false;
        OnReturnedToMenu?.Invoke();
    }

    void DestroyCurrent()
    {
        if (Current) { Current.OnHidden(); Destroy(Current.gameObject); Current = null; }
        CurrentIndex = -1;
    }
}