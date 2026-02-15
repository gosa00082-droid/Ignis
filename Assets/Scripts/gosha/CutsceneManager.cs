using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.Playables;

public class CutsceneManager : MonoBehaviour
{
    [SerializeField] private GameObject player; // Ссылка на объект игрока (капсула)
    [SerializeField] private GameObject playerCameraObject; // GameObject с обычной камерой игрока (без Cinemachine)
    [SerializeField] private Camera cutsceneCamera; // Моя main camera с CinemachineBrain
    [SerializeField] private PlayableDirector cutsceneDirector; // Playable Director с Timeline
    [SerializeField] private CinemachineCamera[] cutsceneVCams; // Массив всех vcam для катсцены

    void Start()
    {
        StartGameWithCutscene(); // Автоматически запускает катсцену при загрузке сцены
    }

    public void StartGameWithCutscene()
    {
        Debug.Log("Катсцена запущена!");

        // Скрыть игрока и отключить его контроллер
        if (player != null)
        {
            Debug.Log("Скрываем капсулу игрока");
            player.SetActive(false);
            // Если у игрока есть скрипт контроллера: player.GetComponent<PlayerController>().enabled = false;
        }

        // Отключить камеру игрока
        if (playerCameraObject != null)
        {
            Debug.Log("Отключаем камеру игрока");
            playerCameraObject.SetActive(false);
        }

        // Включить камеру катсцены
        if (cutsceneCamera != null)
        {
            Debug.Log("Включаем камеру катсцены");
            cutsceneCamera.gameObject.SetActive(true);
        }

        // Запустить Timeline (он активирует vcam)
        if (cutsceneDirector != null)
        {
            cutsceneDirector.Play();
        }

        // Подписаться на окончание
        cutsceneDirector.stopped += OnCutsceneFinished;
    }

    private void OnCutsceneFinished(PlayableDirector director)
    {
        Debug.Log("Катсцена завершена, возвращаем всё");

        // ЗАПУСК ОБУЧЕНИЯ
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.StartTutorial();
        }

        // Отключить все катсценные vcam
        foreach (var vcam in cutsceneVCams)
        {
            if (vcam != null)
            {
                Debug.Log("Отключаем vcam: " + vcam.name);
                vcam.Priority = 0; // Или vcam.gameObject.SetActive(false);
            }
        }

        // Отключить камеру катсцены
        if (cutsceneCamera != null)
        {
            Debug.Log("Выключаем камеру после катсцены");
            cutsceneCamera.gameObject.SetActive(false);
        }

        // Включить камеру игрока
        if (playerCameraObject != null)
        {
            Debug.Log("Включаем камеру игрока");
            playerCameraObject.SetActive(true);
        }

        // Показать игрока и включить контроллер
        if (player != null)
        {
            Debug.Log("Показываем капсулу игрока");
            player.SetActive(true);
            // Если у игрока есть скрипт контроллера: player.GetComponent<PlayerController>().enabled = true;
        }

        // Отписаться
        cutsceneDirector.stopped -= OnCutsceneFinished;
    }
}