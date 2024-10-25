using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(NotificationManager))]
public class NotificationManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();

        // Add a space in the Inspector
        GUILayout.Space(10);

        // Reference to the NotificationManager
        NotificationManager manager = (NotificationManager)target;

        // Add a button to trigger the notification
        if (GUILayout.Button("Trigger Test Notification"))
        {
            manager.DebugShowNotification("Iron ore");
        }

        if (GUILayout.Button("Trigger Test Notification 2"))
        {
            manager.DebugShowNotification("Copper ore");
        }
    }
}