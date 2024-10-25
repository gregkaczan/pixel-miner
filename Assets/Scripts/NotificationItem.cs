using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class NotificationItem : MonoBehaviour
{
    public TextMeshProUGUI messageText; // Assign in Prefab
    private string resourceName;
    private int totalAmount;
    private float displayDuration;
    private NotificationManager manager;

    private Coroutine fadeCoroutine;

    private Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void Initialize(string resourceName, int amount, float duration, NotificationManager manager)
    {
        this.resourceName = resourceName;
        this.totalAmount = amount;
        this.displayDuration = duration;
        this.manager = manager;

        UpdateMessage();
        StartFadeCoroutine();
    }

    public void UpdateAmount(int amount)
    {
        totalAmount += amount;
        UpdateMessage();
        RestartFadeCoroutine();
    }

    private void UpdateMessage()
    {
        animator.Play("NotificationPopUp", -1, 0f);
        messageText.text = $"{totalAmount} {resourceName}";
    }

    private void StartFadeCoroutine()
    {
        fadeCoroutine = StartCoroutine(FadeOutRoutine());
    }

    private void RestartFadeCoroutine()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        StartFadeCoroutine();
    }

    private IEnumerator FadeOutRoutine()
    {
        // Wait for display duration
        yield return new WaitForSeconds(displayDuration);

        // Fade out
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        float fadeDuration = 1f;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        canvasGroup.alpha = 0f;

        // Remove from manager and destroy
        manager.RemoveNotification(resourceName);
        Destroy(gameObject);
    }
}
