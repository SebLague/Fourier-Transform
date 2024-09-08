using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Seb.Vis.UI;

[ExecuteAlways]
public class LayoutTest : MonoBehaviour
{
    [Header("Grid test")]
    public bool gridGrowUp;
    public int numH;
    public int numV;
    public Vector2 spacingGrid;
    public Vector2 gridPos;

    [Header("Horizontal test")]
    public float spacing;
    public int numA = 3;
    public int numB = 2;
    public Vector2 groupPos;
    public Vector2 horizontalPos;
    public Vector2 buttonSize;

    void Update()
    {



        using (UI.CreateFixedAspectUIScope())
        {
            GridTest();
            HorizontalTest();
        }
    }

    void GridTest()
    {


        using (UI.CreateLayoutScope(UI.LayoutScope.Kind.Right, gridPos, spacingGrid.x))
        {
            for (int i = 0; i < numH; i++)
            {
                var theme = UI.TestButtonTheme;
                theme.buttonCols.normal = Color.HSVToRGB(i / (numH - 1f), 1, 1);

                using (UI.CreateLayoutScope(gridGrowUp ? UI.LayoutScope.Kind.Up : UI.LayoutScope.Kind.Down, gridPos, spacingGrid.y))
                {
                    for (int j = 0; j < numV; j++)
                    {
                        UI.DrawButton($"Row{j}", theme, gridPos, buttonSize, true, true, Seb.Vis.Anchor.Centre);
                    }
                }
            }
        }
    }
    void HorizontalTest()
    {
        var themeA = UI.TestButtonTheme;
        themeA.buttonCols.normal = Color.red;
        var themeB = UI.TestButtonTheme;
        themeB.buttonCols.normal = Color.green;

        using (UI.CreateLayoutScope(UI.LayoutScope.Kind.Right, horizontalPos, spacing))
        {
            for (int i = 0; i < numA; i++)
            {
                UI.DrawButton("TestA", themeA, Vector2.one * 10, buttonSize, true);
            }
            DrawGroupTest(groupPos);
            for (int i = 0; i < numB; i++)
            {
                UI.DrawButton("TestB", themeB, Vector2.one * 10, buttonSize);
            }
        }
    }

    void DrawGroupTest(Vector2 pos)
    {
        var theme = UI.TestButtonTheme;

        using (UI.CreateLayoutScope(UI.LayoutScope.Kind.Right, pos, 1))
        {
            for (int i = 0; i < 4; i++)
            {
                UI.DrawButton("Group", theme, pos, buttonSize, true);
            }
        }
    }
}
