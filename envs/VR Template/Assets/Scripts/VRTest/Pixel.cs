using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Pixel
{
	float curZ;
	FaceCov curFace;
	int faceX;
	int faceY;
	
	public Pixel(){
		curFace = null;
		faceX = -1;
		faceY = -1;
		curZ = SceneCov.viewDistance;
	}
	public void reset(){
		if(curFace != null){
			curFace = null;
			faceX = -1;
			faceY = -1;
			curZ = SceneCov.viewDistance;
		}
	}
	public bool checkSet(int x, int y, float z, FaceCov face){
		if(z > 0.0f && z < curZ){
			curFace = face;
			faceX = x;
			faceY = y;
			curZ = z;
			return true;
		}else{
			return false;
		}
	}
	public void setZ(float z, FaceCov face){
		this.curZ = z;
		if(this.curFace!=face){
			this.curFace = null;
		}
	}
	
	public void addCov(){
		if(curFace!=null){
			curFace.covPoint(faceX, faceY);
		}
	}
}



