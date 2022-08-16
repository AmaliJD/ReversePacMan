using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Ghost : MonoBehaviour
{
    public enum Mode { home, chase, frightented, scatter, eaten};
    public Mode mode;
    private Mode prevMode;

    /*public enum ChaseMode { direct, eastsidenegative};
    public ChaseMode chasemode;*/

    [Min(0)]
    public int lookAhead;

    [Min(0)]
    public int range;

    public Transform eastSideGhost;
    public Transform scatterTarget;

    private bool disable;
    private DisableDirections disabler;

    [HideInInspector]
    public bool stop;

    private Vector2 moveDirection = Vector2.zero;
    public LayerMask mapLayer;
    public Transform target;
    public Color color;
    public float homeTimer;
    private float initHomeTimer;
    public Transform exitPos;
    public AudioSource eatenSfx;
    private Vector2 targetPos;
    private Vector2 startPos;
    private SpriteRenderer outline, inner;
    private GameObject outlineObj, eye, pupil, eyeFrightened;

    public float speed;
    private float initSpeed;

    private float[] distances;
    //private Tilemap map;
    //private Transform pacman;
    private Rigidbody2D rb;
    private Collider2D col;

    private void Awake()
    {
        distances = new float[4];
        //pacman = GameObject.FindGameObjectWithTag("PacMan").GetComponent<Transform>();
        //map = GameObject.FindGameObjectWithTag("Map").GetComponent<Tilemap>();
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CircleCollider2D>();
        targetPos = target.position;
        //mapLayer = LayerMask.NameToLayer("Map");
        initSpeed = speed;
        startPos = transform.position;
        outlineObj = transform.GetChild(0).gameObject;
        eye = transform.GetChild(1).gameObject;
        pupil = eye.transform.GetChild(0).gameObject;
        eyeFrightened = transform.GetChild(2).gameObject;
        outline = outlineObj.GetComponent<SpriteRenderer>();
        inner = outlineObj.transform.GetChild(0).GetComponent<SpriteRenderer>();
        initHomeTimer = homeTimer;
    }

    private void FixedUpdate()
    {
        if (stop) { return; }
        MoveToTarget();
    }

    void MoveToTarget()
    {
        //Vector2Int roundedPos = new Vector2Int(Mathf.RoundToInt(rb.position.x), Mathf.RoundToInt(rb.position.y));
        //Vector2 roundedPosx = new Vector2(Mathf.RoundToInt(rb.position.x), rb.position.y);
        //Vector2 roundedPosY = new Vector2(rb.position.x, Mathf.RoundToInt(rb.position.y));

        if (prevMode != mode && mode != Mode.eaten) { moveDirection = -moveDirection; }
        switch (mode)
        {
            case Mode.chase:
                targetPos = target.position;
                if (lookAhead > 0) { targetPos += ((Vector2)target.right * lookAhead); }
                if (eastSideGhost != null) { targetPos -= ((Vector2)eastSideGhost.position - targetPos); }
                if (range > 0 && Vector2.Distance(transform.position, targetPos) < range) { targetPos = scatterTarget.position; }

                outline.color = Color.white;
                inner.color = color;
                eye.SetActive(true);
                eyeFrightened.SetActive(false);
                pupil.transform.localPosition = moveDirection * .08f;
                speed = initSpeed;
                break;

            case Mode.scatter:
                targetPos = scatterTarget.position;

                outline.color = Color.white;
                inner.color = color;
                eye.SetActive(true);
                eyeFrightened.SetActive(false);
                pupil.transform.localPosition = moveDirection * .08f;
                speed = initSpeed;
                break;

            case Mode.frightented:
                targetPos = (Vector2)transform.position + new Vector2(Random.Range(-1, 2), Random.Range(-1, 2));

                outline.color = new Color(1,1,1,.5f);
                inner.color = Color.blue;
                eye.SetActive(false);
                eyeFrightened.SetActive(true);
                pupil.transform.localPosition = Vector2.zero;
                speed = initSpeed / 2;
                break;

            case Mode.eaten:
                targetPos = exitPos.position;

                outline.color = Color.clear;
                inner.color = Color.clear;
                eye.SetActive(true);
                eyeFrightened.SetActive(false);
                pupil.transform.localPosition = Vector2.zero;
                speed = initSpeed * 2f;

                if(Vector2.Distance(rb.position, exitPos.position) < .1f)
                {
                    mode = Mode.home;
                    homeTimer = 1f;
                }
                break;

            case Mode.home:
                if(homeTimer > 0)
                {
                    col.isTrigger = true;
                    rb.position = Vector2.MoveTowards(rb.position, startPos, 10 * Time.deltaTime);
                    homeTimer -= Time.fixedDeltaTime;
                }
                else if(Vector2.Distance(rb.position, exitPos.position) > .05f)
                {
                    rb.position = Vector2.MoveTowards(rb.position, exitPos.position, 5 * Time.deltaTime);
                }
                else
                {
                    col.isTrigger = false;
                    mode = Mode.chase;
                }

                outline.color = Color.white;
                inner.color = color;
                eye.SetActive(true);
                eyeFrightened.SetActive(false);
                pupil.transform.localPosition = Vector2.zero;

                break;
        }

        if (mode != Mode.home)
        {
            col.isTrigger = false;

            distances[0] = Vector2.Distance(targetPos, rb.position + new Vector2(0, 1));
            distances[1] = Vector2.Distance(targetPos, rb.position + new Vector2(-1, 0));
            distances[2] = Vector2.Distance(targetPos, rb.position + new Vector2(0, -1));
            distances[3] = Vector2.Distance(targetPos, rb.position + new Vector2(1, 0));

            if (Physics2D.BoxCast(rb.position, Vector2.one * .85f, 0, Vector2.up, .1f, mapLayer) || Vector2.up == -moveDirection || (disable && disabler.up)) { distances[0] = float.MaxValue; }
            if (Physics2D.BoxCast(rb.position, Vector2.one * .85f, 0, Vector2.left, .1f, mapLayer) || Vector2.left == -moveDirection || (disable && disabler.left)) { distances[1] = float.MaxValue; }
            if (Physics2D.BoxCast(rb.position, Vector2.one * .85f, 0, Vector2.down, .1f, mapLayer) || Vector2.down == -moveDirection || (disable && disabler.down)) { distances[2] = float.MaxValue; }
            if (Physics2D.BoxCast(rb.position, Vector2.one * .85f, 0, Vector2.right, .1f, mapLayer) || Vector2.right == -moveDirection || (disable && disabler.right)) { distances[3] = float.MaxValue; }

            float minDistance = Mathf.Min(Mathf.Min(Mathf.Min(distances[0], distances[1]), distances[2]), distances[3]);
            if (minDistance == distances[0]) { moveDirection = Vector2.up; }
            else if (minDistance == distances[1]) { moveDirection = Vector2.left; }
            else if (minDistance == distances[2]) { moveDirection = Vector2.down; }
            else if (minDistance == distances[3]) { moveDirection = Vector2.right; }

            if (moveDirection.x == 0) { rb.position = new Vector2(Mathf.RoundToInt(rb.position.x), rb.position.y); }
            else if (moveDirection.y == 0) { rb.position = new Vector2(rb.position.x, Mathf.RoundToInt(rb.position.y)); }

            rb.MovePosition(Vector2.MoveTowards(rb.position, rb.position + moveDirection, speed * Time.fixedDeltaTime));

            prevMode = mode;
        }
    }

    public Vector2Int[] nextSteps(int steps)
    {
        Vector2Int[] path = new Vector2Int[steps];

        //Vector2 tempTarget = (mode == Mode.frightented ? (Vector2)target.position : targetPos);
        Vector2 tempMoveDirection = moveDirection;// (prevMode != Mode.frightented && mode == Mode.frightented) ? -moveDirection : moveDirection;
        float[] tempDistances = new float[4];
        Vector2Int roundedPos = new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y));
        path[0] = roundedPos;

        for (int i = 1; i < steps; i++)
        {
            roundedPos.x += (int)tempMoveDirection.x;
            roundedPos.y += (int)tempMoveDirection.y;

            path[i] = roundedPos;

            tempDistances[0] = Vector2.Distance(targetPos, roundedPos + Vector2.up);
            tempDistances[1] = Vector2.Distance(targetPos, roundedPos + Vector2.left);
            tempDistances[2] = Vector2.Distance(targetPos, roundedPos + Vector2.down);
            tempDistances[3] = Vector2.Distance(targetPos, roundedPos + Vector2.right);

            if (Physics2D.BoxCast(roundedPos, Vector2.one * .9f, 0, Vector2.up, .06f, mapLayer) || Vector2.up == -tempMoveDirection) { tempDistances[0] = float.MaxValue; }
            if (Physics2D.BoxCast(roundedPos, Vector2.one * .9f, 0, Vector2.left, .06f, mapLayer) || Vector2.left == -tempMoveDirection) { tempDistances[1] = float.MaxValue; }
            if (Physics2D.BoxCast(roundedPos, Vector2.one * .9f, 0, Vector2.down, .06f, mapLayer) || Vector2.down == -tempMoveDirection) { tempDistances[2] = float.MaxValue; }
            if (Physics2D.BoxCast(roundedPos, Vector2.one * .9f, 0, Vector2.right, .06f, mapLayer) || Vector2.right == -tempMoveDirection) { tempDistances[3] = float.MaxValue; }

            float minDistance = Mathf.Min(Mathf.Min(Mathf.Min(tempDistances[0], tempDistances[1]), tempDistances[2]), tempDistances[3]);
            if (minDistance == tempDistances[0]) { tempMoveDirection = Vector2.up; }
            else if (minDistance == tempDistances[1]) { tempMoveDirection = Vector2.left; }
            else if (minDistance == tempDistances[2]) { tempMoveDirection = Vector2.down; }
            else if (minDistance == tempDistances[3]) { tempMoveDirection = Vector2.right; }
        }

        return path;
    }

    public void reset()
    {
        transform.position = startPos;
        mode = Mode.home;
        homeTimer = initHomeTimer;
        moveDirection = Vector2.zero;
        stop = false;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.tag == "Disable")
        {
            disable = true;
            disabler = collision.GetComponent<DisableDirections>();
        }  
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if(collision.tag == "Disable") { disable = false; }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Teleport")
        {
            transform.position = collision.transform.GetChild(0).position;
        }
        if (collision.tag == "PacMan" && mode == Mode.frightented)
        {
            eatenSfx.PlayOneShot(eatenSfx.clip, .3f);
            mode = Mode.eaten;
        }
    }

    public Vector2 getTargetPosition()
    {
        return targetPos;
    }

    public IEnumerator SetScatter(float time)
    {
        if(mode == Mode.chase){ mode = Mode.scatter; }
        yield return new WaitForSeconds(time);
        if (mode == Mode.scatter) { mode = Mode.chase; }
    }

    public void ReverseDirection()
    {
        moveDirection = -moveDirection;
    }

    public IEnumerator SetFrightened(float time)
    {
        if (mode == Mode.chase || mode == Mode.scatter) { mode = Mode.frightented; }
        yield return new WaitForSeconds(time);
        if (mode == Mode.frightented) { mode = Mode.chase; }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = color;
        Gizmos.DrawLine(transform.position, targetPos);
        Gizmos.DrawSphere(targetPos, .3f);

        Vector2Int[] path = nextSteps(10);
        foreach (Vector2 v in path)
        {
            Gizmos.DrawCube(v, Vector3.one * .5f);
        }
    }
#endif
}
