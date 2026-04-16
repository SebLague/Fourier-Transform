Shader "Unlit/Spectrogram"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
        }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };


            sampler2D EditedSpectrogram;
            sampler2D PaintMap;
            sampler2D GradientTex;

            int useGradient;
            float maxFrequencyInSpectrum;
            float frequencyDisplayMin;
            float frequencyDisplayMax;
            float decibelsDisplayMin;
            float amplitudeMax;
            int normalizeAmplitude;
            float decibelsDisplayMax;
            int paintMode;
            int useDimView;


            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float AmplitudeToDecibels(float amplitude)
            {
                return 20 * log10(amplitude);
            }


            float Remap01(float val, float minVal, float maxVal)
            {
                return saturate((val - minVal) / (maxVal - minVal));
            }

            float4 DisplayGreyscale(float amplitude)
            {
                float db = AmplitudeToDecibels(amplitude);
                float decibelsT = Remap01(db, decibelsDisplayMin, decibelsDisplayMax);
                return 1 - pow(decibelsT, 2.2);
            }

            float4 DisplayGradient(float amplitude)
            {
                float db = AmplitudeToDecibels(amplitude);
                float decibelsT = Remap01(db, decibelsDisplayMin, decibelsDisplayMax);
                return tex2D(GradientTex, float2(decibelsT, 0.5));
            }

            float4 DisplayFromAmplitude(float amplitude)
            {
                if (useGradient) return DisplayGradient(amplitude);
                else return DisplayGreyscale(amplitude);
            }

            // Spectrogram shader
            float4 frag(v2f i) : SV_Target
            {
                float timeT = i.uv.x;
                float freqT = i.uv.y;

                // Remap frequency to display range
                float frequency = lerp(frequencyDisplayMin, frequencyDisplayMax, freqT);
                freqT = frequency / maxFrequencyInSpectrum;

                // Look up amplitude at current pixel
                float amplitude = tex2D(EditedSpectrogram, float2(timeT, freqT));
                if (normalizeAmplitude) amplitude /= amplitudeMax;
                float amplitudePaint = tex2D(PaintMap, float2(timeT, freqT));

                // ---- Display ----
                float3 displayCol = 0;

                // Paint mode: OFF
                if (paintMode == 0)
                {
                    displayCol = DisplayFromAmplitude(amplitude);
                }
                // Paint mode: OVERLAY
                else if (paintMode == 1)
                {
                    float3 spectrogram = DisplayGreyscale(amplitude).rgb;
                    float3 paint = DisplayGreyscale(amplitudePaint).rgb;
                    paint = float3(1 - paint.r, 0, 0);

                    float3 col = lerp(spectrogram, paint, saturate(amplitudePaint * 2));
                    displayCol = col;
                }
                // Paint mode: EXCLUSIVE
                else
                {
                    displayCol = DisplayFromAmplitude(amplitudePaint);
                }


                // Output
                if (useDimView) displayCol *= 0.3f;
                return float4(displayCol, 1);
            }
            ENDCG
        }
    }
}