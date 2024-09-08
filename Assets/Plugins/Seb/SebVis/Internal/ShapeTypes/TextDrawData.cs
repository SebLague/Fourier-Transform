using Seb.Vis.Text.FontLoading;
using UnityEngine;

namespace Seb.Vis.Internal
{
    public readonly struct TextDrawData
    {
        public readonly FontData fontData;
        public readonly string text;
        public readonly float fontSize;
        public readonly float lineSpacing;
        public readonly Vector2 pos;
        public readonly Anchor anchor;
        public readonly Color col;
        public readonly Vector2 maskMin;
        public readonly Vector2 maskMax;

        public TextDrawData(FontData fontData, string text, float fontSize, float lineSpacing, Vector2 pos, Anchor anchor, Color col, Vector2 maskMin, Vector2 maskMax)
        {
            this.fontData = fontData;
            this.text = text;
            this.fontSize = fontSize;
            this.lineSpacing = lineSpacing;
            this.pos = pos;
            this.anchor = anchor;
            this.col = col;
            this.maskMin = maskMin;
            this.maskMax = maskMax;
        }
    }
}