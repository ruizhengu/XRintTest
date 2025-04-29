using UnityEngine;
using System.Collections;
using System.Reflection;

public class Autowalk : MonoBehaviour
{
    private const int RIGHT_ANGLE = 90;

    // This variable determinates if the player will move or not 
    private bool isWalking = true;

    Transform mainCamera = null;

    //This is the variable for the player speed
    [Tooltip("With this speed the player will move.")]
    public float speed;

    [Tooltip("Activate this checkbox if the player shall move when the Cardboard trigger is pulled.")]
    public bool walkWhenTriggered;

    [Tooltip("Activate this checkbox if the player shall move when he looks below the threshold.")]
    public bool walkWhenLookDown;

    [Tooltip("This has to be an angle from 0° to 90°")]
    public double thresholdAngle;

    [Tooltip("Activate this Checkbox if you want to freeze the y-coordiante for the player. " +
             "For example in the case of you have no collider attached to your CardboardMain-GameObject" +
             "and you want to stay in a fixed level.")]
    public bool freezeYPosition;

    [Tooltip("This is the fixed y-coordinate.")]
    public float yOffset;
	

    void Start()
    {
		MethodBase mbase = MethodBase.GetCurrentMethod();
		SceneCov.methods.Add(mbase.ReflectedType.Name + ":" + mbase.Name);

        mainCamera = Camera.main.transform;
    }

    void Update()
    {
		MethodBase mbase = MethodBase.GetCurrentMethod();
		SceneCov.methods.Add(mbase.ReflectedType.Name + ":" + mbase.Name);
        // Walk when the Cardboard Trigger is used 
        if (walkWhenTriggered && !walkWhenLookDown && !isWalking && Input.GetButtonDown("Fire1"))
        {
            isWalking = true;
        }
        else if (walkWhenTriggered && !walkWhenLookDown && isWalking && Input.GetButtonDown("Fire1"))
        {
            isWalking = false;
        }

        // Walk when player looks below the threshold angle 
        if (walkWhenLookDown && !walkWhenTriggered && !isWalking &&
            mainCamera.transform.eulerAngles.x >= thresholdAngle &&
            mainCamera.transform.eulerAngles.x <= RIGHT_ANGLE)
        {
            isWalking = true;
        }
        else if (walkWhenLookDown && !walkWhenTriggered && isWalking &&
                 (mainCamera.transform.eulerAngles.x <= thresholdAngle ||
                 mainCamera.transform.eulerAngles.x >= RIGHT_ANGLE))
        {
            isWalking = false;
        }

        // Walk when the Cardboard trigger is used and the player looks down below the threshold angle
        if (walkWhenLookDown && walkWhenTriggered && !isWalking &&
            mainCamera.transform.eulerAngles.x >= thresholdAngle &&
            Input.GetButtonDown("Fire1") &&
            mainCamera.transform.eulerAngles.x <= RIGHT_ANGLE)
        {
            isWalking = true;
        }
        else if (walkWhenLookDown && walkWhenTriggered && isWalking &&
                 mainCamera.transform.eulerAngles.x >= thresholdAngle &&
                 (Input.GetButtonDown("Fire1") ||
                 mainCamera.transform.eulerAngles.x >= RIGHT_ANGLE))
        {
            isWalking = false;
        }
		
        if (isWalking)
        {
            Vector3 direction = new Vector3(mainCamera.transform.forward.x, 0, mainCamera.transform.forward.z).normalized * speed * Time.deltaTime;
            Quaternion rotation = Quaternion.Euler(new Vector3(0, -transform.rotation.eulerAngles.y, 0));
            transform.Translate(rotation * direction);
        }

        if (freezeYPosition)
        {
			transform.position = new Vector3(transform.position.x, yOffset, transform.position.z);
        }
    }



	public float passed;
	public int last;
	public Vector3 move;
	public Vector3 rotation;
	public Vector3 center = new Vector3(-2f, 4.3f, 7.5f);
	public Vector3 range = new Vector3(8f, 0f, 5f);

	
	void FixedUpdate(){
		passed = passed + Time.deltaTime;
		if(passed >= 1.0f){
			passed = 0f;
			last = randomChange(last);
			Debug.Log(move);
			Debug.Log(rotation);
		}
		randomWalk();
	}
	
	int randomChange(int lc){
		float r = Random.Range(0f, 10f);
		float h = 0f;
		float v = 0f;
		float x = 0f;
		float z = 0f;
		int choice = Mathf.FloorToInt(r);
		if(r < 1.0f){
			h = 10f;
			if(transform.eulerAngles.x >= 30f)
				Debug.Log("reach lowerbound");
				h = -10f;
		}else if (r < 2.0f){
			v = 10f;
			if(lc == 5)
				v = -10f;
		}else if (r < 3.0f){
			x = 1f;
			if(transform.position.x > center.x + range.x){
				Debug.Log("reach xbound");
				x = -1f;
			}
		}else if (r < 4.0f){
			z = 1f;
			if(transform.position.z > center.z + range.z){
				Debug.Log("reach zbound");
				z = -1f;
			}
		}else if (r < 5.0f){
			h = -10f;
			if(transform.eulerAngles.x <= -30f)
				Debug.Log("reach upperbound");
				h = 10f;
		}else if (r < 6.0f){
			v = -10f;
			if(lc == 1)
				v = 10f;
		}else if (r < 7.0f){
			x = -1f;
			if(transform.position.x < center.x - range.x){
				Debug.Log("reach xbound");
				x = 1f;
			}
		}else if (r < 8.0f){
			z = -1f;
			if(transform.position.z < center.z - range.z){
				z = 1f;
				Debug.Log("reach zbound");
			}
			choice = 7;
		}else{
			v = 10f;
			if(lc == 5)
				v = -10f;
			choice = 1;
		}
		rotation = new Vector3(h, v, 0f)/50f;
		move = new Vector3(x, 0f, z)/50f;
		return choice;
	}
	
	void randomWalk(){
		transform.position += move;
		transform.eulerAngles += rotation;
	}	
}