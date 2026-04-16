using Audio.Display;
using Audio.Tools;
using Seb.Helpers;
using Seb.Visualization;
using UnityEngine;

[ExecuteAlways]
public class SustainAddon : MonoBehaviour
{
    [Range(0,1)] public float sustain;
    public float offsetX;
    public float width;
    public CurveWidget curve;
    public Color colBar;
    public Color colHandleNormal;
    bool dragging;
    
    void Update()
    {
        Vis.StartLayerIfNotInMatching(Vector2.zero, 1, false);
        Vector2 bottomRight = new Vector2(offsetX + curve.Right, curve.Bottom);

        float barHeight = curve.Size.y * sustain;
        float x = bottomRight.x + width / 2;
        float y = curve.Bottom + barHeight/2;
        Vector2 size = new Vector2(width, barHeight);
        Vector2 centre = new Vector2(x, y);
        Vis.Quad(centre, size, colBar);
        
        Vector2 handlePos = new Vector2(centre.x, Mathf.Lerp(curve.Bottom, curve.Top, sustain));

        bool hover = false;
        if ((InputHelper.MousePosWorld - handlePos).magnitude < width)
        {
            hover = true;
            if (InputHelper.IsMouseDownThisFrame(MouseButton.Left))
            {
                dragging = true;
            }
        }
        
        
        if (InputHelper.IsMouseUpThisFrame(MouseButton.Left)) dragging = false;

        if (dragging)
        {
            sustain = Mathf.InverseLerp(curve.Bottom, curve.Top, InputHelper.MousePosWorld.y);
        }

        Color handleCol = (hover || dragging) ? Color.white : colHandleNormal;
        Vis.Point(handlePos, width, handleCol);
        
        Vector2 susPos = handlePos + Vector2.right * 0.3f;
        Vis.Text(DisplaySettings.Instance.ActiveUITheme.font, $"SUSTAIN = {sustain:0.0}", DisplaySettings.Instance.ActiveUITheme.textSize, susPos, Anchor.TextCentreLeft, Color.white);

        curve.sustain = sustain;

    }
}
