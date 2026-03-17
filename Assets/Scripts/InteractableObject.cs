using UnityEngine;
using Unity.Cinemachine;

public class InteractableObject : MonoBehaviour
{
    [Header("Название объекта")]
    [SerializeField] private string displayName = "Объект";

    [Header("Камера")]
    [SerializeField] private CinemachineCamera targetCamera;

    [Header("UI")]
    [SerializeField] private GameObject targetUI;

    [Header("Менеджер камеры")]
    [SerializeField] private WorkshopCameraModeManager cameraModeManager;

    [Header("Обводка")]
    [SerializeField] private SimpleOutline outline;

    public string DisplayName => displayName;

    private void Awake()
    {
        if (outline == null)
            outline = GetComponent<SimpleOutline>();

        if (outline == null)
            outline = GetComponentInChildren<SimpleOutline>(true);

        if (outline != null)
            outline.SetActive(false);
    }

    public void SetHighlight(bool state)
    {
        if (outline != null)
            outline.SetActive(state);
    }

    public void Interact()
    {
        if (cameraModeManager == null)
        {
            Debug.LogWarning($"{name}: не назначен CameraModeManager");
            return;
        }

        if (targetCamera == null)
        {
            Debug.LogWarning($"{name}: не назначена targetCamera");
            return;
        }

        if (targetUI == null)
        {
            Debug.LogWarning($"{name}: не назначен targetUI");
            return;
        }

        cameraModeManager.EnterObjectMode(targetCamera, targetUI);
    }
}
