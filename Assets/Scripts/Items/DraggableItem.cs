using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [HideInInspector] public Transform homeParent;   // мини-инвентарь Content — устанавливаем при создании слота
    [HideInInspector] public string itemId;  // ← добавь это поле

    private Canvas canvas;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvas = FindObjectOfType<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;

        transform.SetParent(canvas.transform, true);  // поверх всего
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // Raycast под курсором — ищем DropSlot
        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = eventData.position
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        bool droppedOnSlot = false;
        foreach (var result in results)
        {
            if (result.gameObject.GetComponent<DropSlot>() != null)
            {
                droppedOnSlot = true;
                break;
            }
        }

        if (!droppedOnSlot && homeParent != null)
        {
            // Возврат в мини-инвентарь (в конец списка)
            transform.SetParent(homeParent);
            transform.localPosition = Vector3.zero;

            // ← добавь это
            FurnaceUI furnaceUI = FindObjectOfType<FurnaceUI>();
            if (furnaceUI != null) furnaceUI.UpdatePreview();
        }
        // Если попали в слот печи — DropSlot уже сделал SetParent + центрирование
    }
}