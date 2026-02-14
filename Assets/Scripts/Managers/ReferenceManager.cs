using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class ReferenceManager : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject referencePanel;           // Вся панель справочника
    public GameObject[] contentPanels;          // Массив из 4 панелей справа: 0-Задания, 1-Материалы, 2-Ковка, 3-Управление
    public Button[] chapterButtons;             // Массив из 4 кнопок слева (в том же порядке)

    [Header("Player Control")]
    public PlayerMovement playerMovement;        // Скрипт движения на Player
    public MouseLook mouseLook;                  // Скрипт обзора на камере

    [Header("Canvas")]
    public GameObject uiCanvas;  // Перетащим сюда весь Canvas

    private bool isOpen = false;

    void Start()
    {
        // Скрываем справочник при старте
        referencePanel.SetActive(false);
    }

    void Update()
    {
        // Открытие/закрытие по F
        if (Input.GetKeyDown(KeyCode.F))
        {
            ToggleReference();
        }
    }

    public void ToggleReference()
    {
        isOpen = !isOpen;
        referencePanel.SetActive(isOpen);
        uiCanvas.SetActive(isOpen);  // Включаем или выключаем весь Canvas

        // Управление курсором и движением
        Cursor.visible = isOpen;
        Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;

        if (playerMovement != null) playerMovement.enabled = !isOpen;
        if (mouseLook != null) mouseLook.enabled = !isOpen;

        // При открытии показываем первую главу
        if (isOpen) ShowChapter(0);
    }

    public void ShowChapter(int index)
    {
        // Выключаем все панели справа
        foreach (GameObject panel in contentPanels)
        {
            panel.SetActive(false);
        }

        // Включаем выбранную
        if (index < contentPanels.Length)
        {
            contentPanels[index].SetActive(true);
        }

        // Подсвечиваем активную кнопку (опционально, можно убрать)
        for (int i = 0; i < chapterButtons.Length; i++)
        {
            ColorBlock colors = chapterButtons[i].colors;
            colors.normalColor = (i == index) ? new Color(0.3f, 0.6f, 1f) : Color.white;
            chapterButtons[i].colors = colors;
        }
    }

    // Вызовется с кнопки "X"
    public void CloseReference()
    {
        if (isOpen) ToggleReference();
    }
}