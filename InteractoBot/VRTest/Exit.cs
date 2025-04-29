using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

public class Exit : MonoBehaviour
{
    public void ExitScene()
    {
		MethodBase mbase = MethodBase.GetCurrentMethod();
		SceneCov.methods.Add(mbase.ReflectedType.Name + ":" + mbase.Name);

        Application.Quit();
    }
}
