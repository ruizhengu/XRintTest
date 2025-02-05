using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class SceneExplore : MonoBehaviour
{
    protected float moveStep = 1f;
    protected Vector3 moveUpperBound = new Vector3(7f, 4.4f, 11f);
    protected Vector3 moveLowerBound = new Vector3(-14f, 4.3f, -1f);
    protected Vector3[] moveOpts = new Vector3[6];
    protected List<Vector3> moves = new List<Vector3>();
    Vector3 destPos;
    bool move;
    void Start()
    {
        move = true;
        moveOpts[0] = new Vector3(1f, 0f, 0f);
        moveOpts[1] = new Vector3(-1f, 0f, 0f);
        moveOpts[2] = new Vector3(0f, 1f, 0f);
        moveOpts[3] = new Vector3(0f, -1f, 0f);
        moveOpts[4] = new Vector3(0f, 0f, 1f);
        moveOpts[5] = new Vector3(0f, 0f, -1f);

        destPos = transform.position;

    }

    void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, destPos, moveStep * 0.02f);
        if (transform.position == destPos)
        {
            destPos = Move();
        }
    }

    public void Moving()
    {
        transform.position = Vector3.MoveTowards(transform.position, destPos, moveStep * 0.02f);
        if (transform.position == destPos)
        {
            destPos = Move();
        }
    }

    public bool Movable(Vector3 position, int flag)
    {
        switch (flag)
        {
            case 0: return position.x + moveStep < moveUpperBound.x && !Physics.Raycast(position, Vector3.right, moveStep);
            case 1: return position.x - moveStep > moveLowerBound.x && !Physics.Raycast(position, Vector3.left, moveStep);
            case 2: return position.y + moveStep < moveUpperBound.y && !Physics.Raycast(position, Vector3.up, moveStep);
            case 3: return position.y - moveStep > moveLowerBound.y && !Physics.Raycast(position, Vector3.down, moveStep);
            case 4: return position.z + moveStep < moveUpperBound.z && !Physics.Raycast(position, Vector3.forward, moveStep);
            case 5: return position.z - moveStep > moveLowerBound.z && !Physics.Raycast(position, Vector3.back, moveStep);
            default: return false;
        }
    }

    public virtual Vector3 Move()
    {
        if (move)
        {
            UpdateMoves();
            System.Random rnd = new System.Random();
            int n = rnd.Next(0, moves.Count);
            Debug.Log(n);
            Debug.Log(moves);
            return transform.position + moves[n] * moveStep;
        }
        else
        {
            return transform.position;
        }
    }

    public void UpdateMoves()
    {
        moves.Clear();
        Vector3 pos = transform.position;
        for (int i = 0; i < 6; i++)
        {
            if (Movable(pos, i))
            {
                moves.Add(moveOpts[i]);
            }
        }
    }
}
