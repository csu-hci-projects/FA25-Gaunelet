using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

// This script only runs in the Unity Editor and provides a new menu item.
public class RemoveColliders : EditorWindow
{
    // Define the menu item location: Tools -> Cleanup -> Remove All Box Colliders
    [MenuItem("Tools/Cleanup/Remove All Box Colliders from Selection")]
    public static void RemoveAllBoxCollidersFromSelection()
    {
        // Must select objects to run the tool
        if (Selection.gameObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", "Please select the parent object (or all individual objects) you want to clean up in the Hierarchy.", "OK");
            return;
        }

        int collidersRemoved = 0;
        
        // Use an outer loop to iterate through every root object you have selected
        foreach (GameObject selectedObject in Selection.gameObjects)
        {
            // Get ALL BoxColliders in the selected object AND its entire child hierarchy
            BoxCollider[] colliders = selectedObject.GetComponentsInChildren<BoxCollider>(true);

            // Use a list for safety, although GetComponentsInChildren already returns a new array
            List<BoxCollider> collidersList = new List<BoxCollider>(colliders);

            foreach (BoxCollider collider in collidersList)
            {
                // DestroyImmediate must be used when working with components in the Editor,
                // instead of Destroy(), which is for runtime.
                DestroyImmediate(collider, true);
                collidersRemoved++;
            }
        }
        
        // Display a summary of the operation
        EditorUtility.DisplayDialog("Cleanup Complete", 
                                    $"Successfully removed {collidersRemoved} Box Colliders from the selected object(s) and their children.", 
                                    "Great!");
    }
}