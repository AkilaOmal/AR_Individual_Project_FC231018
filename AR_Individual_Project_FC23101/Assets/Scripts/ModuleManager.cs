using UnityEngine;

public class ModuleManager : MonoBehaviour
{
    [SerializeField] PlacementController placement;
    [SerializeField] BaseModule[] modulePrefabs; // index 0..4
    BaseModule current;
    int pendingIndex = -1;

    void OnEnable() => placement.OnPlacePoseChosen += HandlePlace;
    void OnDisable() => placement.OnPlacePoseChosen -= HandlePlace;

    public void SelectModule(int index)
    {
        if (current) { current.OnHidden(); Destroy(current.gameObject); }
        pendingIndex = index;
        placement.PlacementEnabled = true;   // wait for user to tap a plane
    }

    void HandlePlace(Pose pose)
    {
        if (pendingIndex < 0) return;
        current = Instantiate(modulePrefabs[pendingIndex], pose.position, pose.rotation);
        current.OnShown();
        placement.PlacementEnabled = false;
        pendingIndex = -1;
    }

    public void ResetCurrent() { if (current) current.ResetModule(); }
    public void BackToMenu() { if (current) { Destroy(current.gameObject); current = null; } }
}