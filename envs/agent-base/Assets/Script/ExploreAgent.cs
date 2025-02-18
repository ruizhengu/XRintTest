using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Collections;
using System.Collections.Generic;
// using UnityEngine.InputSystem;

public class ExploreAgent : Agent
{
    [SerializeField] Transform target;

    // private InputActions inputActions;

    // private void OnEnable()
    // {
    //     inputActions = new InputActions();
    //     inputActions.Enable();
    // }

    // private void OnDisable()
    // {
    //     inputActions.Disable();
    // }

    public override void OnEpisodeBegin() {
        transform.position = new Vector3(Random.Range(0.5f, 2f), 0f, Random.Range(-2f, 2f));
        target.position = new Vector3(Random.Range(-0.5f, -2f), 0f, Random.Range(-2f, 2f));
    }

    public override void CollectObservations(VectorSensor sensor) {
        sensor.AddObservation((Vector3)transform.position);
        sensor.AddObservation((Vector3)target.position);
    }

    public override void OnActionReceived(ActionBuffers actions) {

        float moveX = actions.ContinuousActions[0];
        float moveZ = actions.ContinuousActions[1];

        float movementSpeed = 3f;

        transform.localPosition += new Vector3(moveX, 0, moveZ) * Time.deltaTime * movementSpeed;
    }

    private void OnTriggerEnter(Collider collider) {
        if (collider.TryGetComponent(out Target target)) {
            AddReward(10f);
            EndEpisode();
        } else if (collider.TryGetComponent(out Wall wall)) {
            AddReward(-2f);
            EndEpisode();
        }
    }
    
    public override void Heuristic(in ActionBuffers actionsOut) {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxisRaw("Horizontal");
        continuousActions[1] = Input.GetAxisRaw("Vertical");
    }
}
