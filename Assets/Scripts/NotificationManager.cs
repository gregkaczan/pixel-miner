using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class NotificationManager : MonoBehaviour
{
    public GameObject notificationPrefab; // Assign in Inspector
    public Transform notificationsContainer; // Assign in Inspector
    public float displayDuration = 2f; // Duration before fade-out

    // Dictionary to track active notifications
    private Dictionary<string, NotificationItem> activeNotifications = new Dictionary<string, NotificationItem>();

    public void ShowNotification(string resourceName, int amount)
    {
        if (activeNotifications.ContainsKey(resourceName))
        {
            // Update existing notification
            activeNotifications[resourceName].UpdateAmount(amount);
        }
        else
        {
            // Create new notification
            GameObject notificationGO = Instantiate(notificationPrefab, notificationsContainer);
            NotificationItem notificationItem = notificationGO.GetComponent<NotificationItem>();
            notificationItem.Initialize(resourceName, amount, displayDuration, this);

            // Add to active notifications
            activeNotifications.Add(resourceName, notificationItem);
        }
    }

    public void RemoveNotification(string resourceName)
    {
        if (activeNotifications.ContainsKey(resourceName))
        {
            activeNotifications.Remove(resourceName);
        }
    }

    public void DebugShowNotification(string text)
    {
        ShowNotification(text, 1);
    }
}
