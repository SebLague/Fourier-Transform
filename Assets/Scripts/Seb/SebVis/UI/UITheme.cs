using System;
using UnityEngine;

namespace Seb.Visualization.UI
{
    public class UITheme
    {
        public FontType font;
        public float textSize;
        public Color panelCol;
        public Color panelOutlineCol;
        public float panelOutlineThickness;

        // ---- UI element themes ----
        public ButtonTheme buttonTheme;
        public CheckboxTheme checkboxTheme;
        public InputFieldTheme inputFieldTheme;
        public WheelSelectorTheme wheelSelector;
    }


    public struct ScrollViewTheme
    {
        public Color backgroundCol;
        public Color scrollBarColBackground;
        public Color scrollBarColInactive;
        public Color scrollBarColNormal;
        public Color scrollBarColHover;
        public Color scrollBarColPressed;
        public float padding;
        public float scrollBarWidth;
    }


    public struct CheckboxTheme
    {
        public Color boxCol;
        public Color tickCol;
    }

    public struct WheelSelectorTheme
    {
        public ButtonTheme buttonTheme;
        public Color backgroundCol;
        public Color textCol;
        public Color inactiveTextCol;

        public void OverrideFont(FontType font)
        {
            buttonTheme.font = font;
        }

        public void OverrideFontSize(float size)
        {
            buttonTheme.fontSize = size;
        }

        public void OverrideFontAndSize(FontType font, float size)
        {
            buttonTheme.font = font;
            buttonTheme.fontSize = size;
        }
    }

    [Serializable]
    public struct ButtonTheme
    {
        public FontType font;
        public float fontSize;
        public StateCols textCols;
        public StateCols buttonCols;
        public Vector2 paddingScale;

        [Serializable]
        public struct StateCols
        {
            public Color normal;
            public Color hover;
            public Color pressed;
            public Color inactive;

            public StateCols(Color normal, Color hover, Color pressed, Color inactive)
            {
                this.normal = normal;
                this.hover = hover;
                this.pressed = pressed;
                this.inactive = inactive;
            }

            public Color GetCol(bool isHover, bool isPressed, bool isActive)
            {
                if (!isActive) return inactive;
                if (isPressed && isHover) return pressed;
                if (isHover) return hover;
                return normal;
            }
        }
    }

    [Serializable]
    public struct InputFieldTheme
    {
        public FontType font;
        public float fontSize;
        public Color defaultTextCol;
        public Color textCol;
        public Color bgCol;
        public Color focusBorderCol;
        public float borderThickness;
    }
}