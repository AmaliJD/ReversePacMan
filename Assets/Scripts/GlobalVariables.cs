using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Global Variables", menuName = "Scriptable Objects/Global Variables", order = 1)]
public class GlobalVariables : ScriptableObject
{
    [SerializeField]
    public Color mapColor;

    public Color MapColor
    {
        get => mapColor;
        set => mapColor = value;
    }

}
