using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class WorkshopCameraModeManager : MonoBehaviour
{
    [Header("Камеры")]
    [SerializeField] private CinemachineCamera workshopCamera;
    [SerializeField] private MouseLook mouseLook;
    [SerializeField] private PlayerInteraction playerInteraction;

    [Header("Длительность блокировки на возврате")]
    [SerializeField] private float returnBlendLockTime = 1.0f;

    private CinemachineCamera activeCamera;
    private GameObject activeUI;

    private bool objectMode;
    private bool isReturning;

    public bool IsInObjectMode => objectMode || isReturning;

    public void EnterObjectMode(CinemachineCamera targetCamera, GameObject targetUI)
    {
        Debug.Log($"[CameraMode] EnterObjectMode вызван. camera={(targetCamera != null ? targetCamera.name : "NULL")}, ui={(targetUI != null ? targetUI.name : "NULL")}");

        if (objectMode || isReturning)
        {
            Debug.Log("[CameraMode] Уже в object mode или идет возврат");
            return;
        }

        if (targetCamera == null)
        {
            Debug.LogWarning("[CameraMode] targetCamera == null");
            return;
        }

        if (targetUI != null)
        {
            if (UIManager.Instance == null)
            {
                Debug.LogWarning("[CameraMode] UIManager.Instance == null, UI открыть нельзя");
                return;
            }

            if (!UIManager.Instance.TryOpenUI(targetUI))
            {
                Debug.LogWarning($"[CameraMode] Не удалось открыть UI {targetUI.name}");
                return;
            }

            Debug.Log($"[CameraMode] UI {targetUI.name} открыт");
        }
        else
        {
            Debug.Log("[CameraMode] Работаем без UI, это нормально");
        }

        objectMode = true;
        activeCamera = targetCamera;
        activeUI = targetUI;

        if (playerInteraction != null)
            playerInteraction.ForceClearHover();

        if (mouseLook != null)
            mouseLook.LockLook();

        if (workshopCamera != null)
            workshopCamera.Priority = 10;

        activeCamera.Priority = 20;

        Debug.Log($"[CameraMode] Object mode включен. Активная камера: {activeCamera.name}");
    }

    public void ExitObjectMode()
    {
        if (!objectMode || isReturning) return;

        StartCoroutine(ReturnToWorkshopRoutine());
    }

    private IEnumerator ReturnToWorkshopRoutine()
    {
        isReturning = true;

        if (activeCamera != null)
            activeCamera.Priority = 10;

        if (workshopCamera != null)
            workshopCamera.Priority = 20;

        if (activeUI != null && UIManager.Instance != null)
        {
            Debug.Log($"[CameraMode] Закрываю UI {activeUI.name}");
            UIManager.Instance.CloseCurrent();
        }
        else
        {
            Debug.Log("[CameraMode] UI закрывать не нужно");
        }

        if (playerInteraction != null)
            playerInteraction.ForceClearHover();

        if (mouseLook != null)
            mouseLook.LockLook();

        yield return new WaitForSeconds(returnBlendLockTime);

        activeCamera = null;
        activeUI = null;
        objectMode = false;
        isReturning = false;

        if (mouseLook != null)
        {
            mouseLook.UnlockLook();
            mouseLook.ForceResetState();
        }

        if (playerInteraction != null)
            playerInteraction.ForceClearHover();
    }

    public void SwitchObjectMode(CinemachineCamera targetCamera, GameObject targetUI)
    {
        if (UIManager.Instance == null) return;
        if (targetCamera == null || targetUI == null) return;

        if (playerInteraction != null)
            playerInteraction.ForceClearHover();

        if (mouseLook != null)
            mouseLook.LockLook();

        if (activeCamera != null)
            activeCamera.Priority = 10;

        if (activeUI != null && UIManager.Instance.currentUI == activeUI)
            UIManager.Instance.CloseCurrent();

        if (!UIManager.Instance.TryOpenUI(targetUI))
            return;

        objectMode = true;
        isReturning = false;
        activeCamera = targetCamera;
        activeUI = targetUI;

        if (workshopCamera != null)
            workshopCamera.Priority = 10;

        activeCamera.Priority = 20;
    }

    private void Update()
    {
        if (objectMode && !isReturning && Input.GetKeyDown(KeyCode.Escape))
        {
            ExitObjectMode();
        }
    }
}
