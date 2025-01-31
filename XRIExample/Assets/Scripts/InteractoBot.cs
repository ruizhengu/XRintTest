using System.Collections.Generic;
using UnityEngine;

public class InteractoBot : MonoBehaviour
{
    protected static Dictionary<GameObject, string> interactables = new Dictionary<GameObject, string>(); 
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        

    }

    // Update is called once per frame
    void Update()
    {
        GameObject mainCamera = GameObject.FindWithTag("MainCamera");
        Debug.Log("mainCamera: (" + mainCamera.transform.position + ") (" + mainCamera.transform.rotation + ")");
        GameObject leftController = GameObject.FindWithTag("LeftController");
        if (leftController) {
            Debug.Log("leftController: (" + leftController.transform.position + ") (" + leftController.transform.rotation + ")");
        } else {
            Debug.Log("leftController not found");
        }
        GameObject rightController = GameObject.FindWithTag("RightController");
        if (rightController) {
            Debug.Log("rightController: (" + rightController.transform.position + ") (" + rightController.transform.rotation + ")");
        } else {
            Debug.Log("rightController not found");
        }

    }
}
