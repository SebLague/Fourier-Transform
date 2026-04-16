using UnityEngine;

namespace Seb.Helpers
{
    // ---- Version 0.0 [06/Jan/2026] ----
    public static class TextureHelper
    {
        /*
         * Shader usage note:
         * Texture2D<float4> ColourMap;
         * SamplerState linear_clamp_sampler;
         * ColourMap.SampleLevel(linear_clamp_sampler, float2(colT, 0.5), 0);
         */
        
        public static void TextureFromGradient(ref Texture2D texture, int width, Gradient gradient, FilterMode filterMode = FilterMode.Bilinear)
        {
            if (texture == null)
            {
                texture = new Texture2D(width, 1);
            }
            else if (texture.width != width)
            {
                texture.Reinitialize(width, 1);
            }
            if (gradient == null)
            {
                gradient = new Gradient();
                gradient.SetKeys(
                    new GradientColorKey[] { new(Color.black, 0), new(Color.black, 1) },
                    new GradientAlphaKey[] { new(1, 0), new(1, 1) }
                );
            }
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = filterMode;

            Color[] cols = new Color[width];
            for (int i = 0; i < cols.Length; i++)
            {
                float t = i / (cols.Length - 1f);
                cols[i] = gradient.Evaluate(t);
            }
            texture.SetPixels(cols);
            texture.Apply();
        }
    }
}