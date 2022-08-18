using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public GlobalVariables global;
    private Color startColor;
    public int levelNumber;
    public Vector2 swapModeRates;
    private List<Ghost> ghosts;
    public Transform ghostsParent;
    public Tilemap map;
    private PlayerController player;
    private PacManAI pacman;
    private float timer, timeTillSwap, time;

    public AudioSource bgMusic, winsfx;
    public AudioSource[] sirens;
    public AudioSource frightened, eaten;
    private AudioSource siren;
    public int sirenIndex = 0;
    private int maxlevels = 5;

    public GameObject loseScreen, winScreen;
    public GameObject[] reasons;
    private bool winstate, losestate;

    private void Awake()
    {
        Application.targetFrameRate = 60;
        map.color = global.MapColor;
        startColor = map.color;

        ghosts = new List<Ghost>();
        foreach (Transform t in ghostsParent)
        {
            //ghosts.Add(t.GetComponent<Ghost>());
            if (t.gameObject.activeSelf)
            {
                ghosts.Add(t.GetComponent<Ghost>());
            }
        }
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerController>();
        pacman = GameObject.FindGameObjectWithTag("PacMan").GetComponent<PacManAI>();

        siren = sirens[sirenIndex];
        if(swapModeRates.x != 0 && swapModeRates.y != 0)
        {
            StartCoroutine(SwapMode());
        }
        //StartCoroutine(SwapMode());
        StartCoroutine(ChangeMapColor());
        StartCoroutine(BGMusic());

        winstate = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            Restart();
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            Menu();
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            if(winstate)
            {
                NextLevel();
            }
            else if(losestate)
            {
                Restart();
            }
        }
    }

    public void Restart()
    {
        global.MapColor = startColor;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void Menu()
    {
        global.MapColor = new Color(.1f, .2f, 1, 1);
        SceneManager.LoadScene("Menu");
    }

    public void NextLevel()
    {
        global.MapColor = map.color;
        if (levelNumber + 1 > maxlevels)
        {
            SceneManager.LoadScene("Menu");
        }
        else
        {
            SceneManager.LoadScene("Level " + (levelNumber + 1));
        }
    }

    IEnumerator BGMusic()
    {
        while(true)
        {
            /*bool DEAD = false, EAT = false, FRIGHT = false;

            DEAD = pacman.getDead();
            foreach (Ghost g in ghosts)
            {
                if(g.mode == Ghost.Mode.eaten)
                {
                    EAT = true;
                    break;
                }
                if (g.mode == Ghost.Mode.frightented)
                {
                    FRIGHT = true;
                    break;
                }
            }
            if (player.mode == PlayerController.Mode.eaten)
            {
                EAT = true;
            }
            if (player.mode == PlayerController.Mode.frightented)
            {
                FRIGHT = true;
            }

            if(DEAD)
            {
                eaten.Stop();
                frightened.Stop();
                siren.Stop();
            }
            else if (EAT)
            {
                if(!eaten.isPlaying)
                {
                    eaten.Play();
                    frightened.Stop();
                    siren.Stop();
                }
            }
            else if (FRIGHT)
            {
                if (!frightened.isPlaying)
                {
                    eaten.Stop();
                    frightened.Play();
                    siren.Stop();
                }
            }
            else
            {
                if (!siren.isPlaying)
                {
                    eaten.Stop();
                    frightened.Stop();

                    siren = sirens[sirenIndex];
                    siren.Play();
                }
                else if(siren.isPlaying && !sirens[sirenIndex].isPlaying)
                {
                    siren.Stop();
                    siren = sirens[sirenIndex];
                    siren.Play();
                }
            }*/

            bool DEAD = false, EAT = false, FRIGHT = false;

            DEAD = pacman.getDead();
            foreach (Ghost g in ghosts)
            {
                if (g.mode == Ghost.Mode.eaten)
                {
                    EAT = true;
                    break;
                }
                if (g.mode == Ghost.Mode.frightented)
                {
                    FRIGHT = true;
                    break;
                }
            }
            if (player.mode == PlayerController.Mode.eaten)
            {
                EAT = true;
            }
            if (player.mode == PlayerController.Mode.frightented)
            {
                FRIGHT = true;
            }

            if (DEAD)
            {
                eaten.Stop();
                frightened.Stop();
                siren.Stop();
            }
            else
            {
                if(!bgMusic.isPlaying)
                {
                    bgMusic.Play();
                }
            }
            
            if (EAT || FRIGHT)
            {
                if (!eaten.isPlaying)
                {
                    eaten.Play();
                    frightened.Stop();
                }
            }
            else
            {
                eaten.Stop();
                frightened.Stop();
            }

            yield return null;
        }
    }

    IEnumerator SwapMode()
    {
        while (true)
        {
            //timeTillSwap = Random.Range(10f, 30f);
            //time = Random.Range(1f, 10f);
            //Debug.Log("Wait: " + swapModeRates.x);
            yield return new WaitForSeconds(swapModeRates.x);

            foreach (Ghost g in ghosts)
            {
                StartCoroutine(g.SetScatter(swapModeRates.y));
            }
            //Debug.Log("SCATTER: " + swapModeRates.y);

            yield return new WaitForSeconds(swapModeRates.y);
        }
    }

    private IEnumerator ChangeMapColor()
    {
        while(true)
        {
            float time = 0, duration = 2f;
            Color oldColor = map.color;
            Color.RGBToHSV(map.color, out float H, out float S, out float V);
            H = (float)((int)(H * 360 - 1) % 360) / 360f;
            Color newColor = Color.HSVToRGB(H, S, V);

            while(time < duration)
            {
                map.color = Color.Lerp(oldColor, newColor, time / duration);
                time += Time.deltaTime;
                yield return null;
            }
            map.color = newColor;
            yield return null;
        }
    }

    public void Win()
    {
        StopAllCoroutines();
        eaten.Stop();
        frightened.Stop();
        siren.Stop();
        winsfx.PlayOneShot(winsfx.clip, .4f);
        winstate = true;
        winScreen.SetActive(true);

        foreach (Ghost g in ghosts)
        {
            g.stop = true;
        }
        pacman.stop = true;
    }

    public void Lose(int reason)
    {
        StopAllCoroutines();
        eaten.Stop();
        frightened.Stop();
        siren.Stop();
        losestate = true;

        loseScreen.SetActive(true);
        reasons[reason].SetActive(true);

        foreach (Ghost g in ghosts)
        {
            g.stop = true;
        }
        pacman.stop = true;
        player.stop = true; 
    }
}
