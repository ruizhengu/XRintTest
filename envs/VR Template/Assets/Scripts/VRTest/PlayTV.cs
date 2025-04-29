using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

public class PlayTV : MonoBehaviour
{
    public GameObject canvas;
    private bool Isvisible = false;

    public void ToggleVisibility()
    {
		MethodBase mbase = MethodBase.GetCurrentMethod();
		SceneCov.methods.Add(mbase.ReflectedType.Name + ":" + mbase.Name);

        Isvisible = !Isvisible;
        canvas.SetActive(Isvisible);
    }
}
