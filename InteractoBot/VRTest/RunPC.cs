using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

public class RunPC : MonoBehaviour
{
    public GameObject canvas;
    private bool Isvisible = false;

    public AudioSource source;
    [SerializeField]
    private AudioClip StartSound;
    [SerializeField]
    private AudioClip EndSound;

    public void TogglePC()
    {
		MethodBase mbase = MethodBase.GetCurrentMethod();
		SceneCov.methods.Add(mbase.ReflectedType.Name + ":" + mbase.Name);

        if (!Isvisible)
        {
            StartPC();
        }
        else
        {
            ShutDownPC();
        }
    }

    private void StartPC()
    {
		MethodBase mbase = MethodBase.GetCurrentMethod();
		SceneCov.methods.Add(mbase.ReflectedType.Name + ":" + mbase.Name);

        canvas.SetActive(true);
        source.clip = StartSound;
        source.Play();
        Isvisible = true;
    }

    private void ShutDownPC()
    {
		MethodBase mbase = MethodBase.GetCurrentMethod();
		SceneCov.methods.Add(mbase.ReflectedType.Name + ":" + mbase.Name);

        canvas.SetActive(false);
        source.clip = EndSound;
        source.Play();
        Isvisible = false;
    }
}