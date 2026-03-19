using UnityEngine;
using System;

public class ChoiceUI : MonoBehaviour
{
    public GameObject panel;

    public Action OnNowAction;
    public Action OnLaterAction;

    public void Show()
    {
        panel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void HideOnly()
    {
        panel.SetActive(false);
    }

    public void OnNow()
    {
        HideOnly();
        OnNowAction?.Invoke();
    }

    public void OnLater()
    {
        HideOnly();
        OnLaterAction?.Invoke();
    }
}