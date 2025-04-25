using UnityEngine;

public class ControllerAction : MonoBehaviour
{
    [SerializeField] private float moveRange = 5f; // Range of movement in each direction
    [SerializeField] private float moveSpeed = 2f; // Speed of movement

    private Vector3 targetPosition;
    private float nextMoveTime;

    private void Start()
    {
        // Set initial target position
        SetNewTargetPosition();
    }

    private void Update()
    {
        // Move towards target position
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        // If we've reached the target position or it's time to change direction
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f || Time.time >= nextMoveTime)
        {
            SetNewTargetPosition();
        }
    }

    private void SetNewTargetPosition()
    {
        // Generate random position within range
        float randomX = Random.Range(-moveRange, moveRange);
        float randomY = Random.Range(-moveRange, moveRange);
        float randomZ = Random.Range(-moveRange, moveRange);

        targetPosition = new Vector3(randomX, randomY, randomZ);
        nextMoveTime = Time.time + Random.Range(2f, 5f); // Change direction every 2-5 seconds
    }
}
