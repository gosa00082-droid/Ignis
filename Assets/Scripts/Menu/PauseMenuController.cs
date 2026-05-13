using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    [Header("Сцена главного меню")]
    [SerializeField] private string mainMenuSceneName = "Menu";

    [Header("Корень меню паузы")]
    [SerializeField] private GameObject pauseMenuRoot;

    [Header("Кнопки, которые скрываются при сообщении настроек")]
    [SerializeField] private GameObject[] menuButtons;

    [Header("Надпись настроек")]
    [SerializeField] private GameObject settingsWarning;

    [Header("Загрузочный экран")]
    [SerializeField] private GameObject loadingRoot;
    [SerializeField] private Slider progressBar;
    [SerializeField] private RectTransform spinnerImage;
    [SerializeField] private float spinnerRotationSpeed = 180f;
    [SerializeField] private float minimumLoadingTime = 1f;

    [Header("Менеджер объектного режима")]
    [SerializeField] private WorkshopCameraModeManager cameraModeManager;

    [Header("Скрипты игрока / взаимодействия")]
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private MouseLook mouseLook;

    [Header("Курсор")]
    [SerializeField] private bool showCursorOnPause = true;

    private bool isPaused = false;
    private bool isSettingsWarningOpen = false;
    private bool isLoading = false;

    private void Start()
    {
        ForceResumeState();

        if (loadingRoot != null)
            loadingRoot.SetActive(false);

        if (progressBar != null)
        {
            progressBar.minValue = 0f;
            progressBar.maxValue = 1f;
            progressBar.value = 0f;
            progressBar.interactable = false;
        }
    }

    private void Update()
    {
        if (isLoading)
        {
            if (spinnerImage != null)
                spinnerImage.Rotate(0f, 0f, -spinnerRotationSpeed * Time.unscaledDeltaTime);

            return;
        }

        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        if (cameraModeManager != null && cameraModeManager.IsInObjectMode)
            return;

        if (isSettingsWarningOpen)
        {
            CloseSettingsWarning();
            return;
        }

        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void PauseGame()
    {
        if (cameraModeManager != null && cameraModeManager.IsInObjectMode)
            return;

        isPaused = true;
        isSettingsWarningOpen = false;

        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(true);

        if (loadingRoot != null)
            loadingRoot.SetActive(false);

        SetMenuButtonsActive(true);

        if (settingsWarning != null)
            settingsWarning.SetActive(false);

        Time.timeScale = 0f;
        AudioListener.pause = true;

        if (playerInteraction != null)
        {
            playerInteraction.ForceClearHover();
            playerInteraction.enabled = false;
        }

        if (mouseLook != null)
        {
            mouseLook.ForceResetState();
            mouseLook.enabled = false;
        }

        if (showCursorOnPause)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    public void ResumeGame()
    {
        isPaused = false;
        isSettingsWarningOpen = false;

        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(false);

        if (loadingRoot != null)
            loadingRoot.SetActive(false);

        if (settingsWarning != null)
            settingsWarning.SetActive(false);

        SetMenuButtonsActive(true);

        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (playerInteraction != null)
        {
            playerInteraction.enabled = true;
            playerInteraction.ForceClearHover();
        }

        if (mouseLook != null)
        {
            mouseLook.enabled = true;
            mouseLook.ForceResetState();
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void OpenSettings()
    {
        if (!isPaused || isLoading)
            return;

        isSettingsWarningOpen = true;

        SetMenuButtonsActive(false);

        if (settingsWarning != null)
            settingsWarning.SetActive(true);
    }

    public void CloseSettingsWarning()
    {
        isSettingsWarningOpen = false;

        if (settingsWarning != null)
            settingsWarning.SetActive(false);

        SetMenuButtonsActive(true);
    }

    public void ExitGame()
    {
        if (isLoading)
            return;

        StartCoroutine(ReturnToMainMenuRoutine());
    }

    private IEnumerator ReturnToMainMenuRoutine()
    {
        isLoading = true;
        isPaused = false;
        isSettingsWarningOpen = false;

        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (playerInteraction != null)
        {
            playerInteraction.enabled = false;
            playerInteraction.ForceClearHover();
        }

        if (mouseLook != null)
        {
            mouseLook.ForceResetState();
            mouseLook.enabled = false;
        }

        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(false);

        if (settingsWarning != null)
            settingsWarning.SetActive(false);

        if (loadingRoot != null)
            loadingRoot.SetActive(true);

        if (progressBar != null)
            progressBar.value = 0f;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        float timer = 0f;

        AsyncOperation operation = SceneManager.LoadSceneAsync(mainMenuSceneName);

        if (operation == null)
        {
            Debug.LogError("Не удалось загрузить главное меню: " + mainMenuSceneName);
            yield break;
        }

        operation.allowSceneActivation = false;

        while (!operation.isDone)
        {
            timer += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(operation.progress / 0.9f);

            if (progressBar != null)
                progressBar.value = progress;

            if (operation.progress >= 0.9f && timer >= minimumLoadingTime)
            {
                if (progressBar != null)
                    progressBar.value = 1f;

                yield return new WaitForSecondsRealtime(0.2f);

                operation.allowSceneActivation = true;
            }

            yield return null;
        }
    }

    private void SetMenuButtonsActive(bool state)
    {
        if (menuButtons == null)
            return;

        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i] != null)
                menuButtons[i].SetActive(state);
        }
    }

    private void ForceResumeState()
    {
        isPaused = false;
        isSettingsWarningOpen = false;
        isLoading = false;

        if (pauseMenuRoot != null)
            pauseMenuRoot.SetActive(false);

        if (settingsWarning != null)
            settingsWarning.SetActive(false);

        SetMenuButtonsActive(true);

        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (playerInteraction != null)
        {
            playerInteraction.enabled = true;
            playerInteraction.ForceClearHover();
        }

        if (mouseLook != null)
        {
            mouseLook.enabled = true;
            mouseLook.ForceResetState();
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void OnDisable()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }
}
