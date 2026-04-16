using Audio.Synth;
using UnityEngine;

public abstract class DiscreteCurveWidgetBase : Widget
{
    protected DiscreteCurve discreteCurve;
    
    public DiscreteCurve GetDiscreteCurve()
    {
        UpdateDiscreteCurve();
        return discreteCurve;
    }

    public abstract Vector2 GetXAxisMinMax();
    public abstract Vector2 GetYAxisMinMax();
    
    protected abstract void UpdateDiscreteCurve();

}
