using UnityEngine;
using TMPro;

public class ServerNode : MonoBehaviour
{
    public int capacity = 4;
    public Renderer body;
    public GameObject warningIcon;        // small red "!" object, child of the server
    public TextMeshPro loadLabel;         // "0/4"
    public Color idleColor = new Color(0.2f, 0.8f, 0.3f);
    public Color busyColor = new Color(0.9f, 0.15f, 0.15f);

    public int Active { get; private set; }
    public bool IsFull => Active >= capacity;

    public bool TryAccept()
    {
        if (IsFull) return false;
        Active++; Refresh(); return true;
    }

    public void Release()
    {
        Active = Mathf.Max(0, Active - 1); Refresh();
    }

    public void ResetNode() { Active = 0; Refresh(); }

    void Refresh()
    {
        float load = (float)Active / capacity;
        body.material.color = Color.Lerp(idleColor, busyColor, load);
        if (warningIcon) warningIcon.SetActive(IsFull);
        if (loadLabel) loadLabel.text = $"{Active}/{capacity}";
    }

    void OnEnable() => Refresh();
}