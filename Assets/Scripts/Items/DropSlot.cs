using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DropSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Color highlightColor = new Color(0.8f, 0.9f, 1f);

    private Image backgroundImage;

    private void Awake()
    {
        backgroundImage = GetComponent<Image>();

        // Защита на случай, если Image на дочернем объекте (часто бывает)
        if (backgroundImage == null)
        {
            backgroundImage = GetComponentInChildren<Image>();
        }

        if (backgroundImage == null)
        {
            Debug.LogWarning("DropSlot: Image-компонент не найден на слоте " + name);
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null)
        {
            eventData.pointerDrag.transform.SetParent(transform);
            eventData.pointerDrag.transform.localPosition = Vector3.zero;
            FindObjectOfType<FurnaceUI>().UpdatePreview();  // Вызов preview
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (eventData.pointerDrag != null && backgroundImage != null)
        {
            backgroundImage.color = highlightColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = Color.white;
        }
    }
}