using UnityEngine;
using Unity.Cinemachine;

public class InteractableObject : MonoBehaviour
{
    [Header("Камера")]
    [SerializeField] private CinemachineCamera targetCamera;

    [Header("UI")]
    [SerializeField] private GameObject targetUI;

    [Header("Менеджер камеры")]
    [SerializeField] private WorkshopCameraModeManager cameraModeManager;

    public void Interact()
    {
        if (cameraModeManager == null) return;
        if (targetCamera == null) return;
        if (targetUI == null) return;

        cameraModeManager.EnterObjectMode(targetCamera, targetUI);
    }
}
