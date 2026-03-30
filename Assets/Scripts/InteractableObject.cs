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


    public CinemachineCamera TargetCamera => targetCamera;
    public GameObject TargetUI => targetUI;
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
        Debug.Log($"[InteractableObject] Клик по объекту: {name}", this);

        if (cameraModeManager == null)
        {
            Debug.LogWarning($"[InteractableObject] {name}: не назначен CameraModeManager", this);
            return;
        }

        if (targetCamera == null)
        {
            Debug.LogWarning($"[InteractableObject] {name}: не назначена targetCamera", this);
            return;
        }

        Debug.Log($"[InteractableObject] {name}: вызываю EnterObjectMode. targetUI={(targetUI != null ? targetUI.name : "NULL")}", this);
        cameraModeManager.EnterObjectMode(targetCamera, targetUI);
    }
}
