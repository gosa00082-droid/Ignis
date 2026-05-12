using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingMenuController : MonoBehaviour
{
    [Header("Название сцены, которая запускается")]
    [SerializeField] private string gameSceneName = "готово для миреа (без интерфейса)";

    [Header("Главное меню")]
    [SerializeField] private GameObject menuRoot;

    [Header("Кнопки, которые надо скрывать при открытии настроек")]
    [SerializeField] private GameObject[] menuButtons;

    [Header("Надпись настроек")]
    [SerializeField] private GameObject settingsMessage;

    [Header("Загрузочный экран")]
    [SerializeField] private GameObject loadingRoot;

    [Header("Полоса загрузки")]
    [SerializeField] private Slider progressBar;

    [Header("Крутящаяся картинка")]
    [SerializeField] private RectTransform spinnerImage;
    [SerializeField] private float spinnerRotationSpeed = 180f;

    [Header("Настройки загрузки")]
    [SerializeField] private float minimumLoadingTime = 1f;

    private bool isLoading = false;
    private bool isSettingsMessageOpen = false;

    private void Start()
    {
        ShowMainMenu();
    }

    private void Update()
    {
        if (isLoading)
        {
            if (spinnerImage != null)
                spinnerImage.Rotate(0f, 0f, -spinnerRotationSpeed * Time.unscaledDeltaTime);

            return;
        }

        if (isSettingsMessageOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseSettingsMessage();
        }
    }

    public void StartGame()
    {
        if (isLoading)
            return;

        if (isSettingsMessageOpen)
            return;

        StartCoroutine(LoadGameScene());
    }

    public void OpenSettings()
    {
        if (isLoading)
            return;

        isSettingsMessageOpen = true;

        SetMenuButtonsActive(false);

        if (settingsMessage != null)
            settingsMessage.SetActive(true);
    }

    public void CloseSettingsMessage()
    {
        isSettingsMessageOpen = false;

        if (settingsMessage != null)
            settingsMessage.SetActive(false);

        SetMenuButtonsActive(true);
    }

    public void ExitGame()
    {
        if (isLoading)
            return;

        if (isSettingsMessageOpen)
            return;

        Application.Quit();

#if UNITY_EDITOR
        Debug.Log("ExitGame вызван. В редакторе Unity Application.Quit() не закрывает Play Mode.");
#endif
    }

    private void ShowMainMenu()
    {
        isLoading = false;
        isSettingsMessageOpen = false;

        if (menuRoot != null)
            menuRoot.SetActive(true);

        if (loadingRoot != null)
            loadingRoot.SetActive(false);

        SetMenuButtonsActive(true);

        if (settingsMessage != null)
            settingsMessage.SetActive(false);

        if (progressBar != null)
        {
            progressBar.minValue = 0f;
            progressBar.maxValue = 1f;
            progressBar.value = 0f;
            progressBar.interactable = false;
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
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

    private IEnumerator LoadGameScene()
    {
        isLoading = true;
        isSettingsMessageOpen = false;

        if (settingsMessage != null)
            settingsMessage.SetActive(false);

        SetMenuButtonsActive(false);

        if (menuRoot != null)
            menuRoot.SetActive(false);

        if (loadingRoot != null)
            loadingRoot.SetActive(true);

        if (progressBar != null)
            progressBar.value = 0f;

        float timer = 0f;

        AsyncOperation operation = SceneManager.LoadSceneAsync(gameSceneName);

        if (operation == null)
        {
            Debug.LogError("Не удалось загрузить сцену: " + gameSceneName);
            ShowMainMenu();
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
}
