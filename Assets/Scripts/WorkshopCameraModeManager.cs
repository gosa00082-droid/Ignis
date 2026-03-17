using UnityEngine;
using Unity.Cinemachine;

public class WorkshopCameraModeManager : MonoBehaviour
{
    [SerializeField] private CinemachineCamera workshopCamera;
    [SerializeField] private MouseLook mouseLook;

    private CinemachineCamera activeCamera;
    private GameObject activeUI;

    private bool objectMode;

    public void EnterObjectMode(CinemachineCamera targetCamera, GameObject targetUI)
    {
        if (objectMode) return;
        if (UIManager.Instance == null) return;

        if (!UIManager.Instance.TryOpenUI(targetUI))
            return;

        objectMode = true;

        activeCamera = targetCamera;
        activeUI = targetUI;

        if (mouseLook != null)
            mouseLook.LockLook();

        workshopCamera.Priority = 10;
        activeCamera.Priority = 20;
    }

    public void ExitObjectMode()
    {
        if (!objectMode) return;

        if (activeCamera != null)
            activeCamera.Priority = 10;

        workshopCamera.Priority = 20;

        if (UIManager.Instance != null)
            UIManager.Instance.CloseCurrent();

        if (mouseLook != null)
            mouseLook.UnlockLook();

        activeCamera = null;
        activeUI = null;
        objectMode = false;
    }

    void Update()
    {
        if (objectMode && Input.GetKeyDown(KeyCode.Escape))
        {
            ExitObjectMode();
        }
    }
}
