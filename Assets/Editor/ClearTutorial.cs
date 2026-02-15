#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class ClearTutorial
{
    [MenuItem("Tools/Ignis/Сбросить обучение %&t")] // Ctrl+Shift+T
    static void ResetTutorial()
    {
        PlayerPrefs.DeleteKey("TutorialComplete");
        PlayerPrefs.Save();
        Debug.Log("<color=green>✅ Обучение сброшено! Перезапустите сцену.</color>");
    }
}
#endif