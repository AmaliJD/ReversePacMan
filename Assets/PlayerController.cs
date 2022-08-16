using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    public enum Mode { chase, eaten, frightented};
    public Mode mode;
    private Mode prevMode;

    public LayerMask mapLayer;
    public Color color;
    public Transform target;
    private Vector2 targetPos;
    private Vector2 inputDirection, moveDirection;
    private Vector2 prevPosition;
    private bool moving = false;

    public int lives;

    public float speed;
    private float initSpeed;
    private Vector2 initPos;

    private Rigidbody2D rb;
    private bool dead;

    [HideInInspector]
    public bool stop;
    private GameManager gamemanager;

    private SpriteRenderer outline, inner;
    private GameObject outlineObj, eye, pupil, eyeFrightened;

    public AudioSource eatenSfx, deathSfx;
    public Text livesText;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        initSpeed = speed;

        outlineObj = transform.GetChild(0).gameObject;
        eye = transform.GetChild(1).gameObject;
        pupil = eye.transform.GetChild(0).gameObject;
        eyeFrightened = transform.GetChild(2).gameObject;
        outline = outlineObj.GetComponent<SpriteRenderer>();
        inner = outlineObj.transform.GetChild(0).GetComponent<SpriteRenderer>();

        targetPos = target.position;

        prevMode = mode;
        moveDirection = Vector3.up;
        inputDirection = moveDirection;
        prevPosition = rb.position;

        initPos = transform.position;

        gamemanager = GameObject.FindGameObjectWithTag("Main").GetComponent<GameManager>();
    }

    private void Update()
    {
        livesText.text = "x" + lives;
        /*if(Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W) && moveDirection != Vector2.down)
        {
            inputDirection = Vector2.up;
        }
        else if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A) && moveDirection != Vector2.right)
        {
            inputDirection = Vector2.left;
        }
        else if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A) && moveDirection != Vector2.up)
        {
            inputDirection = Vector2.down;
        }
        else if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A) && moveDirection != Vector2.left)
        {
            inputDirection = Vector2.right;
        }*/

        if (Input.GetAxis("Vertical") > 0 && moveDirection != Vector2.down)
        {
            inputDirection = Vector2.up;
        }
        else if (Input.GetAxis("Horizontal") < 0 && moveDirection != Vector2.right)
        {
            inputDirection = Vector2.left;
        }
        else if (Input.GetAxis("Vertical") < 0 && moveDirection != Vector2.up)
        {
            inputDirection = Vector2.down;
        }
        else if (Input.GetAxis("Horizontal") > 0 && moveDirection != Vector2.left)
        {
            inputDirection = Vector2.right;
        }

        if (Input.GetAxis("Vertical") == 0 && Input.GetAxis("Horizontal") == 0)
        {
            inputDirection = moveDirection;
        }

        //if (prevMode != mode) { inputDirection = -moveDirection; }
        switch (mode)
        {
            case Mode.chase:
                targetPos = target.position;

                outline.color = Color.red;
                inner.color = color;
                eye.SetActive(true);
                eyeFrightened.SetActive(false);
                pupil.transform.localPosition = moveDirection * .08f;
                speed = initSpeed;
                break;

            case Mode.frightented:
                targetPos = target.position;

                outline.color = Color.white;
                inner.color = Color.blue;
                eye.SetActive(false);
                eyeFrightened.SetActive(true);
                pupil.transform.localPosition = Vector2.zero;
                speed = initSpeed / 2;
                break;

            case Mode.eaten:
                targetPos = initPos;

                outline.color = Color.clear;
                inner.color = Color.clear;
                eye.SetActive(true);
                eyeFrightened.SetActive(false);
                pupil.transform.localPosition = Vector2.zero;
                speed = initSpeed * 2f;

                if (Vector2.Distance(rb.position, initPos) < .2f)
                {
                    mode = Mode.chase;
                    gamemanager.sirenIndex = Mathf.Clamp(gamemanager.sirenIndex + 1, 0, 4);
                }
                break;
        }

        prevMode = mode;
        moving = rb.position != prevPosition;
        //Debug.Log(moving);
        //Debug.Log(inputDirection + "   " + moveDirection);
    }

    private void FixedUpdate()
    {
        if (dead) { return; }
        if (stop) { return; }

        if (mode != Mode.eaten)
        {
            if (!Physics2D.BoxCast(rb.position, Vector2.one * .85f, 0, inputDirection, .1f, mapLayer))
            {
                moveDirection = inputDirection;
            }
        }
        else
        {
            float[] distances = new float[4];
            distances[0] = Vector2.Distance(targetPos, rb.position + new Vector2(0, 1));
            distances[1] = Vector2.Distance(targetPos, rb.position + new Vector2(-1, 0));
            distances[2] = Vector2.Distance(targetPos, rb.position + new Vector2(0, -1));
            distances[3] = Vector2.Distance(targetPos, rb.position + new Vector2(1, 0));

            if (Physics2D.BoxCast(rb.position, Vector2.one * .9f, 0, Vector2.up, .06f, mapLayer) || Vector2.up == -moveDirection) { distances[0] = float.MaxValue; }
            if (Physics2D.BoxCast(rb.position, Vector2.one * .9f, 0, Vector2.left, .06f, mapLayer) || Vector2.left == -moveDirection) { distances[1] = float.MaxValue; }
            if (Physics2D.BoxCast(rb.position, Vector2.one * .9f, 0, Vector2.down, .06f, mapLayer) || Vector2.down == -moveDirection) { distances[2] = float.MaxValue; }
            if (Physics2D.BoxCast(rb.position, Vector2.one * .9f, 0, Vector2.right, .06f, mapLayer) || Vector2.right == -moveDirection) { distances[3] = float.MaxValue; }

            float minDistance = Mathf.Min(Mathf.Min(Mathf.Min(distances[0], distances[1]), distances[2]), distances[3]);
            if (minDistance == distances[0]) { moveDirection = Vector2.up; }
            else if (minDistance == distances[1]) { moveDirection = Vector2.left; }
            else if (minDistance == distances[2]) { moveDirection = Vector2.down; }
            else if (minDistance == distances[3]) { moveDirection = Vector2.right; }
        }

        if (moveDirection.x == 0) { rb.position = new Vector2(Mathf.RoundToInt(rb.position.x), rb.position.y); }
        else if (moveDirection.y == 0) { rb.position = new Vector2(rb.position.x, Mathf.RoundToInt(rb.position.y)); }

        prevPosition = rb.position;
        rb.MovePosition(Vector2.MoveTowards(rb.position, rb.position + moveDirection, speed * Time.fixedDeltaTime));
    }

    public Vector2Int[] nextSteps(int steps)
    {
        Vector2Int[] path = new Vector2Int[steps];

        //Vector2 tempTarget = (mode == Mode.frightented ? (Vector2)target.position : targetPos);
        Vector2 tempMoveDirection = moveDirection;// (prevMode != Mode.frightented && mode == Mode.frightented) ? -moveDirection : moveDirection;
        Vector2Int roundedPos = new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y));

        if (!moving)
        {
            float[] distances = new float[4];
            distances[0] = Vector2.Distance(targetPos, roundedPos + new Vector2(0, 1));
            distances[1] = Vector2.Distance(targetPos, roundedPos + new Vector2(-1, 0));
            distances[2] = Vector2.Distance(targetPos, roundedPos + new Vector2(0, -1));
            distances[3] = Vector2.Distance(targetPos, roundedPos + new Vector2(1, 0));

            if (Physics2D.BoxCast(roundedPos, Vector2.one * .9f, 0, Vector2.up, .06f, mapLayer) || Vector2.up == -tempMoveDirection) { distances[0] = float.MaxValue; }
            if (Physics2D.BoxCast(roundedPos, Vector2.one * .9f, 0, Vector2.left, .06f, mapLayer) || Vector2.left == -tempMoveDirection) { distances[1] = float.MaxValue; }
            if (Physics2D.BoxCast(roundedPos, Vector2.one * .9f, 0, Vector2.down, .06f, mapLayer) || Vector2.down == -tempMoveDirection) { distances[2] = float.MaxValue; }
            if (Physics2D.BoxCast(roundedPos, Vector2.one * .9f, 0, Vector2.right, .06f, mapLayer) || Vector2.right == -tempMoveDirection) { distances[3] = float.MaxValue; }

            float minDistance = Mathf.Min(Mathf.Min(Mathf.Min(distances[0], distances[1]), distances[2]), distances[3]);
            if (minDistance == distances[0]) { tempMoveDirection = Vector2.up; }
            else if (minDistance == distances[1]) { tempMoveDirection = Vector2.left; }
            else if (minDistance == distances[2]) { tempMoveDirection = Vector2.down; }
            else if (minDistance == distances[3]) { tempMoveDirection = Vector2.right; }
        }

        float[] tempDistances = new float[4];
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
        transform.position = initPos;
        mode = Mode.chase;
        moveDirection = Vector2.up;
        stop = false;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Teleport")
        {
            transform.position = collision.transform.GetChild(0).position;
        }
        if (collision.tag == "PacMan")
        {
            if(mode == Mode.frightented)
            {
                lives--;

                if(lives <= 0)
                {
                    deathSfx.PlayOneShot(deathSfx.clip, .3f);
                    dead = true;
                    gamemanager.Lose(0);
                }
                else
                {
                    eatenSfx.PlayOneShot(eatenSfx.clip, .3f);
                    mode = Mode.eaten;
                }
            }
            else if (mode == Mode.chase)
            {
                //gamemanager.Win();
            }
        }
    }

    public IEnumerator SetFrightened(float time)
    {
        if (mode == Mode.chase) { mode = Mode.frightented; }
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
