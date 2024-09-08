using UnityEngine;
using Seb.Vis;

[ExecuteAlways]
public class UIField : MonoBehaviour
{
    public Vector2 size;
    public Color colBG;
    public Color colBGHover;
    public float fontSize;
    public Color textCol;
    public float value;
    public float sensitivity;
    public bool intDisplay;
    public bool singleDecimal;
    public string label;
    public Vector2 minMax = new Vector2(-100, 100);

    public string pad;
    public string unit;

    static UIField active;

    bool inside;
    bool dragging;
    Vector2 dragPrev;

    float lastUpTime;
    float startVal;

    public void OnEnable()
    {
        active = null;
        startVal = value;
    }

    private void Update()
    {
        DrawField();
        if (Application.isPlaying)
        {
            HandleInput();
        }
    }

    void HandleInput()
    {
        Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        bool hover = PointInBox(mouseWorld, transform.position, size);
        inside = hover && active == null;

        if (inside && Input.GetMouseButtonDown(0))
        {
            dragging = true;
            dragPrev = mouseWorld;
            active = this;
        }
        if (Input.GetMouseButtonUp(0))
        {
            dragging = false;
            active = null;
            if (Time.time - lastUpTime < 0.4f && hover)
            {
                value = startVal;
            }
            lastUpTime = Time.time;
        }

        if (dragging)
        {
            Vector2 del = mouseWorld - dragPrev;
            value += del.x * sensitivity;
            value = Mathf.Clamp(value, minMax.x, minMax.y);
            dragPrev = mouseWorld;
        }
    }

    bool PointInBox(Vector2 p, Vector2 centre, Vector2 size)
    {
        Vector2 o = p - centre;
        return Mathf.Abs(o.x) < size.x / 2 && Mathf.Abs(o.y) < size.y / 2;

    }

    public float GetValue()
    {
        return value;
    }

    void DrawField()
    {
        Color activeColBg = colBG;
        if (dragging || inside)
        {
            activeColBg = colBGHover;
        }
        Draw.StartLayerIfNotInMatching(Vector2.zero, 1, false);
        Draw.Quad(transform.position, size, activeColBg);

        string valDisplay = singleDecimal ? $"{value:0.0}" : $"{value:0.00}";
        if (intDisplay) valDisplay = $"{(int)value}";

        string text = $"{label}:{pad} {valDisplay} {unit}";
        Color textCol = dragging ? Color.white : this.textCol;
        Vector2 textPos = (Vector2)transform.position;
        Draw.Text(FontType.MapleMonoBold, text, fontSize, textPos, Anchor.TextCentre, textCol);
    }
}
