using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelect : MonoBehaviour
{
    public GlobalVariables global;

    private void Awake()
    {
        global.MapColor = new Color(.1f, .2f, 1, 1);
    }

    public void GoToLevel(int i)
    {
        SceneManager.LoadScene("Level " + i);
    }

    public void Quit()
    {
        Application.Quit();
    }
}
