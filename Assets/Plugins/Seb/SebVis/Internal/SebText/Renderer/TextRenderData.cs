using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Seb.Vis.Text.FontLoading;

namespace Seb.Vis.Text.Rendering
{
    public class TextRenderData
    {
        public readonly Dictionary<Glyph, int> glyphMetadataIndexLookup = new();
        // [Shader Data] for each unique glyph, stores: bezier offset, num contours, contour length/s
        public readonly List<int> glyphMetadata = new();
        // [Shader Data] for each glyph, stores: bezier points
        public readonly List<Vector2> bezierPoints = new();

    }
}