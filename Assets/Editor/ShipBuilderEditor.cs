using UnityEngine;
using UnityEditor;
using Assets.Scripts;

[CustomEditor(typeof(PlayerInventory))]
public class ShipBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();

        // Add a space in the Inspector
        GUILayout.Space(10);

        // Reference to the NotificationManager
        PlayerInventory manager = (PlayerInventory)target;

        // Add a button to trigger the notification
        if (GUILayout.Button("Reload from save"))
        {
            manager.ReloadFromSave();
        }
    }
}