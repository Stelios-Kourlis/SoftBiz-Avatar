using UnityEditor;
using UnityEngine;

public class MissingScriptFinder : EditorWindow
{
    [MenuItem("Tools/Find Missing Scripts")]
    static void FindMissingScripts()
    {
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        int count = 0;

        foreach (GameObject go in allObjects)
        {
            var components = go.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp == null)
                {
                    Debug.Log($"Missing script found in GameObject: {go.name} (Instance ID: {go.GetInstanceID()})", go);
                    count++;
                }
            }
        }

        Debug.Log($"Found {count} missing scripts.");
    }
}
