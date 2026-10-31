using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuizPanel : MonoBehaviour
{
    [Header("Texts (UI TextMeshPro)")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private TMP_Text feedbackText;

    [Header("Answer buttons")]
    [SerializeField] private Button optionA;
    [SerializeField] private Button optionB;
    [SerializeField] private TMP_Text optionAText;
    [SerializeField] private TMP_Text optionBText;

    [Header("Navigation")]
    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_Text nextButtonText;
    [SerializeField] private Button closeButton;

    [Header("Colours")]
    [SerializeField] private Color normalColor = new Color(0.20f, 0.30f, 0.50f);
    [SerializeField] private Color correctColor = new Color(0.15f, 0.65f, 0.25f);
    [SerializeField] private Color wrongColor = new Color(0.80f, 0.20f, 0.20f);
    [SerializeField] private Color correctTextColor = new Color(0.55f, 1.00f, 0.60f);
    [SerializeField] private Color wrongTextColor = new Color(1.00f, 0.55f, 0.55f);

    private QuizQuestion[] questions;
    private int moduleIndex;
    private int current;
    private int score;
    private bool answered;
    private bool finished;
    private bool correctOnLeft;
    private bool listenersAdded;

    private void Awake()
    {
        AddListeners();
    }

    private bool Validate()
    {
        if (titleText == null || progressText == null || questionText == null || feedbackText == null ||
            optionA == null || optionB == null || optionAText == null || optionBText == null ||
            nextButton == null || nextButtonText == null || closeButton == null)
        {
            Debug.LogError("QuizPanel: one or more Inspector slots are empty.", this);
            return false;
        }
        return true;
    }

    private void AddListeners()
    {
        if (listenersAdded || !Validate()) return;
        listenersAdded = true;

        optionA.onClick.AddListener(OnOptionA);
        optionB.onClick.AddListener(OnOptionB);
        nextButton.onClick.AddListener(OnNext);
        closeButton.onClick.AddListener(OnClose);
    }

    // ---------- Called by AppUIManager ----------
    public void Open(int index)
    {
        gameObject.SetActive(true);
        AddListeners();
        if (!listenersAdded) return;

        moduleIndex = index;
        questions = QuizData.ForModule(index);

        if (questions == null || questions.Length == 0)
        {
            titleText.text = "Quiz";
            progressText.text = "";
            questionText.text = "No quiz is available for this module yet.";
            feedbackText.text = "";
            optionA.gameObject.SetActive(false);
            optionB.gameObject.SetActive(false);
            nextButton.gameObject.SetActive(false);
            return;
        }

        StartQuiz();
    }

    private void StartQuiz()
    {
        current = 0;
        score = 0;
        finished = false;
        titleText.text = QuizData.TitleFor(moduleIndex);
        ShowQuestion();
    }

    private void ShowQuestion()
    {
        answered = false;
        QuizQuestion q = questions[current];

        correctOnLeft = Random.value > 0.5f;
        optionAText.text = correctOnLeft ? q.correct : q.wrong;
        optionBText.text = correctOnLeft ? q.wrong : q.correct;

        SetButtonColor(optionA, normalColor);
        SetButtonColor(optionB, normalColor);
        optionA.gameObject.SetActive(true);
        optionB.gameObject.SetActive(true);

        progressText.text = "Question " + (current + 1) + " of " + questions.Length;
        questionText.text = q.question;
        feedbackText.text = "";
        nextButton.gameObject.SetActive(false);
    }

    // ---------- Answers ----------
    private void OnOptionA() { Answer(true); }
    private void OnOptionB() { Answer(false); }

    private void Answer(bool pickedLeft)
    {
        if (answered || finished || questions == null) return;
        answered = true;

        bool isCorrect = (pickedLeft == correctOnLeft);
        QuizQuestion q = questions[current];

        Button picked = pickedLeft ? optionA : optionB;
        Button other = pickedLeft ? optionB : optionA;

        if (isCorrect)
        {
            score++;
            SetButtonColor(picked, correctColor);
            feedbackText.color = correctTextColor;
            feedbackText.text = "Correct!\n" + q.explanation;
        }
        else
        {
            SetButtonColor(picked, wrongColor);
            SetButtonColor(other, correctColor);     // show the right answer
            feedbackText.color = wrongTextColor;
            feedbackText.text = "Not correct.\n" + q.explanation;
        }

        nextButtonText.text = (current >= questions.Length - 1) ? "See score" : "Next question";
        nextButton.gameObject.SetActive(true);
    }

    private void OnNext()
    {
        if (finished)
        {
            StartQuiz();         // "Try again"
            return;
        }

        current++;
        if (current >= questions.Length) ShowResult();
        else ShowQuestion();
    }

    private void ShowResult()
    {
        finished = true;

        optionA.gameObject.SetActive(false);
        optionB.gameObject.SetActive(false);

        progressText.text = "Finished";
        questionText.text = "Quiz complete!";
        feedbackText.color = Color.white;

        string message;
        if (score == questions.Length) message = "Perfect score!";
        else if (score >= 3) message = "Good work. Try again for a perfect score.";
        else message = "Review the module and try again.";

        feedbackText.text = "You scored " + score + " out of " + questions.Length + ".\n" + message;
        nextButtonText.text = "Try again";
        nextButton.gameObject.SetActive(true);
    }

    private void OnClose()
    {
        gameObject.SetActive(false);
    }

    private void SetButtonColor(Button b, Color c)
    {
        Image img = b.GetComponent<Image>();
        if (img != null) img.color = c;
    }
}