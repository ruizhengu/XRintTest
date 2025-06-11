using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class SceneChecker : EditorWindow
{
    [MenuItem("Tools/Check Scene Requirements")]
    public static void CheckSceneRequirements()
    {
        // Check if XR Device Simulator folder exists
        string simulatorPath = Path.Combine(Application.dataPath, "Samples/XR Interaction Toolkit/3.1.1/XR Device Simulator/XRInteractionSimulator");
        if (Directory.Exists(simulatorPath))
        {
            Debug.Log("XR Device Simulator folder exists at: " + simulatorPath);

            // Check if XR Interaction Simulator prefab exists in the scene
            string prefabPath = Path.Combine(simulatorPath, "XR Interaction Simulator.prefab");
            if (File.Exists(prefabPath))
            {
                GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
                bool simulatorExists = false;
                GameObject simulator = null;

                foreach (GameObject obj in rootObjects)
                {
                    if (obj.name == "XR Interaction Simulator")
                    {
                        simulatorExists = true;
                        simulator = obj;
                        Debug.Log("XR Interaction Simulator already exists in the scene.");
                        break;
                    }
                }

                if (!simulatorExists)
                {
                    // Load and instantiate the prefab
                    GameObject simulatorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Samples/XR Interaction Toolkit/3.1.1/XR Device Simulator/XRInteractionSimulator/XR Interaction Simulator.prefab");
                    if (simulatorPrefab != null)
                    {
                        simulator = PrefabUtility.InstantiatePrefab(simulatorPrefab) as GameObject;
                        Debug.Log("XR Interaction Simulator has been added to the scene.");
                    }
                    else
                    {
                        Debug.LogError("Failed to load XR Interaction Simulator prefab.");
                        return;
                    }
                }

                // Check for Right Controller and its Mesh Collider
                if (simulator != null)
                {
                    GameObject rightController = GameObject.Find("Right Controller");
                    if (rightController != null)
                    {
                        // Check if it has a Mesh Collider
                        MeshCollider meshCollider = rightController.GetComponent<MeshCollider>();
                        if (meshCollider == null)
                        {
                            // Add Mesh Collider if it doesn't exist
                            meshCollider = rightController.gameObject.AddComponent<MeshCollider>();
                            meshCollider.convex = true;
                            meshCollider.isTrigger = true;
                            Debug.Log("Added Mesh Collider to Right Controller with Convex and Is Trigger enabled.");
                        }
                        else
                        {
                            bool needsUpdate = false;
                            // Check and enable Convex if needed
                            if (!meshCollider.convex)
                            {
                                meshCollider.convex = true;
                                needsUpdate = true;
                                Debug.Log("Enabled Convex property on Right Controller's Mesh Collider.");
                            }

                            // Check and enable Is Trigger if needed
                            if (!meshCollider.isTrigger)
                            {
                                meshCollider.isTrigger = true;
                                needsUpdate = true;
                                Debug.Log("Enabled Is Trigger property on Right Controller's Mesh Collider.");
                            }

                            if (!needsUpdate)
                            {
                                Debug.Log("Right Controller's Mesh Collider already has Convex and Is Trigger enabled.");
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Right Controller not found in the scene.");
                    }
                }
            }
            else
            {
                Debug.LogError("XR Interaction Simulator prefab not found at: " + prefabPath);
            }
        }
        else
        {
            Debug.LogWarning("XR Device Simulator folder not found at: " + simulatorPath + ". Please install the XR Device Simulator.");
        }
    }
}
