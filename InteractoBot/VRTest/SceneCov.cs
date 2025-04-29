using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;

public class SceneCov : MonoBehaviour
{
	public static List<string> methods = new List<string>();

	public static float FaceGran = 0.1f;
	public static float viewDistance = 344f;
	public static bool quad = false;

	Pixel[,] screen = new Pixel[1000, 1000];
	Dictionary<string, GOCov> gos = new Dictionary<string, GOCov>();
	List<List<string>> covData = new List<List<string>>();
	Vector3 Campos;
	Quaternion CamRot;
	Camera mainCam;

	// Start is called before the first frame update
	void Start()
	{
		mainCam = Camera.main;
		for (int i = 0; i < 1000; i++)
		{
			for (int j = 0; j < 1000; j++)
			{
				screen[i, j] = new Pixel();
			}
		}

		//		Vector3 scopeC = new Vector3(-4.8f, 4.3f, 5.7f);
		//		Vector3 scopeE = new Vector3(11.9f, 0.1f, 6.7f);
		Vector3 scopeC = new Vector3(0f, 0f, -10f);
		Vector3 scopeE = new Vector3(2f, 0.1f, 1f);

		string covLogPath = "D:/SceneCoverage/Logs/room/covLog_3";
		string covReportPath = "D:/SceneCoverage/Logs/room/covReport_3";
		loadAllBatches(covLogPath);
		loadNewBatch(covData[0]);

		StreamWriter covReport = new StreamWriter(covReportPath);
		foreach (KeyValuePair<string, GOCov> entry in gos)
		{
			GOCov go = entry.Value;
			if (go.isStatic)
			{
				go.computeOutOfScope(scopeC, scopeE);
				foreach (KeyValuePair<string, GOCov> entry1 in gos)
				{
					GOCov go1 = entry1.Value;
					if (go1 != go && go1.isStatic)
					{
						go.computeDeadAreas(go1);
					}
				}
			}
		}

		foreach (List<string> batch in covData)
		{
			loadNewBatch(batch);

			mainCam.transform.position = Campos;
			mainCam.transform.rotation = CamRot;

			foreach (KeyValuePair<string, GOCov> entry in gos)
			{
				GOCov go = entry.Value;
				Debug.Log("checking...xPlus");
				go.xPlus.checkCov(mainCam, screen);
				Debug.Log("checking...xMinus");
				go.xMinus.checkCov(mainCam, screen);
				Debug.Log("checking...yPlus");
				go.yPlus.checkCov(mainCam, screen);
				Debug.Log("checking...yMinus");
				go.yMinus.checkCov(mainCam, screen);
				Debug.Log("checking...zPlus");
				go.zPlus.checkCov(mainCam, screen);
				Debug.Log("checking...zMinus");
				go.zMinus.checkCov(mainCam, screen);
			}
			for (int i = 0; i < screen.GetLength(0); i++)
			{
				for (int j = 0; j < screen.GetLength(1); j++)
				{
					screen[i, j].addCov();
					screen[i, j].reset();
				}
			}
			reportCov(gos, covReport);
		}
		covReport.Close();
		/*		Debug.Log(transform.rotation);
				GameObject go = GameObject.Find("Cube");
				Debug.Log(go.GetComponent<Renderer>().bounds.center);
				Debug.Log(go.GetComponent<Renderer>().bounds.extents);
				Debug.Log(go.transform.eulerAngles);*/
	}

	public void loadNewBatch(List<string> batch)
	{
		int i = 0;
		while (i < batch.Count)
		{
			string name = batch[i].Trim();
			i = i + 1;
			Vector3 position = parseVector3(batch[i]);
			i = i + 1;
			if (name.Equals("mainCamera"))
			{
				Campos = position;
				CamRot = parseQuarternion(batch[i]);
				i = i + 1;
			}
			else
			{
				Vector3 extent = parseVector3(batch[i]);
				i = i + 1;
				i = i + 1;
				if (gos.ContainsKey(name))
				{
					gos[name].reInit(position, new Vector3(0f, 0f, 0f));
				}
				else
				{
					gos[name] = new GOCov(position, extent);
					gos[name].isStatic = true;
				}
			}
		}
	}

	public Vector3 parseVector3(string line)
	{
		Debug.Log(line);

		string baseline = line.Trim();
		string middle = baseline.Substring(1, baseline.Length - 2);
		string[] words = middle.Split(',');

		return new Vector3(Single.Parse(words[0].Trim()), Single.Parse(words[1].Trim()), Single.Parse(words[2].Trim()));
	}

	public Quaternion parseQuarternion(string line)
	{
		string baseline = line.Trim();
		string middle = baseline.Substring(1, baseline.Length - 2);
		string[] words = middle.Split(',');
		return new Quaternion(Single.Parse(words[0].Trim()), Single.Parse(words[1].Trim()), Single.Parse(words[2].Trim()), Single.Parse(words[2].Trim()));
	}

	public void reportCov(Dictionary<string, GOCov> gos, StreamWriter sw)
	{
		int allpoints = 0;
		int covered = 0;
		foreach (KeyValuePair<string, GOCov> entry in gos)
		{
			GOCov go = entry.Value;
			string name = entry.Key;
			int allgo = 0;
			int covgo = 0;
			allgo += go.xPlus.getSumPoints();
			covgo += go.xPlus.getCoveredPoints();
			Debug.Log("xPlus:" + covgo + "/" + allgo);
			allgo += go.yPlus.getSumPoints();
			covgo += go.yPlus.getCoveredPoints();
			Debug.Log("yPlus:" + covgo + "/" + allgo);
			allgo += go.zPlus.getSumPoints();
			covgo += go.zPlus.getCoveredPoints();
			Debug.Log("zPlus:" + covgo + "/" + allgo);
			allgo += go.xMinus.getSumPoints();
			covgo += go.xMinus.getCoveredPoints();
			Debug.Log("xMinus:" + covgo + "/" + allgo);
			allgo += go.yMinus.getSumPoints();
			covgo += go.yMinus.getCoveredPoints();
			Debug.Log("yMinus:" + covgo + "/" + allgo);
			allgo += go.zMinus.getSumPoints();
			covgo += go.zMinus.getCoveredPoints();
			Debug.Log("zMinus:" + covgo + "/" + allgo);

			allpoints += allgo;
			covered += covgo;
			sw.WriteLine(name + "\t" + 1.0 * covgo / allgo);
		}
		sw.WriteLine("Overall Coverage:\t" + 1.0 * covered / allpoints);
	}

	public void loadAllBatches(string logPath)
	{
		StreamReader sr = new StreamReader(logPath);
		while (sr.Peek() >= 0)
		{
			string line = sr.ReadLine();
			if (line.Trim().Equals("mainCamera"))
			{
				List<string> batch = new List<string>();
				batch.Add(line);
				covData.Add(batch);
			}
			else if (line.Trim().StartsWith("TimeStamp:"))
			{
				;
			}
			else
			{
				if (covData.Count > 0)
				{
					covData[covData.Count - 1].Add(line);
				}
			}
		}
		sr.Close();
	}
}
