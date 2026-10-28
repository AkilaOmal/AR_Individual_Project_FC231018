using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [SerializeField] ModuleManager moduleManager;

    [Header("Panels")]
    [SerializeField] GameObject menuPanel;
    [SerializeField] GameObject hudPanel;
    [SerializeField] GameObject infoPanel;
    [SerializeField] GameObject hintText;

    [Header("Info panel")]
    [SerializeField] TMP_Text infoTitle;
    [SerializeField] TMP_Text infoText;

    [Header("Quiz (add later)")]
    [SerializeField] GameObject quizPanel;   // optional for now

    void OnEnable()
    {
        moduleManager.OnSelectionStarted += ShowPlacing;
        moduleManager.OnModulePlaced += ShowActive;
        moduleManager.OnReturnedToMenu += ShowMenu;
    }

    void OnDisable()
    {
        moduleManager.OnSelectionStarted -= ShowPlacing;
        moduleManager.OnModulePlaced -= ShowActive;
        moduleManager.OnReturnedToMenu -= ShowMenu;
    }

    void Start() => ShowMenu();

    // ----- states -----
    void ShowMenu()
    {
        menuPanel.SetActive(true);
        hudPanel.SetActive(false);
        infoPanel.SetActive(false);
        hintText.SetActive(false);
        if (quizPanel) quizPanel.SetActive(false);
    }

    void ShowPlacing()
    {
        menuPanel.SetActive(false);
        hudPanel.SetActive(true);      // lets the user press Back while placing
        infoPanel.SetActive(false);
        hintText.SetActive(true);
    }

    void ShowActive()
    {
        menuPanel.SetActive(false);
        hudPanel.SetActive(true);
        hintText.SetActive(false);
    }

    // ----- buttons -----
    public void OnInfoPressed()
    {
        var m = moduleManager.Current;
        if (m == null) return;              // nothing placed yet
        infoTitle.text = m.moduleName;
        infoText.text = m.explanation;
        infoPanel.SetActive(true);
    }

    public void OnCloseInfo() => infoPanel.SetActive(false);

    public void OnQuizPressed()
    {
        if (quizPanel == null) { Debug.Log("Quiz not built yet."); return; }
        quizPanel.SetActive(true);
    }
}