using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("Список шагов")]
    public List<TutorialStep> steps = new List<TutorialStep>();

    [Header("UI Ссылки")]
    [SerializeField] private Canvas tutorialCanvas; // Canvas с Sort Order 100
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private GameObject taskPanel; // Панель задач в углу
    [SerializeField] private TextMeshProUGUI taskListText;
    [SerializeField] private Button btnBack;
    [SerializeField] private Button btnUnderstood;
    [SerializeField] private Button panelClickArea;

    [Header("Игрок")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private MouseLook mouseLook;

    private int currentStepIndex = 0;
    private int currentSlideIndex = 0;
    private bool isActive = false; // Активна ли система обучения вообще
    private bool isWaitingForGoals = false; // Ждем ли выполнения целей (панель скрыта)
    private const string SaveKey = "TutorialComplete";

    private void Awake()
    {
        if (Instance == null) Instance = this;

        // Автостарт убрали! Запуск будет из CutsceneManager
        if (PlayerPrefs.GetInt(SaveKey, 0) == 1)
        {
            ForceFinish();
            return;
        }

        // Настройка кнопок
        panelClickArea.onClick.AddListener(OnPanelClicked);
        btnBack.onClick.AddListener(PrevSlide);
        btnUnderstood.onClick.AddListener(OnUnderstoodClicked);

        // Скрываем UI на старте
        if (tutorialCanvas != null) tutorialCanvas.gameObject.SetActive(false);
        if (taskPanel != null) taskPanel.SetActive(false);
    }

    // Метод вызывается из CutsceneManager
    public void StartTutorial()
    {
        if (PlayerPrefs.GetInt(SaveKey, 0) == 1) return;

        foreach (var step in steps)
        {
            foreach (var goal in step.goals)
            {
                goal.isCompleted = false;
            }
        }

        isActive = true;
        currentStepIndex = 0;
        currentSlideIndex = 0;

        ShowCurrentSlide();
    }

    // Клик по панели (Переход вперед по слайдам)
    private void OnPanelClicked()
    {
        // Если ждем выполнения целей или не активны - игнор
        if (!isActive || isWaitingForGoals) return;

        TutorialStep step = steps[currentStepIndex];

        // Если есть еще слайды
        if (currentSlideIndex < step.slides.Count - 1)
        {
            currentSlideIndex++;
            ShowCurrentSlide();
        }
        // Если слайд последний, но целей нет -> сразу следующий шаг
        else if (step.goals.Count == 0)
        {
            GoToNextStep();
        }
        // Если слайд последний и есть цели -> ничего не делаем (ждем кнопку "Я все понял")
    }

    // Кнопка "Я все понял!"
    private void OnUnderstoodClicked()
    {
        TutorialStep step = steps[currentStepIndex];

        // Если это последний шаг обучения И цели выполнены (или их нет)
        if (step.isFinalStep && step.IsGoalsComplete())
        {
            FinishTutorial();
            return;
        }

        // Если цели есть, но еще не выполнены -> Прячем панель, даем играть
        if (step.goals.Count > 0)
        {
            HideDialoguePanel();
        }
        else
        {
            // Если целей нет и это не конец -> просто следующий шаг
            GoToNextStep();
        }
    }

    private void PrevSlide()
    {
        if (!isActive || isWaitingForGoals) return;

        if (currentSlideIndex > 0)
        {
            currentSlideIndex--;
            ShowCurrentSlide();
        }
        else if (currentStepIndex > 0)
        {
            currentStepIndex--;
            TutorialStep prevStep = steps[currentStepIndex];
            currentSlideIndex = prevStep.slides.Count - 1;
            ShowCurrentSlide();
        }
    }

    private void ShowCurrentSlide()
    {
        TutorialStep step = steps[currentStepIndex];

        // Показываем Canvas
        tutorialCanvas.gameObject.SetActive(true);
        isWaitingForGoals = false;

        // БЛОКИРУЕМ ИГРОКА (он не может двигаться, пока читает)
        LockPlayer(true);

        // Обновляем текст
        if (step.slides.Count > 0)
            dialogueText.text = step.slides[currentSlideIndex];

        // Кнопка Назад
        btnBack.gameObject.SetActive(currentSlideIndex > 0 || currentStepIndex > 0);

        // Логика кнопки "Я все понял"
        bool isLastSlide = (currentSlideIndex == step.slides.Count - 1);
        btnUnderstood.gameObject.SetActive(isLastSlide);

        // Текст на кнопке
        TMP_Text btnText = btnUnderstood.GetComponentInChildren<TMP_Text>();
        if (btnText != null)
        {
            if (step.isFinalStep && isLastSlide)
                btnText.text = "Завершить";
            else
                btnText.text = "Приступить";
        }

        UpdateTaskPanel();
    }

    private void HideDialoguePanel()
    {
        tutorialCanvas.gameObject.SetActive(false);
        isWaitingForGoals = true;

        // РАЗБЛОКИРУЕМ ИГРОКА (он может идти выполнять цели)
        LockPlayer(false);

        // Показываем задачи в углу
        if (steps[currentStepIndex].goals.Count > 0)
            taskPanel.SetActive(true);

        Debug.Log("Обучение: Выполните задачи в углу экрана.");
    }

    // Проверка целей (вызывается из Inventory/Anvil/Quest)
    public void CheckGoals(string itemID, int amount, GoalType type)
    {
        if (!isActive) return;

        TutorialStep step = steps[currentStepIndex];
        bool wasComplete = step.IsGoalsComplete();

        // Обновляем статусы целей
        foreach (var goal in step.goals)
        {
            if (goal.isCompleted) continue;
            if (goal.type == type && goal.targetID == itemID)
            {
                if (type == GoalType.CollectItem)
                {
                    if (amount >= goal.requiredAmount) goal.isCompleted = true;
                }
                else { goal.isCompleted = true; }
            }
        }

        Debug.Log($"Проверка цели: тип={type}, itemID={itemID}, текущий шаг={currentStepIndex}");
        foreach (var goal in step.goals)
        {
            Debug.Log($"  Цель: тип={goal.type}, targetID='{goal.targetID}', выполнено={goal.isCompleted}");
        }

        UpdateTaskPanel();

        // Если мы ждали выполнения целей и они выполнены -> Следующий шаг
        if (isWaitingForGoals && step.IsGoalsComplete())
        {
            Debug.Log("Цели выполнены! Переход к следующему шагу.");
            GoToNextStep();
        }
    }

    private void GoToNextStep()
    {
        // Если текущий шаг был последним
        if (steps[currentStepIndex].isFinalStep)
        {
            FinishTutorial();
            return;
        }

        currentStepIndex++;
        currentSlideIndex = 0;

        if (currentStepIndex < steps.Count)
        {
            ShowCurrentSlide();
        }
        else
        {
            FinishTutorial(); // Страховка
        }
    }

    private void UpdateTaskPanel()
    {
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
    }

    private void FinishTutorial()
    {
        PlayerPrefs.SetInt(SaveKey, 1);
        isActive = false;
        isWaitingForGoals = false;

        tutorialCanvas.gameObject.SetActive(false);
        taskPanel.SetActive(false);

        LockPlayer(false); // Разблокируем управление (на всякий случай)
        Debug.Log("Обучение завершено!");
    }

    private void ForceFinish()
    {
        isActive = false;
        tutorialCanvas.gameObject.SetActive(false);
        taskPanel.SetActive(false);
    }

    // Блокировка игрока (опционально, если хочешь блокировать во время чтения)
    private void LockPlayer(bool isLocked)
    {
        if (playerMovement != null) playerMovement.enabled = !isLocked;
        if (mouseLook != null) mouseLook.enabled = !isLocked;
        Cursor.visible = isLocked;
        Cursor.lockState = isLocked ? CursorLockMode.None : CursorLockMode.Locked;
    }
}