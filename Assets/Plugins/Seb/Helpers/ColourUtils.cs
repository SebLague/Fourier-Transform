using System;
using UnityEngine;

namespace Seb.Helpers
{
    public class ColourUtils
    {
        public static Color WithAlpha(Color col, float a) => new Color(col.r, col.g, col.b, a);

        public static Color Greyscale(float value) => new Color(value, value, value, 1);
        public static Color MakeCol255(int v) => MakeCol255(v, v, v, 255);
        public static Color MakeCol255(int r, int g, int b) => MakeCol255(r, g, b, 255);
        public static Color MakeCol255(int r, int g, int b, int a) => new(r / 255f, g / 255f, b / 255f, a / 255f);

        // Parse hex code. Can be in format "RRGGBBAA" or "RRGGBB". The # should not be included.
        public static bool TryParseHexCode(ReadOnlySpan<char> code, out Color col)
        {
            col = Color.white;

            // invalid length
            if (code.Length != 6 && code.Length != 8) return false;
            bool hasAlpha = code.Length == 8;

            // ensure all inputs are valid hex values
            for (int i = 0; i < code.Length; i++)
            {
                char c = code[i];
                bool validChar = c >= '0' && c <= '9' || c >= 'a' && c <= 'f' || c >= 'A' && c <= 'F';
                if (!validChar) return false;
            }

            byte r = byte.Parse(code.Slice(0, 2), System.Globalization.NumberStyles.HexNumber);
            byte g = byte.Parse(code.Slice(2, 2), System.Globalization.NumberStyles.HexNumber);
            byte b = byte.Parse(code.Slice(4, 2), System.Globalization.NumberStyles.HexNumber);
            byte a = hasAlpha ? byte.Parse(code.Slice(6, 2), System.Globalization.NumberStyles.HexNumber) : (byte)255;

            const float byteToFloat = 1 / 255f;
            col = new Color(r * byteToFloat, g * byteToFloat, b * byteToFloat, a * byteToFloat);
            return true;
        }

        public static Color TextBlackOrWhite(Color backgroundCol)
        {
            return Luminance(backgroundCol) > 0.57f ? Color.black : Color.white;
        }

        public static Color GenerateRandomColour(System.Random rng)
        {
            float hue = (float)rng.NextDouble();
            float sat = Mathf.Lerp(0.2f, 1, (float)rng.NextDouble());
            float val = Mathf.Lerp(0.2f, 1, (float)rng.NextDouble());
            return Color.HSVToRGB(hue, sat, val);
        }

        public static float Luminance(Color col)
        {
            return 0.2126f * col.r + 0.7152f * col.g + 0.0722f * col.b;
        }

        public static Color Darken(Color col, float darkenAmount, float desaturateAmount = 0)
        {
            return TweakHSV(col, 0, -desaturateAmount, -darkenAmount);
        }

        public static Color Brighten(Color col, float brightenAmount, float saturateAmount = 0)
        {
            return TweakHSV(col, 0, saturateAmount, brightenAmount);
        }

        public static Color TweakHSV(Color col, float deltaH, float deltaS, float deltaV)
        {
            Color.RGBToHSV(col, out float h, out float s, out float v);
            h = (h + deltaH + 1) % 1;
            s = Mathf.Clamp01(s + deltaS);
            v = Mathf.Clamp01(v + deltaV);
            return Color.HSVToRGB(h, s, v);
        }

    }
}