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

    [Header("Загрузочный экран")]
    [SerializeField] private GameObject loadingRoot;

    [Header("Полоса загрузки")]
    [SerializeField] private Slider progressBar;

    [Header("Крутящаяся картинка")]
    [SerializeField] private RectTransform spinnerImage;
    [SerializeField] private float spinnerRotationSpeed = 180f;

    [Header("Настройки")]
    [SerializeField] private float minimumLoadingTime = 1f;

    private bool isLoading = false;

    private void Start()
    {
        if (menuRoot != null)
            menuRoot.SetActive(true);

        if (loadingRoot != null)
            loadingRoot.SetActive(false);

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

    private void Update()
    {
        if (!isLoading)
            return;

        if (spinnerImage != null)
        {
            spinnerImage.Rotate(0f, 0f, -spinnerRotationSpeed * Time.unscaledDeltaTime);
        }
    }

    public void StartGame()
    {
        if (isLoading)
            return;

        StartCoroutine(LoadGameScene());
    }

    public void OpenSettings()
    {
        Debug.Log("Настройки пока не сделаны.");
    }

    public void ExitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        Debug.Log("ExitGame вызван. В редакторе Unity Application.Quit() не закрывает Play Mode.");
#endif
    }

    private IEnumerator LoadGameScene()
    {
        isLoading = true;

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

            isLoading = false;

            if (menuRoot != null)
                menuRoot.SetActive(true);

            if (loadingRoot != null)
                loadingRoot.SetActive(false);

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
