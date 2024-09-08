using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Seb.Vis.Tests
{
    [ExecuteAlways]
    public class TextTest : MonoBehaviour
    {
        public bool screenSpace;
        public FontType font;
        [Multiline(3)]
        public string text;
        public float fontSize;
        public float lineSpacing = 1;
        public Anchor anchor;
        public Color col;
        public Vector2 layerOffset;
        public float layerScale;

        void Update()
        {
            Draw.StartLayerIfNotInMatching(layerOffset, layerScale, screenSpace);

            var bounds = Draw.CalculateTextBounds(text, font, fontSize, transform.position, anchor, lineSpacing);
            Draw.QuadMinMax(bounds.BoundsMin, bounds.BoundsMax, Color.black);
            Draw.Text(font, text, fontSize, transform.position, anchor, col, lineSpacing);
        }
    }
}