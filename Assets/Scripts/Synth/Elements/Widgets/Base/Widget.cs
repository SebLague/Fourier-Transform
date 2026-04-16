using Audio.Display;
using Audio.Tools;
using Seb.Helpers;
using Seb.Visualization;
using Seb.Visualization.UI;
using UnityEngine;

public class Widget : MonoBehaviour
{
    //public float titleBgPad;
    [Header("Widget Display")]
    public ThemeCol themeCol;
    public string title = "Title";
    public Vector2 titleOffset;

    public DisplaySettings GlobalTheme => DisplaySettings.Instance;
    public UITheme uiTheme => DisplaySettings.Instance.ActiveUITheme;
    public WidgetTheme widgetTheme => GlobalTheme.GetWidgetTheme(themeCol);

    public Vector2 Centre => transform.position;
    public Vector2 Size => transform.localScale;

    public Vector2 MousePos => InputHelper.MousePosWorld - Centre;

    public float thickness => GlobalTheme.lineThickness;
    public float handleSize => GlobalTheme.pointRadius;
    public float handleSizeSelected => GlobalTheme.pointRadius + GlobalTheme.pointSelectPad;

    protected void StartLayer()
    {
        Vis.StartLayerIfNotInMatching(Centre, 1, false);
    }

    protected void DrawPanel(bool axes)
    {
        // Background
        Vis.Quad(Vector2.zero, Size, widgetTheme.panelCol);
        if (axes)
        {
            Vis.LineCentered(Vector2.zero, Vector2.right * Size.x / 2, uiTheme.panelOutlineThickness, widgetTheme.panelAxisCol);
            Vis.LineCentered(Vector2.zero, Vector2.up * Size.x / 2, uiTheme.panelOutlineThickness, widgetTheme.panelAxisCol);
        }

        // Name
        Vector2 textPosCentreLeft = new Vector2(Left, Top) + titleOffset;
        Vector2 textBoundsSize = Vis.CalculateTextBoundsSize(title, uiTheme.textSize, uiTheme.font);
        //Vis.Quad(textPosCentreLeft + Vector2.right * textBoundsSize.x / 2, textBoundsSize + Vector2.one * titleBgPad, Color.black);
        //Vis.Text(uiTheme.font, title, uiTheme.textSize, textPosCentreLeft, Anchor.CentreLeft, widgetTheme.titleCol);
        Vis.Text(uiTheme.font, title, uiTheme.textSize, textPosCentreLeft, Anchor.CentreLeft, Color.white);
    }


    protected void DrawPanelOutline()
    {
        Color outlineCol = widgetTheme.panelBorderCol;
        Vis.QuadOutline(Vector2.zero, Size, uiTheme.panelOutlineThickness, outlineCol);
    }

    // Returns true if mouse is over handle
    protected void DrawHandle(ref Vector2 controlUV, int controlID, ref int selectedID, ref int mouseOverID, ref Vector2 mouseOffset, bool lockX = false)
    {
        Vector2 handleWorld = UVToWorld(controlUV);

        Vector2 mouseWorld = MousePos;

        bool selected = controlID == selectedID;
        bool hover = (handleWorld - mouseWorld).magnitude < handleSize * 1.5f;

        if (hover) mouseOverID = controlID;

        if (selected)
        {
            handleWorld = mouseWorld + mouseOffset;
            Vector2 controlUV_new = WorldToUV(handleWorld);
            if (lockX) controlUV_new.x = controlUV.x;
            controlUV = controlUV_new;
            handleWorld = UVToWorld(controlUV);
        }

        float activeSize = (hover || selected) ? handleSizeSelected : handleSize;

        Color activeCol = selected ? widgetTheme.controlPointSelectedCol : hover ? widgetTheme.controlPointHoverCol : widgetTheme.controlPointCol;
        Vis.Point(handleWorld, activeSize + GlobalTheme.pointOutlineSize, widgetTheme.controlPointOutlineCol);
        Vis.Point(handleWorld, activeSize, activeCol);

        if (hover && InputHelper.IsMouseDownThisFrame(MouseButton.Left))
        {
            selectedID = controlID;
            mouseOffset = handleWorld - mouseWorld;
        }

        if (InputHelper.IsMouseUpThisFrame(MouseButton.Left))
        {
            selectedID = -1;
        }
    }

    public bool MouseInBounds => InputHelper.MouseInsideBounds_World(Centre, Size);

    public Vector2 GetMouseUV(bool clamped)
    {
        Vector2 uv = MousePos + Size / 2;
        uv.x /= Size.x;
        uv.y /= Size.y;
        if (clamped)
        {
            uv.x = Mathf.Clamp01(uv.x);
            uv.y = Mathf.Clamp01(uv.y);
        }

        return uv;
    }

    public virtual Vector2 UVToWorld(Vector2 uv)
    {
        float x = Left + Size.x * uv.x;
        float y = Bottom + Size.y * uv.y;
        return new Vector2(x, y);
    }

    public virtual Vector2 WorldToUV(Vector2 pos)
    {
        float u = Mathf.InverseLerp(Left, Left + Size.x, pos.x);
        float v = Mathf.InverseLerp(Bottom, Bottom + Size.y, pos.y);
        return new Vector2(u, v);
    }

    public float Left => -Size.x / 2;
    public float Right => Size.x / 2;
    public float Bottom => -Size.y / 2;
    public float Top => Size.y / 2;
}