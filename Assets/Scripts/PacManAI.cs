using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PacManAI : MonoBehaviour
{
    public LayerMask mapLayer;
    public bool scatterGhosts;
    public Transform lowerLeft, upperRight;
    public Tilemap map, pelletMap, powerPelletMap, oobMap, nodeMap;
    public Transform ghostsParent;
    private List<Ghost> ghosts;
    private PlayerController player;
    private List<Vector2> nodePos;
    private TextMesh[] weightTexts;
    private Transform weightTextParent;

    private Animator pacmanAnim;
    public AudioSource pelletSfx1, pelletSfx2, deathSfx;
    bool pelletNum1 = true;

    public float speed;
    public int lives;
    private int totalPellets;
    private Vector2 initPos;
    private bool dead;
    
    [HideInInspector]
    public bool stop;

    private Rigidbody2D rb;
    private float[,] baseGrid, alterGrid;
    private int steps = 10;
    private float[] weights;
    private Vector2 moveDirection, prevDirection, prevNodeDirection;
    //private int changeDirectionCounter = 0;
    private float updateGridTimer;
    private float noPelletTimer;
    //private float chooseNodeTimer;
    private Vector2 chosenNode;
    private float previousBest = 0;
    private bool toNode;
    private Vector2[] directionTracker;
    private GameManager gamemanager;

    public Text livesText;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        pacmanAnim = transform.GetChild(0).GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
        ghosts = new List<Ghost>();
        nodePos = new List<Vector2>();
        initPos = transform.position;
        //ghostsPos = new List<Vector2>();
        //targets = new List<Vector2>();
        foreach (Transform t in ghostsParent)
        {
            if(t.gameObject.activeSelf)
            {
                ghosts.Add(t.GetComponent<Ghost>());
            }
        }
        /*foreach (Transform t in nodeMap.transform)
        {
            nodePos.Add(t.transform.position);
        }*/
#if UNITY_EDITOR
        weightTexts = new TextMesh[4];
        weightTextParent = transform.GetChild(transform.childCount - 1);
        weightTextParent.gameObject.SetActive(true);
        weightTexts[0] = weightTextParent.GetChild(0).GetComponent<TextMesh>();
        weightTexts[1] = weightTextParent.GetChild(1).GetComponent<TextMesh>();
        weightTexts[2] = weightTextParent.GetChild(2).GetComponent<TextMesh>();
        weightTexts[3] = weightTextParent.GetChild(3).GetComponent<TextMesh>();
#endif

        gamemanager = GameObject.FindGameObjectWithTag("Main").GetComponent<GameManager>();

        updateGridTimer = -.2f;
        //moveDirection = Vector2.zero;
        int rev = Random.Range(0, 2) == 0 ? -1 : 1;
        moveDirection = new Vector2(rev * 1, 0);

        directionTracker = new Vector2[2];
        weights = new float[4];
        baseGrid = new float[(int)upperRight.position.x - (int)lowerLeft.position.x, (int)upperRight.position.y - (int)lowerLeft.position.y];
        PopulateGrid();
        //Debug.Log(baseGrid.GetLength(0) + "  " + baseGrid.GetLength(1));
    }

    void PopulateGrid()
    {
        for(int i = 0; i < baseGrid.GetLength(0); i++)
        {
            for (int j = 0; j < baseGrid.GetLength(1); j++)
            {
                Vector2 worldPos = ConvertGridToWorldCoords(new Vector2(i, j));
                if (map.HasTile(map.WorldToCell(worldPos)) || oobMap.HasTile(oobMap.WorldToCell(worldPos))) { baseGrid[i, j] = -1; }
                else if (pelletMap.HasTile(pelletMap.WorldToCell(worldPos))) { baseGrid[i, j] = 60; }
                else if (powerPelletMap.HasTile(pelletMap.WorldToCell(worldPos))) { baseGrid[i, j] = 100; }
                else { baseGrid[i, j] = 40; }
            }
        }
    }

    Vector2Int ConvertWorldToGridCoords(Vector2 position)
    {
        Vector2Int roundedPosition = new Vector2Int(Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y));
        return new Vector2Int(roundedPosition.x - (int)lowerLeft.position.x, roundedPosition.y - (int)lowerLeft.position.y);
    }

    Vector2 ConvertGridToWorldCoords(Vector2 position)
    {
        return new Vector2((int)position.x + (int)lowerLeft.position.x, (int)position.y + (int)lowerLeft.position.y);
    }

    private void Update()
    {
        livesText.text = "x" + lives;

        if (stop) { return; }
        if (dead) { return; }
        if (totalPellets <= 0 && gotPelletCount)
        {
            gamemanager.Lose(1);
        }

#if UNITY_EDITOR
        if(weightTextParent.rotation != Quaternion.identity)
        {
            weightTextParent.gameObject.SetActive(false);
        }
#endif
    }

    bool gotPelletCount;

    private void FixedUpdate()
    {
        if (stop) { return; }
        if (dead) { return; }

        if(nodePos.Count == 0)
        {
            //Debug.Log("Count: " + nodeMap.transform.childCount);
            foreach (Transform t in nodeMap.transform)
            {
                nodePos.Add(t.transform.position);
            }
        }
        alterGrid = (float[,])baseGrid.Clone();

        Vector2Int selfCoord = ConvertWorldToGridCoords(transform.position);

        foreach (Ghost g in ghosts)
        {
            Vector2Int[] path = g.nextSteps(/*(g.mode == Ghost.Mode.frightented ? 2 : 1) * */steps, g.mode == Ghost.Mode.frightented, g.mode == Ghost.Mode.frightented);
            int i = 1;
            bool reached = false;
            foreach (Vector2 v in path)
            {
                Vector2Int coord = ConvertWorldToGridCoords(v);
                
                if(coord.x >= 0 && coord.x < alterGrid.GetLength(0) && coord.y >= 0 && coord.y < alterGrid.GetLength(1))
                {
                    switch(g.mode)
                    {
                        case Ghost.Mode.chase:
                        case Ghost.Mode.scatter:
                            alterGrid[coord.x, coord.y] *= Mathf.Pow((float)i / (float)steps, reached ? 1 : 2);
                            break;
                        case Ghost.Mode.frightented:
                            alterGrid[coord.x, coord.y] /= ((float)i / (float)steps);
                            break;
                        case Ghost.Mode.home:
                        case Ghost.Mode.eaten:
                            break;
                    }
                    
                }
                if (selfCoord == coord) { reached = true; }
                //if (reached) { break; }

                i++;
            }
        }

        Vector2Int[] playerPath = player.nextSteps(/*(player.mode == PlayerController.Mode.frightented ? 2 : 1) * */steps, player.mode == PlayerController.Mode.frightented, player.mode == PlayerController.Mode.frightented);
        int j = 1;
        bool playerReached = false;
        foreach (Vector2 v in playerPath)
        {
            Vector2Int coord = ConvertWorldToGridCoords(v);
            
            if (coord.x >= 0 && coord.x < alterGrid.GetLength(0) && coord.y >= 0 && coord.y < alterGrid.GetLength(1))
            {
                switch (player.mode)
                {
                    case PlayerController.Mode.chase:
                        alterGrid[coord.x, coord.y] *= Mathf.Pow((float)j / (float)steps, playerReached ? 1 : 2);
                        break;
                    case PlayerController.Mode.frightented:
                        alterGrid[coord.x, coord.y] /= ((float)j / (float)steps);
                        break;
                    case PlayerController.Mode.eaten:
                        break;
                }
                if (selfCoord == coord) { playerReached = true; }
                //if (playerReached) { break; }
            }

            j++;
        }

        /*float currWeightBase = baseGrid[ConvertWorldToGridCoords((Vector2)transform.position).x, ConvertWorldToGridCoords((Vector2)transform.position).y];
        if (currWeightBase > 20 && currWeightBase <= 40) { baseGrid[ConvertWorldToGridCoords((Vector2)transform.position).x, ConvertWorldToGridCoords((Vector2)transform.position).y] -= 5f; }*/
        
        if (!gotPelletCount && GameObject.FindGameObjectsWithTag("Pellet").Length + GameObject.FindGameObjectsWithTag("Power Pellet").Length != 0)
        {
            totalPellets += GameObject.FindGameObjectsWithTag("Pellet").Length + GameObject.FindGameObjectsWithTag("Power Pellet").Length;
            gotPelletCount = true;
        }

        if (updateGridTimer >= .15f || alterGrid[ConvertWorldToGridCoords((Vector2)transform.position + moveDirection).x, ConvertWorldToGridCoords((Vector2)transform.position + moveDirection).y] == -1) { updateGridTimer = 0; }
        if (updateGridTimer == 0)
        {
            weights[0] = alterGrid[ConvertWorldToGridCoords((Vector2)transform.position + Vector2.up).x, ConvertWorldToGridCoords((Vector2)transform.position + Vector2.up).y];
            weights[1] = alterGrid[ConvertWorldToGridCoords((Vector2)transform.position + Vector2.left).x, ConvertWorldToGridCoords((Vector2)transform.position + Vector2.left).y];
            weights[2] = alterGrid[ConvertWorldToGridCoords((Vector2)transform.position + Vector2.down).x, ConvertWorldToGridCoords((Vector2)transform.position + Vector2.down).y];
            weights[3] = alterGrid[ConvertWorldToGridCoords((Vector2)transform.position + Vector2.right).x, ConvertWorldToGridCoords((Vector2)transform.position + Vector2.right).y];

            float maxWeight = Mathf.Max(Mathf.Max(Mathf.Max(weights[0], weights[1]), weights[2]), weights[3]);
            float minWeight = Mathf.Min(Mathf.Min(Mathf.Min(weights[0] >= 0 ? weights[0] : 20, weights[1] >= 0 ? weights[1] : 20), weights[2] >= 0 ? weights[2] : 20), weights[3] >= 0 ? weights[3] : 20);

            float[] sameWeights = new float[4];
            int sameWeightCount = 0;

            int closeGhosts = 0;
            /*foreach (Ghost g in ghosts)
            {
                if (Vector2.Distance(g.transform.position, rb.position) <= 2.5f) { closeGhosts++; }
            }*/
            if (maxWeight <= 2f && scatterGhosts)// || closeGhosts >= 2)
            {
                //Debug.Log("cheat: SCATTER");
                foreach (Ghost g in ghosts)
                {
                    if (Vector2.Distance(g.transform.position, rb.position) <= 2f) { StartCoroutine(g.SetScatter(2)); }
                    //g.ReverseDirection();
                }
            }

            if (maxWeight == previousBest) { }
            else
            {
                if (nodePos.Count == 0)
                {
                    if (maxWeight == weights[0]) { moveDirection = Vector2.up; }
                    else if (maxWeight == weights[1]) { moveDirection = Vector2.left; }
                    else if (maxWeight == weights[2]) { moveDirection = Vector2.down; }
                    else if (maxWeight == weights[3]) { moveDirection = Vector2.right; }
                }
                else
                {
                    if (maxWeight == weights[0])
                    {
                        sameWeightCount += 1;
                        sameWeights[0] = 1;
                        moveDirection = Vector2.up;
                    }
                    if (maxWeight == weights[1])
                    {
                        sameWeightCount += 1;
                        sameWeights[1] = 1;
                        moveDirection = Vector2.left;
                    }
                    if (maxWeight == weights[2])
                    {
                        sameWeightCount += 1;
                        sameWeights[2] = 1;
                        moveDirection = Vector2.down;
                    }
                    if (maxWeight == weights[3])
                    {
                        sameWeightCount += 1;
                        sameWeights[3] = 1;
                        moveDirection = Vector2.right;
                    }

                    if (sameWeightCount > 1 && !toNode)
                    {
                        Vector2[] vectors = new Vector2[] { Vector2.up, Vector2.left, Vector2.down, Vector2.right };
                        int rand = 0;
                        do
                        {
                            rand = Random.Range(0, 4);
                        } while (sameWeights[rand] != 1 || directionTracker[0] == vectors[rand]);
                        moveDirection = vectors[rand];
                    }

                    //Debug.Log("noPelets: " + noPelletTimer + "    minWeight: " + minWeight);
                    if (noPelletTimer > .2f && minWeight >= 12 && maxWeight <= 80)
                    {
                        if(!toNode)
                        {
                            //chosenNode = nodePos[Random.Range(0, nodePos.Count)];
                            chosenNode = new Vector2(200, 0);

                            foreach (Vector2 node in nodePos)//for (int i = 0; i < nodePos.Count; i++)
                            {
                                if (hasXPelletsAround(node, 1) && Vector2.Distance(node + nodeOffset, transform.position) < Vector2.Distance(chosenNode, transform.position)) { chosenNode = node + nodeOffset; }
                            }
                            
                        }

                        //Debug.Log("To Node: " + "    noPelets: " + noPelletTimer + "    minWeight: " + minWeight);
                        toNode = true;

                        if(chosenNode == new Vector2(200, 0))
                        {
                            GameObject[] pellets = GameObject.FindGameObjectsWithTag("Pellet");
                            GameObject[] powerPellets = GameObject.FindGameObjectsWithTag("Power Pellet");

                            if(pellets.Length != 0) { chosenNode = pellets[0].transform.position; }
                            else if (powerPellets.Length != 0) { chosenNode = powerPellets[0].transform.position; }
                        }
                        SetMoveDirectionFromTarget(chosenNode);

                        if (!hasXPelletsAround(chosenNode, 1) || Vector2.Distance(rb.position, chosenNode) < .1f) { toNode = false; }
                        //Debug.Log("To Node: " + chosenNode);
                    }
                    else
                    {
                        toNode = false;
                    }

                    /*if (sameWeightCount > 1 && !toNode)
                    {
                        chooseNodeTimer = 0;
                        toNode = true;
                        Vector2 bestNode = nodePos[Random.Range(0, nodePos.Count)];

                        foreach (Vector2 node in nodePos)//for (int i = 0; i < nodePos.Count; i++)
                        {
                            if (hasXPelletsAround(node, 1) && Vector2.Distance(node, transform.position) < Vector2.Distance(bestNode, transform.position)) { bestNode = node; }
                        }
                        SetMoveDirectionFromTarget(bestNode);
                    }
                    //else if (sameWeightCount == 1) { toNode = false; }
                    if(chooseNodeTimer < 20) { toNode = false; }*/
                }
            }
        }

        if (moveDirection.x == 0 && moveDirection.y != 0) { rb.position = new Vector2(Mathf.RoundToInt(rb.position.x), rb.position.y); }
        else if (moveDirection.y == 0 && moveDirection.x != 0) { rb.position = new Vector2(rb.position.x, Mathf.RoundToInt(rb.position.y)); }

        rb.MovePosition(Vector2.MoveTowards(rb.position, new Vector2(Mathf.RoundToInt(rb.position.x), Mathf.RoundToInt(rb.position.y)) + moveDirection, speed * Time.fixedDeltaTime));
        transform.right = moveDirection;

        if(prevDirection != moveDirection)
        {
            directionTracker[0] = directionTracker[1];
            directionTracker[1] = moveDirection;
        }
        prevDirection = moveDirection;
        

        Vector2Int coords = ConvertWorldToGridCoords(transform.position);
        //Debug.Log(coords.x + "  " + coords.y);
        //Debug.Log(alterGrid[coords.x, coords.y]);
        //Debug.Log(alterGrid[coords.x, coords.y] + "   " + alterGrid[coords.x + (int)Vector2.up.x, coords.y + (int)Vector2.up.y] + "   " + alterGrid[coords.x + (int)Vector2.left.x, coords.y + (int)Vector2.left.y]
        //                                        + "   " + alterGrid[coords.x + (int)Vector2.down.x, coords.y + (int)Vector2.down.y] + "   " + alterGrid[coords.x + (int)Vector2.right.x, coords.y + (int)Vector2.right.y]);

        updateGridTimer += Time.fixedDeltaTime;
        //chooseNodeTimer += Time.fixedDeltaTime;
        noPelletTimer += Time.fixedDeltaTime;
    }

    void SetMoveDirectionFromTarget(Vector2 target)
    {
        float[] distances = new float[4];
        distances[0] = Vector2.Distance(target, rb.position + new Vector2(0, 1));
        distances[1] = Vector2.Distance(target, rb.position + new Vector2(-1, 0));
        distances[2] = Vector2.Distance(target, rb.position + new Vector2(0, -1));
        distances[3] = Vector2.Distance(target, rb.position + new Vector2(1, 0));

        /*if (Physics2D.BoxCast(rb.position, Vector2.one * .6f, 0, Vector2.up, .3f, mapLayer) || Vector2.up == -prevDirection) { distances[0] = float.MaxValue; }
        if (Physics2D.BoxCast(rb.position, Vector2.one * .6f, 0, Vector2.left, .3f, mapLayer) || Vector2.left == -prevDirection) { distances[1] = float.MaxValue; }
        if (Physics2D.BoxCast(rb.position, Vector2.one * .6f, 0, Vector2.down, .3f, mapLayer) || Vector2.down == -prevDirection) { distances[2] = float.MaxValue; }
        if (Physics2D.BoxCast(rb.position, Vector2.one * .6f, 0, Vector2.right, .3f, mapLayer) || Vector2.right == -prevDirection) { distances[3] = float.MaxValue; }*/
        if (baseGrid[ConvertWorldToGridCoords(rb.position + Vector2.up * .6f).x, ConvertWorldToGridCoords(rb.position + Vector2.up * .6f).y] < 0 || Vector2.up == -prevDirection) { distances[0] = float.MaxValue; }
        if (baseGrid[ConvertWorldToGridCoords(rb.position + Vector2.left * .6f).x, ConvertWorldToGridCoords(rb.position + Vector2.left * .6f).y] < 0 || Vector2.left == -prevDirection) { distances[1] = float.MaxValue; }
        if (baseGrid[ConvertWorldToGridCoords(rb.position + Vector2.down * .6f).x, ConvertWorldToGridCoords(rb.position + Vector2.down * .6f).y] < 0 || Vector2.down == -prevDirection) { distances[2] = float.MaxValue; }
        if (baseGrid[ConvertWorldToGridCoords(rb.position + Vector2.right * .6f).x, ConvertWorldToGridCoords(rb.position + Vector2.right * .6f).y] < 0 || Vector2.right == -prevDirection) { distances[3] = float.MaxValue; }

        float minDistance = Mathf.Min(Mathf.Min(Mathf.Min(distances[0], distances[1]), distances[2]), distances[3]);
        if (minDistance == distances[0]) { moveDirection = Vector2.up; }
        else if (minDistance == distances[1]) { moveDirection = Vector2.left; }
        else if (minDistance == distances[2]) { moveDirection = Vector2.down; }
        else if (minDistance == distances[3]) { moveDirection = Vector2.right; }
        //prevNodeDirection = moveDirection;
    }

    Vector2 nodeOffset;
    bool hasXPelletsAround(Vector2 pos, int x)
    {
        int pelletCount = 0;

        /*if(pelletMap.HasTile(pelletMap.WorldToCell(pos + Vector2.up))) { pelletCount++; }
        if(pelletMap.HasTile(pelletMap.WorldToCell(pos + Vector2.left))) { pelletCount++; }
        if(pelletMap.HasTile(pelletMap.WorldToCell(pos + Vector2.down))) { pelletCount++; }
        if(pelletMap.HasTile(pelletMap.WorldToCell(pos + Vector2.right))) { pelletCount++; }*/
        if (baseGrid[ConvertWorldToGridCoords(pos + Vector2.up).x, ConvertWorldToGridCoords(pos + Vector2.up).y] == 60) { pelletCount++; nodeOffset = Vector2.up; }
        if (baseGrid[ConvertWorldToGridCoords(pos + Vector2.left).x, ConvertWorldToGridCoords(pos + Vector2.left).y] == 60) { pelletCount++; nodeOffset = Vector2.left; }
        if (baseGrid[ConvertWorldToGridCoords(pos + Vector2.down).x, ConvertWorldToGridCoords(pos + Vector2.down).y] == 60) { pelletCount++; nodeOffset = Vector2.down; }
        if (baseGrid[ConvertWorldToGridCoords(pos + Vector2.right).x, ConvertWorldToGridCoords(pos + Vector2.right).y] == 60) { pelletCount++; nodeOffset = Vector2.right; }
        if (baseGrid[ConvertWorldToGridCoords(pos).x, ConvertWorldToGridCoords(pos).y] == 60) { pelletCount++; nodeOffset = Vector2.zero; }

        return pelletCount >= x;
    }

    /*void Die()
    {
        lives--;

        transform.position = initPos;
        foreach (Ghost g in ghosts)
        {
            g.reset();
        }
        player.reset();
    }*/

    IEnumerator Die()
    {
        dead = true;
        //stop = true;
        deathSfx.PlayOneShot(deathSfx.clip, .3f);
        pacmanAnim.Play("PacDie", -1, 0f);
        yield return new WaitForSeconds(1.5f);

        lives--;
        if(lives <= 0)
        {
            gamemanager.Win();
            yield break;
        }

        //moveDirection = Vector2.zero;
        updateGridTimer = -.2f;
        int rev = Random.Range(0, 2) == 0 ? -1 : 1;
        moveDirection = new Vector2(rev*1, 0);
        transform.position = initPos;
        foreach (Ghost g in ghosts)
        {
            g.reset();
        }
        player.reset();
        dead = false;
        stop = false;

        pacmanAnim.Play("PacMan", -1, 0f);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.tag == "Pellet")
        {
            Vector2 position = collision.transform.position;
            baseGrid[ConvertWorldToGridCoords(position).x, ConvertWorldToGridCoords(position).y] = 40;
            collision.gameObject.SetActive(false);
            Destroy(collision.gameObject);

            noPelletTimer = 0;
            totalPellets--;

            if(pelletNum1)
            {
                pelletSfx1.PlayOneShot(pelletSfx1.clip, .3f);
            }
            else
            {
                pelletSfx2.PlayOneShot(pelletSfx2.clip, .3f);
            }
            
            pelletNum1 = !pelletNum1;
        }

        if (collision.tag == "Power Pellet")
        {
            foreach (Ghost g in ghosts)
            {
                StartCoroutine(g.SetFrightened(6));
            }
            StartCoroutine(player.SetFrightened(6));

            Vector2 position = collision.transform.position;
            baseGrid[ConvertWorldToGridCoords(position).x, ConvertWorldToGridCoords(position).y] = 40;
            collision.gameObject.SetActive(false);
            Destroy(collision.gameObject);

            noPelletTimer = 0;
            totalPellets--;
        }

        if (collision.tag == "Ghost" && !dead)
        {
            Ghost g = collision.GetComponent<Ghost>();
            if(g.mode == Ghost.Mode.chase || g.mode == Ghost.Mode.scatter)
            {
                //Die();
                dead = true;
                StartCoroutine(Die());
            }
        }

        if (collision.tag == "Player" && !dead)
        {
            PlayerController p = collision.GetComponent<PlayerController>();
            if (p.mode == PlayerController.Mode.chase)
            {
                //Die();
                dead = true;
                StartCoroutine(Die());
            }
        }

        if (collision.tag == "Teleport")
        {
            transform.position = collision.transform.GetChild(0).position;
        }
    }

    public bool getDead()
    {
        return dead;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Vector2Int roundedPosition = new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y));

        if (toNode)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, chosenNode);
            Gizmos.DrawSphere(chosenNode, .5f);
        }
        else
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, (Vector2)transform.position + moveDirection);
            Gizmos.DrawSphere(roundedPosition + moveDirection, .5f);
        }

        if(weights !=  null && weights.Length > 0)
        {
            /*Handles.BeginGUI();
            GUI.color = Color.yellow;
            
            Handles.Label((Vector2)transform.position + Vector2.up*2, weights[0] + "");
            Handles.Label((Vector2)transform.position + Vector2.left*2, weights[1] + "");
            Handles.Label((Vector2)transform.position + Vector2.down*2, weights[2] + "");
            Handles.Label((Vector2)transform.position + Vector2.right*2, weights[3] + "");
            Handles.EndGUI();*/
            //float maxWeight = Mathf.Max(Mathf.Max(Mathf.Max(weights[0], weights[1]), weights[2]), weights[3]);
            //float minWeight = Mathf.Min(Mathf.Min(Mathf.Min(weights[0] >= 0 ? weights[0] : 20, weights[1] >= 0 ? weights[1] : 20), weights[2] >= 0 ? weights[2] : 20), weights[3] >= 0 ? weights[3] : 20);
            weightTextParent.gameObject.SetActive(true);

            weightTexts[0].text = ((float)((int)(weights[0] * 10))) / 10 + "";
            weightTexts[1].text = ((float)((int)(weights[1] * 10))) / 10 + "";
            weightTexts[2].text = ((float)((int)(weights[2] * 10))) / 10 + "";
            weightTexts[3].text = ((float)((int)(weights[3] * 10))) / 10 + "";

            weightTextParent.rotation = Quaternion.identity;
        }
    }
#endif
}
