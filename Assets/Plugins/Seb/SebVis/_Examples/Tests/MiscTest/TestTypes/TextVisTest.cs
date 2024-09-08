using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextVisTest : MonoBehaviour
{
    [Multiline] public string text;
    public Seb.Vis.FontType fontType;
    public Seb.Vis.Anchor textAlign;

    public float fontSize = 1;
    public Color col = Color.white;

}
