using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("Данные")]
    public List<TutorialStep> steps = new List<TutorialStep>();

    [Header("UI Ссылки")]
    [SerializeField] private GameObject tutorialCanvas;      // <--- ТЕПЕРЬ СЮДА КИДАЕМ CANVAS
    [SerializeField] private TextMeshProUGUI dialogueText;   // Текст внутри панели
    [SerializeField] private GameObject taskPanel;           // Панель задач (Должна быть ВНЕ tutorialCanvas или в другом Canvas!)
    [SerializeField] private TextMeshProUGUI taskListText;   // Текст задач
    [SerializeField] private Button btnBack;
    [SerializeField] private Button btnUnderstood;
    [SerializeField] private Button panelClickArea;          // Кнопка на панели

    [Header("Ссылки на игрока")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private MouseLook mouseLook;

    private int currentStepIndex = 0;
    private bool isActive = false;
    private const string SaveKey = "TutorialComplete";

    private void Awake()
    {
        if (Instance == null) Instance = this;

        if (PlayerPrefs.GetInt(SaveKey, 0) == 1)
        {
            FinishTutorial(false);
            return;
        }

        panelClickArea.onClick.AddListener(OnPanelClicked);
        btnBack.onClick.AddListener(PrevSlide);
        btnUnderstood.onClick.AddListener(OnUnderstoodClicked);

        StartTutorial();
    }

    private void StartTutorial()
    {
        isActive = true;
        currentStepIndex = 0;
        LockPlayer(true);

        // Включаем Canvas
        if (tutorialCanvas != null) tutorialCanvas.SetActive(true);

        ShowStep(currentStepIndex);
    }

    private void OnPanelClicked()
    {
        if (currentStepIndex == steps.Count - 1) return;

        TutorialStep step = steps[currentStepIndex];

        if (step.goals.Count > 0)
        {
            if (!step.IsComplete())
            {
                Debug.Log("Сначала выполни задачи!");
                return;
            }
        }

        NextSlide();
    }

    private void NextSlide()
    {
        if (currentStepIndex < steps.Count - 1)
        {
            currentStepIndex++;
            ShowStep(currentStepIndex);
        }
    }

    private void PrevSlide()
    {
        if (currentStepIndex > 0)
        {
            currentStepIndex--;
            ShowStep(currentStepIndex);
        }
    }

    private void OnUnderstoodClicked()
    {
        PlayerPrefs.SetInt(SaveKey, 1);
        FinishTutorial(true);
    }

    private void ShowStep(int index)
    {
        TutorialStep step = steps[index];
        dialogueText.text = step.dialogueText;

        btnBack.gameObject.SetActive(index > 0);

        bool isLastStep = (index == steps.Count - 1);
        btnUnderstood.gameObject.SetActive(isLastStep && step.IsComplete());

        UpdateTaskPanel();
    }

    public void CheckGoals(string itemID, int amount, GoalType type)
    {
        if (!isActive) return;
        TutorialStep step = steps[currentStepIndex];

        foreach (var goal in step.goals)
        {
            if (goal.isCompleted) continue;

            if (goal.type == type && goal.targetID == itemID)
            {
                if (type == GoalType.CollectItem)
                {
                    if (amount >= goal.requiredAmount) goal.isCompleted = true;
                }
                else
                {
                    goal.isCompleted = true;
                }
            }
        }
        UpdateTaskPanel();
    }

    private void UpdateTaskPanel()
    {
        if (!isActive) return;
        TutorialStep step = steps[currentStepIndex];

        if (step.goals.Count == 0)
        {
            taskPanel.SetActive(false);
            return;
        }

        taskPanel.SetActive(true);
        string tasks = "";
        foreach (var goal in step.goals)
        {
            string status = goal.isCompleted ? "<color=green>✔</color>" : "○";
            tasks += $"{status} {goal.description}\n";
        }
        taskListText.text = tasks;

        if (step.IsComplete())
        {
            if (currentStepIndex == steps.Count - 1)
            {
                btnUnderstood.gameObject.SetActive(true);
            }
        }
    }

    private void FinishTutorial(bool playAnim)
    {
        isActive = false;

        // Выключаем Canvas обучения
        if (tutorialCanvas != null) tutorialCanvas.SetActive(false);

        // Панель задач оставляем включенной!
        // Но только если она нужна для следующих целей (логику задач нужно доработать отдельно)
        // taskPanel.SetActive(false); <-- НЕ ВЫКЛЮЧАЕМ

        LockPlayer(false);
    }

    private void LockPlayer(bool isLocked)
    {
        if (playerMovement != null) playerMovement.enabled = !isLocked;
        if (mouseLook != null) mouseLook.enabled = !isLocked;
        Cursor.visible = isLocked;
        Cursor.lockState = isLocked ? CursorLockMode.None : CursorLockMode.Locked;
    }
}