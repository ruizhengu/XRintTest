using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Reflection;

public class SpawnPaper : MonoBehaviour
{
    [SerializeField]
    private AudioClip clip;

    [SerializeField]
    private GameObject paperToSpawn;

    [SerializeField]
    private GameObject position;

    public void spawn()
    {
		MethodBase mbase = MethodBase.GetCurrentMethod();
		SceneCov.methods.Add(mbase.ReflectedType.Name + ":" + mbase.Name);

        StartCoroutine(ExecuteAfterTime(clip.length));
    }

    public IEnumerator ExecuteAfterTime(float time)
    {
		MethodBase mbase = MethodBase.GetCurrentMethod();
		SceneCov.methods.Add(mbase.ReflectedType.Name + ":" + mbase.Name);

        yield return new WaitForSeconds(time);
        Instantiate(paperToSpawn, position.transform.position, position.transform.rotation);
    }
}
 