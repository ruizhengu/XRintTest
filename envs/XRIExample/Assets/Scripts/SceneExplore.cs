using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class SceneExplore
{
    private float moveStep = 1f;
    private Vector3 botPos;
    private Vector3 destPos;
    private Vector3 moveUpperBound = new Vector3(7f, 4.4f, 11f);
    private Vector3 moveLowerBound = new Vector3(-14f, 4.3f, -1f);

    private Vector3[] directions = {
        new Vector3(1f, 0f, 0f),
        new Vector3(-1f, 0f, 0f),
        new Vector3(0f, 1f, 0f),
        new Vector3(0f, -1f, 0f),
        new Vector3(0f, 0f, 1f),
        new Vector3(0f, 0f, -1f)
    };

    public SceneExplore(Vector3 initPos)
    {
        botPos = initPos;
        destPos = initPos;
    }

    public Vector3 RandomExploration()
    {
        botPos = Vector3.MoveTowards(
            botPos,
            destPos,
            moveStep * Time.deltaTime
        );
        // Update destination when reaching target
        if (botPos == destPos)
        {
            destPos = GetNewDestination();
        }
        return botPos;
    }

    private bool IsMoveValid(Vector3 position, Vector3 direction)
    {
        return (direction.x == 0 || position.x + direction.x * moveStep >= moveLowerBound.x && position.x + direction.x * moveStep <= moveUpperBound.x) &&
               (direction.y == 0 || position.y + direction.y * moveStep >= moveLowerBound.y && position.y + direction.y * moveStep <= moveUpperBound.y) &&
               (direction.z == 0 || position.z + direction.z * moveStep >= moveLowerBound.z && position.z + direction.z * moveStep <= moveUpperBound.z) &&
               !Physics.Raycast(position, direction, moveStep);
    }

    private Vector3 GetNewDestination()
    {
        var validMoves = new List<Vector3>();
        for (int i = 0; i < directions.Length; i++)
        {
            if (IsMoveValid(destPos, directions[i]))
            {
                validMoves.Add(directions[i]);
            }
        }
        if (validMoves.Count > 0)
        {
            return destPos + validMoves[Random.Range(0, validMoves.Count)] * moveStep;
        }
        return destPos;
    }
}


