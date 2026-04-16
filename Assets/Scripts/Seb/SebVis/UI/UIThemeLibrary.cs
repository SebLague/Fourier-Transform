using System;
using Seb.Helpers;
using UnityEngine;

namespace Seb.Visualization.UI
{
    public static class UIThemeLibrary
    {
        public enum ThemeName
        {
            RedTest,
            BlueTest
        }


        public const FontType DefaultFont = FontType.JetbrainsMonoBold;
        public const float FontSizeSmall = 1;

        public static readonly Vector2 PaddingScaleButton = new(1.2f, 1.5f);

        static readonly ThemeSettings red = new()
        {
            FontSize = FontSizeSmall,
            AccentBright = MakeCol(207, 101, 101),
            AccentDark = MakeCol(180, 90, 90),
            Base = MakeCol(243, 168, 168),
            Inactive = MakeCol(160, 130, 130)
        };

        static readonly ThemeSettings blue = red;

        public static UITheme CreateTheme(ThemeName themeName)
        {
            return themeName switch
            {
                ThemeName.RedTest => CreateTheme(red),
                ThemeName.BlueTest => CreateTheme(blue),
                _ => throw new Exception(themeName + " not implemented")
            };
        }

        public static UITheme CreateTheme(ThemeSettings settings)
        {
            ButtonTheme buttonTheme = CreateButtonTheme(settings);

            return new UITheme()
            {
                font = settings.Font,
                textSize = settings.FontSize,
                panelCol = settings.Panel,
                panelOutlineCol =  settings.PanelOutline,
                panelOutlineThickness = settings.PanelOutlineThickness,
                // elements
                buttonTheme = buttonTheme,
                wheelSelector = CreateWheelSelectorTheme(settings, buttonTheme),
                checkboxTheme = CreateCheckboxTheme(settings),
                inputFieldTheme = CreateInputFieldTheme(settings)
            };
        }

        static CheckboxTheme CreateCheckboxTheme(ThemeSettings cols) =>
            new()
            {
                boxCol = Color.white,
                tickCol = Color.black
            };

        static InputFieldTheme CreateInputFieldTheme(ThemeSettings settings) =>
            new()
            {
                bgCol = Color.white,
                defaultTextCol = Color.gray,
                font = settings.Font,
                fontSize = settings.FontSize,
                focusBorderCol = settings.AccentBright,
                textCol = Color.black,
                borderThickness = settings.PanelOutlineThickness
            };

        static ButtonTheme CreateButtonTheme(ThemeSettings settings) =>
            new()
            {
                font = settings.Font,
                fontSize = settings.FontSize,
                textCols = new ButtonTheme.StateCols(Color.white, Color.white, Color.white, ColHelper.Brighten(settings.Inactive, 0.1f)),
                buttonCols = new ButtonTheme.StateCols(settings.Base, settings.AccentBright, settings.AccentDark, settings.Inactive),
                paddingScale = PaddingScaleButton
            };

        static WheelSelectorTheme CreateWheelSelectorTheme(ThemeSettings cols, ButtonTheme buttonTheme) =>
            new()
            {
                buttonTheme = buttonTheme,
                backgroundCol = Color.white,
                textCol = Color.black
            };

        static Color MakeCol(int r, int g, int b)
        {
            const float scale = 1 / 255f;
            return new Color(r * scale, g * scale, b * scale, 1);
        }

        [System.Serializable]
        public struct ThemeSettings
        {
            public FontType Font;
            public float FontSize;
            public Color Panel;
            public Color PanelOutline;
            public float PanelOutlineThickness;
            public Color Base;
            public Color AccentBright;
            public Color AccentDark;
            public Color Inactive;
        }
    }
}