using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Seb.Vis;

namespace Seb.Vis.UI
{
    public struct UITheme
    {
        public float fontSizeSmall;
        public float fontSizeMedium;
        public float fontSizeLarge;
        public Color colBG;

        public ButtonTheme buttonTheme;
        public InputFieldTheme inputFieldTheme;
    }

    [System.Serializable]
    public struct ButtonTheme
    {
        public FontType font;
        public float fontSize;
        public StateCols textCols;
        public StateCols buttonCols;
        public Vector2 paddingScale;

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

    [System.Serializable]
    public struct InputFieldTheme
    {
        public FontType font;
        public float fontSize;
        // final size of the input field will be calculated as: size + paddingScale * size.y
        public Vector2 paddingScale;
        public Color defaultTextCol;
        public Color textCol;
        public Color bgCol;
        public Color focusBorderCol;
    }
}