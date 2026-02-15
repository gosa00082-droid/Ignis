// Assets/Scripts/Managers/UIManager.cs
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public GameObject currentUI = null;     // ← временно public для отладки

    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private MouseLook mouseLook;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Попытка открыть конкретный UI (вызывается из контроллеров)
    public bool TryOpenUI(GameObject uiPanel)
    {
        if (currentUI != null && currentUI != uiPanel)
            return false;  // уже открыт другой → отказ

        SetUI(uiPanel);
        return true;
    }

    // Toggle для конкретного UI (Tab, F и т.д.)
    public void ToggleUI(GameObject uiPanel)
    {
        if (currentUI == uiPanel)
            SetUI(null);       // закрываем текущий
        else if (currentUI == null)
            SetUI(uiPanel);    // открываем только если ничего не открыто
        // иначе — ничего не делаем (другой UI открыт)
    }

    // Принудительное закрытие текущего (Esc)
    public void CloseCurrent()
    {
        SetUI(null);
    }

    private void SetUI(GameObject uiPanel)
    {
        if (currentUI != null)
            currentUI.SetActive(false);

        currentUI = uiPanel;

        if (uiPanel != null)
            uiPanel.SetActive(true);

        UpdateControls();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && currentUI != null)
        {
            CloseCurrent();
        }
    }

    private void UpdateControls()
    {
        bool uiOpen = currentUI != null;

        Cursor.visible = uiOpen;
        Cursor.lockState = uiOpen ? CursorLockMode.None : CursorLockMode.Locked;

        if (playerMovement != null) playerMovement.enabled = !uiOpen;
        if (mouseLook != null) mouseLook.enabled = !uiOpen;
    }
}