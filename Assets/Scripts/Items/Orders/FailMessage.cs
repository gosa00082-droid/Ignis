using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class FailMessage : MonoBehaviour
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private CanvasGroup canvasGroup;

    private void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        gameObject.SetActive(false);
    }

    public void Show(string message)
    {
        messageText.text = message;
        StartCoroutine(ShowCoroutine());
    }

    private IEnumerator ShowCoroutine()
    {
        gameObject.SetActive(true);
        canvasGroup.alpha = 0f;
        yield return StartCoroutine(Fade(1f, 0.5f));  // fade in

        yield return new WaitForSeconds(3f);

        yield return StartCoroutine(Fade(0f, 0.5f));  // fade out
        gameObject.SetActive(false);
    }

    private IEnumerator Fade(float targetAlpha, float duration)
    {
        float startAlpha = canvasGroup.alpha;
        float time = 0;
        while (time < duration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            yield return null;
        }
        canvasGroup.alpha = targetAlpha;
    }
}