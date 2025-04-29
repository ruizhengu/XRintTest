using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class SoundPlayer : MonoBehaviour
{
    public AudioSource source;
    public AudioClip sound;

   public void PlaySound()
    {
		MethodBase mbase = MethodBase.GetCurrentMethod();
		SceneCov.methods.Add(mbase.ReflectedType.Name + ":" + mbase.Name);

        source.clip = sound;
        source.Play();
    }
}
