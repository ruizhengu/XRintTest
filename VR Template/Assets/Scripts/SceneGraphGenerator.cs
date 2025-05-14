using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Text;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[System.Serializable]
public class InteractionResult
{
    public string interactor;
    public List<string> condition;
    public string interactable;
    public string interaction_type;
}

public class SceneGraphGenerator : EditorWindow
{
    [MenuItem("Tools/Generate Scene Graph")]
    public static void GenerateSceneGraph()
    {
        GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        List<InteractionResult> results = new List<InteractionResult>();

        foreach (GameObject rootObj in rootObjects)
        {
            ProcessGameObject(rootObj, results);
        }

        string json = JsonUtility.ToJson(new SerializationWrapper<InteractionResult>(results), true);
        string path = Path.Combine(Application.dataPath, "Scripts/scene_graph.json");
        File.WriteAllText(path, json);
        Debug.Log($"Interaction results exported to {path}");
    }

    private static void ProcessGameObject(GameObject obj, List<InteractionResult> results)
    {
        // Check grab interactions
        var grabInteractable = obj.GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            var result = new InteractionResult
            {
                interactor = "XR Origin (XR Rig)",
                condition = new List<string>(),
                interactable = obj.name,
                interaction_type = "grab"
            };
            results.Add(result);
            // Check trigger interactions
            var activatedEvent = grabInteractable.activated;
            bool triggerInteraction = activatedEvent.GetPersistentEventCount() > 0;
            if (triggerInteraction)
            {
                var triggerResult = new InteractionResult
                {
                    interactor = "XR Origin (XR Rig)",
                    condition = new List<string> { "grab" },
                    interactable = obj.name,
                    interaction_type = "trigger"
                };
                results.Add(triggerResult);
            }
        }

        foreach (Transform child in obj.transform)
        {
            ProcessGameObject(child.gameObject, results);
        }
    }

    private static List<string> GetComponentsList(GameObject obj)
    {
        Component[] components = obj.GetComponents<Component>();
        List<string> componentNames = new List<string>();

        foreach (Component component in components)
        {
            if (component != null)
            {
                componentNames.Add(component.GetType().Name);
            }
        }

        return componentNames;
    }

    // Helper for serializing a list as a JSON array
    [System.Serializable]
    private class SerializationWrapper<T>
    {
        public List<T> items;
        public SerializationWrapper(List<T> items) { this.items = items; }
    }
}
