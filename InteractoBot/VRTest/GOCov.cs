using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GOCov
{
	public bool isStatic;
	public Vector3 center;
	public Vector3 extent;
	public Vector3 rotation;
	public FaceCov xPlus;
	public FaceCov xMinus;
	public FaceCov yPlus;
	public FaceCov yMinus;
	public FaceCov zPlus;
	public FaceCov zMinus;
	
	public GOCov(Vector3 center, Vector3 extent){
		this.center = center;
		this.extent = extent;
		float gran = SceneCov.FaceGran;
		this.xPlus = new FaceCov(center + new Vector3(extent.x, 0f, 0f), new Vector3(0f, extent.y, extent.z), gran, this.center);
		this.xMinus = new FaceCov(center - new Vector3(extent.x, 0f, 0f), new Vector3(0f, extent.y, extent.z), gran, this.center);
		this.yPlus = new FaceCov(center + new Vector3(0f, extent.y, 0f), new Vector3(extent.x, 0f, extent.z), gran, this.center);
		this.yMinus = new FaceCov(center - new Vector3(0f, extent.y, 0f), new Vector3(extent.x, 0f, extent.z), gran, this.center);
		this.zPlus = new FaceCov(center + new Vector3(0f, 0f, extent.z), new Vector3(extent.x, extent.y, 0f), gran, this.center);
		this.zMinus = new FaceCov(center - new Vector3(0f, 0f, extent.z), new Vector3(extent.x, extent.y, 0f), gran, this.center);
	}
	
	public void reInit(Vector3 center, Vector3 rotation){
		this.center = center;
		this.xPlus.reInit(center + new Vector3(extent.x, 0f, 0f), rotation, this.center);
		this.xMinus.reInit(center - new Vector3(extent.x, 0f, 0f), rotation, this.center);
		this.yPlus.reInit(center + new Vector3(0f, extent.y, 0f), rotation, this.center);
		this.yMinus.reInit(center - new Vector3(0f, extent.y, 0f), rotation, this.center);
		this.zPlus.reInit(center + new Vector3(0f, 0f, extent.z), rotation, this.center);
		this.zMinus.reInit(center - new Vector3(0f, 0f, extent.z), rotation, this.center);
	}
	
	public void computeOutOfScope(Vector3 scopeC, Vector3 scopeE){
		if(center.x + extent.x > scopeC.x + scopeE.x || center.x + extent.x < scopeC.x - scopeE.x - SceneCov.viewDistance){
			Debug.Log("OutOfScope: xPlus");
			this.xPlus.setOutOfScope(true);
		}
		if(center.x - extent.x < scopeC.x - scopeE.x || center.x - extent.x > scopeC.x + scopeE.x + SceneCov.viewDistance){
			Debug.Log("OutOfScope: xMinus");
			this.xMinus.setOutOfScope(true);
		}
		if(center.y + extent.y > scopeC.y + scopeE.y || center.y + extent.y < scopeC.y - scopeE.y - SceneCov.viewDistance){
			Debug.Log("OutOfScope: yPlus");
			this.yPlus.setOutOfScope(true);
		}
		if(center.y - extent.y < scopeC.y - scopeE.y || center.y - extent.y > scopeC.y + scopeE.y + SceneCov.viewDistance){
			Debug.Log("OutOfScope: yMinus");
			this.yMinus.setOutOfScope(true);
		}
		if(center.z + extent.z > scopeC.z + scopeE.z || center.z + extent.z < scopeC.z - scopeE.z - SceneCov.viewDistance){
			Debug.Log("OutOfScope: zPlus");
			this.zPlus.setOutOfScope(true);
		}
		if(center.z - extent.z < scopeC.z - scopeE.z || center.z - extent.z > scopeC.z + scopeE.z + SceneCov.viewDistance){
			Debug.Log("OutOfScope: zMinus");
			this.zMinus.setOutOfScope(true);
		}
	}
	
	public void computeDeadAreas(GOCov go){
		this.xPlus.computeDeadAreas(go);
		this.xMinus.computeDeadAreas(go);
		this.yPlus.computeDeadAreas(go);
		this.yMinus.computeDeadAreas(go);
		this.zPlus.computeDeadAreas(go);
		this.zMinus.computeDeadAreas(go);
	}
}



