using System;
using Audio.Tools;
using Seb.Visualization.UI;
using UnityEngine;

namespace Audio.Display
{
    public class DisplaySettings : MonoBehaviour
    {
        public UIThemeLibrary.ThemeSettings themeSettings;
        [Header("Other Settings")]
        public float pointRadius;
        public float pointOutlineSize;
        public float pointSelectPad;
        public float lineThickness;
        public float textPadFactor;
        public Color axisLabelCol;

        public float textPadding => themeSettings.FontSize * textPadFactor;

        public WidgetTheme curveWidgetRed;
        public WidgetTheme curveWidgetYellow;
        public WidgetTheme curveWidgetGreen;
        public WidgetTheme curveWidgetBlue;
        public WidgetTheme curveWidgetWhite;
        public WidgetTheme curveWidgetPurple;

        UITheme theme;

        static DisplaySettings instance;
        bool themeNeedsUpdate = true;

        void UpdateTheme()
        {
            themeNeedsUpdate = false;
            theme = UIThemeLibrary.CreateTheme(themeSettings);
        }

        public static DisplaySettings Instance
        {
            get
            {
                if (!instance) instance = FindFirstObjectByType<DisplaySettings>();
                return instance;
            }
        }

        public UITheme ActiveUITheme
        {
            get
            {
                if (theme == null || themeNeedsUpdate) UpdateTheme();
                return theme;
            }
        }

        void OnValidate()
        {
            themeNeedsUpdate = true;
        }

        public WidgetTheme GetWidgetTheme(ThemeCol themeCol)
        {
            return themeCol switch
            {
                ThemeCol.Green => curveWidgetGreen,
                ThemeCol.Yellow => curveWidgetYellow,
                ThemeCol.Red => curveWidgetRed,
                ThemeCol.Blue => curveWidgetBlue,
                ThemeCol.White => curveWidgetWhite,
                ThemeCol.Purple => curveWidgetPurple,
                _ => throw new Exception("Missing theme")
            };
        }
    }
    
    public enum ThemeCol
    {
        Red,
        Yellow,
        Green,
        Blue,
        White,
        Purple
    }

}