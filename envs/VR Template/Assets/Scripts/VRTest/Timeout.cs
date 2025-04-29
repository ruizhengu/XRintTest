using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

public class Timeout : MonoBehaviour
{
    public GameObject canvas;

    // Start is called before the first frame update
    void Start()
    {
		MethodBase mbase = MethodBase.GetCurrentMethod();
		SceneCov.methods.Add(mbase.ReflectedType.Name + ":" + mbase.Name);

        StartCoroutine(ExecuteAfterTime(60));
    }

    private void Update()
    {
		MethodBase mbase = MethodBase.GetCurrentMethod();
		SceneCov.methods.Add(mbase.ReflectedType.Name + ":" + mbase.Name);

        if (Input.GetButtonDown("Fire1"))
        {
            canvas.SetActive(false);
        }
    }

    public IEnumerator ExecuteAfterTime(float time)
    {
		MethodBase mbase = MethodBase.GetCurrentMethod();
		SceneCov.methods.Add(mbase.ReflectedType.Name + ":" + mbase.Name);

        yield return new WaitForSeconds(time);
        canvas.SetActive(false);
    }
}
