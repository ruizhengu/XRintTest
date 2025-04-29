using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class FaceCov
{
	bool outOfScope;
	int[,] points; //coverage profile: -1 dead, 0 uncovered, 1 covered 
	Vector3[,] poses;
	bool[,] posesReady;
	int direction; //-1 : x intersection, 0 : y intersection, 1 : z intersection
	float granulate;
	Vector3 center;
	Vector3 extent;
	Vector3 rotation;
	Vector3 goCenter;
	public FaceCov(Vector3 center, Vector3 extent, float granulate, Vector3 goCenter){
		this.center = center;
		this.extent = extent;
		this.granulate = granulate;
		this.goCenter = goCenter;
		float extent0 = 0f;
		float extent1 = 0f;
		if(extent.x < 0.01f){
			direction = -1;
			extent0 = extent.y * 2;
			extent1 = extent.z * 2;
		}else if(extent.y < 0.01f){
			direction = 0;
			extent0 = extent.x * 2;
			extent1 = extent.z * 2;
		}else if(extent.z < 0.01f){
			direction = 1;
			extent0 = extent.x * 2;
			extent1 = extent.y * 2;
		}
		points = new int[Mathf.FloorToInt(extent0/granulate), Mathf.FloorToInt(extent1/granulate)];
		poses = new Vector3[Mathf.FloorToInt(extent0/granulate), Mathf.FloorToInt(extent1/granulate)];
		posesReady = new bool[Mathf.FloorToInt(extent0/granulate), Mathf.FloorToInt(extent1/granulate)];
		outOfScope = false;
	}
	
	public void reInit(Vector3 center, Vector3 rotation, Vector3 goCenter){
		this.center = center;
		this.rotation = rotation;
		this.goCenter = goCenter;
		int xlen = poses.GetLength(0);
		int ylen = poses.GetLength(1);
		for (int i = 0; i < xlen; i++){
			for(int j = 0; j < ylen; j++){
				posesReady[i,j] = false;
			}
		}
	}
	
	public void computeDeadAreas(GOCov go){
		int xlen = points.GetLength(0);
		int ylen = points.GetLength(1);
		for (int i = 0; i < xlen; i++){
			for(int j = 0; j < ylen; j++){
				Vector3 pos = calPos(i, j);
				if (pos.x < go.center.x + go.extent.x + SceneCov.FaceGran && pos.x > go.center.x - go.extent.x - SceneCov.FaceGran){
					if (pos.y < go.center.y + go.extent.y + SceneCov.FaceGran && pos.y > go.center.y - go.extent.y - SceneCov.FaceGran){
						if (pos.z < go.center.z + go.extent.z + SceneCov.FaceGran && pos.z > go.center.z - go.extent.z - SceneCov.FaceGran){
							points[i, j] = -1;
						}
					}
				}
			}
		}
	}
	
	public void setOutOfScope(bool outOfScope){
		this.outOfScope = outOfScope;
	}
	
	public void covPoint(int x, int y){
		if(points[x,y] == 0){
			points[x,y] = 1;
		}
	}
	public int getSumPoints(){
		if(outOfScope){
			return 0;
		}
		int xlen = points.GetLength(0);
		int ylen = points.GetLength(1);
		int sum = 0;
		for (int i = 0; i < xlen; i++){
			for(int j = 0; j < ylen; j++){
				if(points[i,j] != -1){
					sum = sum + 1;
				}
			}
	 	}
		return sum;
	}
	public int getCoveredPoints(){
		if(outOfScope){
			return 0;
		}

		int xlen = points.GetLength(0);
		int ylen = points.GetLength(1);
		int sum = 0;
		for (int i = 0; i < xlen; i++){
			for(int j = 0; j < ylen; j++){
				if(points[i,j] == 1){
					sum = sum + 1;
				}
			}
		}
		return sum;
	}
	public void checkCov(Camera cam, Pixel[,] screen){
		if(outOfScope){
			return;
		}
		int xlen = points.GetLength(0);
		int ylen = points.GetLength(1);
		if(xlen == 0 || ylen == 0){
			return;
		}
		if(SceneCov.quad){
			recursiveQuadCov(cam, screen, 0, xlen, 0, ylen);
		}else{
			if(!alwaysOut(0, xlen - 1, 0, ylen - 1, cam)){
				checkCovArea(cam, screen, 0, xlen, 0, ylen);
			}
		}
	}
	
	public void checkCovArea(Camera cam, Pixel[,] screen, int x0, int x1, int y0, int y1){
		for (int i = x0; i < x1; i++){
			for(int j = y0; j < y1; j++){
				calCamPos(i, j, cam);
			}
		}					
		for (int i = x0; i < x1; i++){
			for(int j = y0; j < y1; j++){
				Vector3 camPos = poses[i,j];
				if(camPos.x < 1 && camPos.x > 0 && camPos.y < 1 && camPos.y > 0){
					int x = Mathf.FloorToInt(camPos.x * screen.GetLength(0));
					int y = Mathf.FloorToInt(camPos.y * screen.GetLength(1));
					if(screen[x,y].checkSet(i, j, camPos.z, this)){
						if(i >= x0 + 1 && j >= y0 + 1){
							fillTriangle(camPos, poses[i-1,j], poses[i,j-1], screen);
						}
						if(i < x1 - 1 && j < y1 - 1){
							fillTriangle(camPos, poses[i+1,j], poses[i,j+1], screen);
						}
					}
				}
			}						
		}
	}
	
	private void fillTriangle(Vector3 p1, Vector3 p2, Vector3 p3, Pixel[,] screen){
		Vector3 xlow = minX(p1, p2, p3);
		Vector3 ylow = minY(p1, p2, p3);
		Vector3 xhigh = maxX(p1, p2, p3);
		Vector3 yhigh = maxY(p1, p2, p3);
		Vector2Int xlowInt = new Vector2Int(Mathf.RoundToInt(xlow.x * screen.GetLength(0)), Mathf.RoundToInt(xlow.y * screen.GetLength(1)));
		Vector2Int ylowInt = new Vector2Int(Mathf.RoundToInt(ylow.x * screen.GetLength(0)), Mathf.RoundToInt(ylow.y * screen.GetLength(1)));
		Vector2Int xhighInt = new Vector2Int(Mathf.RoundToInt(xhigh.x * screen.GetLength(0)), Mathf.RoundToInt(xhigh.y * screen.GetLength(1)));
		Vector2Int yhighInt = new Vector2Int(Mathf.RoundToInt(yhigh.x * screen.GetLength(0)), Mathf.RoundToInt(yhigh.y * screen.GetLength(1)));
//		Debug.Log(p1*1000 + ";" + p2*1000 + ";" +  p3*1000 + ";" + xlowInt + ":" + ylowInt + ":" + xhighInt + ":" + yhighInt + ":" + p1.z);

		float xgrad = xhighInt.x == xlowInt.x ? 0f : (xhigh.z - xlow.z) / (xhighInt.x - xlowInt.x);
		float ygrad = yhighInt.y == ylowInt.y ? 0f : (yhigh.z - ylow.z) / (yhighInt.y - ylowInt.y);

		if(xlow.x == xhigh.x){
			for (int y = ylowInt.y; y <= yhighInt.y; y++){
				float z = ylow.z + ygrad * (y - ylowInt.y);
				if(xlowInt.x >= 0 && xlowInt.x < screen.GetLength(0) && y >=0 && y < screen.GetLength(1)){
					screen[xlowInt.x, y].setZ(z, this);
				}
			}
			return;
		}
		
		float xlowKup = 0f;
		float xlowKdown = 0f;
		float xhighKup = 0f;
		float xhighKdown = 0f;
		
		if(xlowInt.x == ylowInt.x && xlowInt.y == ylowInt.y && xhighInt.x == yhighInt.x && xhighInt.y == yhighInt.y){
			return;
		}
		
		xlowKup = (yhighInt.x == xlowInt.x && yhighInt.y == xlowInt.y) ? slide(xhighInt, xlowInt) : slide(yhighInt, xlowInt);
		xlowKdown = (ylowInt.x == xlowInt.x && ylowInt.y == xlowInt.y) ? slide(xhighInt, xlowInt) : slide(ylowInt, xlowInt);
		xhighKup = (yhighInt.x == xhighInt.x && yhighInt.y == xhighInt.y) ? slide(xhighInt, xlowInt) : slide(xhighInt, yhighInt);
		xhighKdown = (ylowInt.x == xhighInt.x && ylowInt.y == xhighInt.y) ? slide(xhighInt, xlowInt) : slide(xhighInt, ylowInt);

		for (int x = xlowInt.x; x <= xhighInt.x; x++){
			for (int y = ylowInt.y; y <= yhighInt.y; y++){
				bool inAngleLeft = xlowKup*(x - xlowInt.x) + xlowInt.y >= y && xlowKdown*(x - xlowInt.x) + xlowInt.y <= y;
				bool inAngleRight = xhighInt.y - xhighKdown * (xhighInt.x - x) <=y && xhighInt.y - xhighKup * (xhighInt.x - x) >= y;
				if(inAngleLeft && inAngleRight){			
					float z = ylow.z + xgrad * (x - ylowInt.x) + ygrad * (y - ylowInt.y);
					if(x >= 0 && x < screen.GetLength(0) && y >=0 && y < screen.GetLength(1)){
						screen[x,y].setZ(z, this);
					}
				}
			}
		}
	}
	
	private float slide(Vector2Int p1, Vector2Int p2){
		if(p1.x == p2.x){
			if(p1.y > p2.y){
				return Mathf.Infinity;
			}else{
				return Mathf.NegativeInfinity;
			}
		}
		return 1.0f*(p1.y - p2.y)/(p1.x - p2.x);
	}
	
	private Vector3 minX(Vector3 p1, Vector3 p2, Vector3 p3){
		if(p1.x < p2.x){
			if(p1.x < p3.x){
				return p1;
			}else{
				return p3;
			}
		}else{
			if(p2.x < p3.x){
				return p2;
			}else{
				return p3;
			}
		}
	}
	private Vector3 minY(Vector3 p1, Vector3 p2, Vector3 p3){
		if(p1.y < p2.y){
			if(p1.y < p3.y){
				return p1;
			}else{
				return p3;
			}
		}else{
			if(p2.y < p3.y){
				return p2;
			}else{
				return p3;
			}
		}
	}
	private Vector3 maxX(Vector3 p1, Vector3 p2, Vector3 p3){
		if(p1.x > p2.x){
			if(p1.x > p3.x){
				return p1;
			}else{
				return p3;
			}
		}else{
			if(p2.x > p3.x){
				return p2;
			}else{
				return p3;
			}
		}
	}
	private Vector3 maxY(Vector3 p1, Vector3 p2, Vector3 p3){
		if(p1.y > p2.y){
			if(p1.y > p3.y){
				return p1;
			}else{
				return p3;
			}
		}else{
			if(p2.y > p3.y){
				return p2;
			}else{
				return p3;
			}
		}
	}	
	
	private Vector3 calPos(int i, int j){
		Vector3 pos;
		if(direction == -1){
			pos = new Vector3(center.x, center.y - extent.y + granulate * i, center.z - extent.z + granulate * j);
		}else if (direction == 0){
			pos = new Vector3(center.x - extent.x + granulate * i, center.y, center.z - extent.z + granulate * j);
		}else{
			pos = new Vector3(center.x - extent.x + granulate * i, center.y - extent.y + granulate * j, center.z);
		}
		return pos;
	}
	
	private Vector3 calCamPos(int i, int j, Camera cam){
		Debug.Log(i + "," + j + ":" + posesReady.GetLength(0) + "," + posesReady.GetLength(1));
		if(posesReady[i, j]){
			return poses[i, j];
		}else{
			Vector3 pos = calPos(i, j);
			Vector3 posRotate = rotate(pos, rotation, this.goCenter);
			Vector3 camPos = cam.WorldToViewportPoint(posRotate);
			poses[i, j] = camPos;
			posesReady[i, j] = true;
			return poses[i, j];
		}
	}
	
	private Vector3 rotate(Vector3 ori, Vector3 rotation, Vector3 center){
		return ori;
	}
	
	private bool alwaysOut(int x0, int x1, int y0, int y1, Camera cam){
		Vector3 x0y0 = calCamPos(x0, y0, cam);
		Vector3 x0y1 = calCamPos(x0, y1, cam);
		Vector3 x1y0 = calCamPos(x1, y0, cam);
		Vector3 x1y1 = calCamPos(x1, y1, cam);
		Debug.Log("x0y0:" + x0y0);
		Debug.Log("x0y1:" + x0y1);
		Debug.Log("x1y0:" + x1y0);
		Debug.Log("x1y1:" + x1y1);

		if(x0y0.x > 1 && x0y1.x > 1 && x1y0.x > 1 && x1y1.x > 1){
			return true;
		}else if (x0y0.x < 0 && x0y1.x < 0 && x1y0.x < 0 && x1y1.x < 0){
			return true;
		}else if (x0y0.y < 0 && x0y1.y < 0 && x1y0.y < 0 && x1y1.y < 0){
			return true;
		}else if (x0y0.y > 1 && x0y1.y > 1 && x1y0.y > 1 && x1y1.y > 1){
			return true;
		}else if (x0y0.z < 0 && x0y1.z < 0 && x1y0.z < 0 && x1y1.z < 0){
			return true;
		}
		return false;
	}
	
	public void recursiveQuadCov(Camera cam, Pixel[,] screen, int x0, int x1, int y0, int y1){
		if((x1 - x0 + 1) * (y1 - y0 + 1) < 100){
			checkCovArea(cam, screen, x0, x1, y0, y1);
			return;
		}
		if(!alwaysOut(x0, x1/2, y0, y1/2, cam)){
			recursiveQuadCov(cam, screen, x0, x1/2, y0, y1/2);
		}else if(!alwaysOut(x1/2, x1, y0, y1/2, cam)){
			recursiveQuadCov(cam, screen, x1/2, x1, y0, y1/2);			
		}else if(!alwaysOut(x0, x1/2, y1/2, y1, cam)){
			recursiveQuadCov(cam, screen, x0, x1/2, y1/2, y1);			
		}else if(!alwaysOut(x1/2, x1, y1/2, y1, cam)){
			recursiveQuadCov(cam, screen, x1/2, x1, y1/2, y1);			
		}
	}
}