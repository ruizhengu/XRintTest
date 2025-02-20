using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
public class SceneExplore
{
    private float moveStep = 1f;
    private Vector3 botPos;
    private Vector3 destPos;
    private Vector3 moveUpperBound = new Vector3(7f, 4.4f, 11f);
    private Vector3 moveLowerBound = new Vector3(-14f, 4.3f, -1f);
    public InteractableIdentification interactableIdentification;
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
        interactableIdentification = new InteractableIdentification();

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

    public Vector3 GreedyExploration(GameObject go)
    {
        destPos = go.transform.position;
        Debug.Log(go.name + ": " + destPos);
        botPos = Vector3.MoveTowards(
            botPos,
            destPos,
            moveStep * Time.deltaTime
        );
        // Update destination when reaching target
        // if (botPos == destPos)
        // {
        //     destPos = GetGODestination(go);
        // }
        return botPos;
    }

    public GameObject getCloestInteractable()
    {
        GameObject closest = null;
        float minDistance = Mathf.Infinity;
        foreach (KeyValuePair<GameObject, InteractableIdentification.InteractableInfo> entry in interactableIdentification.getInteractables())
        {
            var interactableInfo = entry.Value;
            if (!interactableInfo.getInteractFlag())
            {
                GameObject obj = interactableInfo.getObject();
                float distance = Vector3.Distance(botPos, obj.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closest = obj;
                }
            }
        }
        return closest;
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

    private Vector3 GetGODestination(GameObject go)
    {
        Vector3 targetPos = go.transform.position;
        var validMoves = new List<Vector3>();
        for (int i = 0; i < directions.Length; i++)
        {
            if (IsMoveValid(destPos, directions[i]))
            {
                validMoves.Add(directions[i]);
            }
        }
        Debug.Log("validMoves: " + validMoves);
        if (validMoves.Count > 0)
        {
            Vector3 bestMove = Vector3.zero;
            float bestDistance = Mathf.Infinity;

            foreach (Vector3 move in validMoves)
            {
                Vector3 moveCandidate = destPos + move * moveStep;
                float distance = Vector3.Distance(moveCandidate, targetPos);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestMove = move;
                }
            }
            if (bestMove != Vector3.zero)
            {
                Debug.Log("bestMove: " + destPos + bestMove * moveStep);
                return destPos + bestMove * moveStep;
            }
        }

        return destPos;
    }
}


