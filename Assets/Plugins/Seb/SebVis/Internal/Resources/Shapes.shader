Shader "Vis/Shapes"
{
	SubShader
	{
		Tags { "Queue"="Transparent" }
		ZWrite Off
		ZTest Always
		Cull Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "DrawCommon.hlsl"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				int type : TEXCOORD0;
				float2 uv : TEXCOORD1;
				float2 sizeData : TEXCOORD2;
				float4 col : TEXCOORD3;
				float2 worldPos :TEXCOORD4;
				float2 worldPosA :TEXCOORD5;
				float2 worldPosB :TEXCOORD6;
				float4 maskMinMax :TEXCOORD7;
			};//

			struct ShapeData
			{
				int type;
				float2 a;
				float2 b;
				float param;
				float4 col;
				float4 maskMinMax;
			};

			float2 Offset;
			float Scale;

			StructuredBuffer<ShapeData> InstanceData;
			uint InstanceOffset;

			static const int LINE_TYPE = 0;
			static const int POINT_TYPE = 1;
			static const int QUAD_TYPE = 2;
			static const int TRIANGLE_TYPE = 3;
			static const int SATVAL_TYPE = 4;
			static const int HUE_TYPE = 5;
			static const int DIAMOND_TYPE = 6;
			static const int POINT_OUTLINE_TYPE = 7;

			float2 CalculateWorldUnitsPerPixel()
			{
				float2 screenSizeWorld = unity_OrthoParams.xy * 2; // world-space width & height of orthographic camera
				float2 screenSizePixels = _ScreenParams.xy; // Pixel size of screen
				float2 worldUnitsPerPixel = screenSizeWorld / screenSizePixels;
				return worldUnitsPerPixel;
			}

			v2f vert (appdata v, uint instanceID : SV_InstanceID)
			{
				ShapeData instance = InstanceData[instanceID + InstanceOffset];
				v2f o;
				o.type = instance.type;
				o.maskMinMax = instance.maskMinMax;
				o.col = instance.col;
				o.uv = v.uv;
				float3 worldVertPos = 0;

				// Line
				if (instance.type == LINE_TYPE)
				{
					float2 worldPosA = instance.a * Scale + Offset;
					float2 worldPosB = instance.b * Scale + Offset;

					float thickness = abs(instance.param * Scale);
					float2 worldOffset = worldPosB - worldPosA;
					float2 centre = (worldPosA + worldPosB) * 0.5;
					float len = length(worldOffset);
					float2 iHat = worldOffset / len;
					float2 jHat = float2(-iHat.y, iHat.x);

					// Offset the corners of the line quad by a few pixels to allow for antialising even with very thin lines
					float2 aaPad = v.vertex.xy * 2 * CalculateWorldUnitsPerPixel() * 2; // v.vertex * 2 gives offset direction

					float3 offset = v.vertex * float4(len + thickness * 2, thickness * 2, 1, 0);
					float3 offset2 = float3(iHat * (offset.x + aaPad.x) + jHat * (offset.y + aaPad.y), offset.z);
					worldVertPos = float3(centre, 0) + offset2;
					
					o.sizeData = float2(len, thickness);
					o.worldPosA = centre - iHat * len / 2;
					o.worldPosB = centre + iHat * len / 2;
				}
				// Point
				else if (instance.type == POINT_TYPE || instance.type == POINT_OUTLINE_TYPE)
				{
					float3 worldCentre = float3(instance.a * Scale + Offset, 0);
					float4 size = float4(instance.b.xy * 2 * Scale, 1, 1);
					worldVertPos = worldCentre + v.vertex * size;
					if (instance.type == POINT_OUTLINE_TYPE)
					{
						o.sizeData = instance.param; // inner radius (as value between 0 and 1)
					}
				}
				// Quad (2 = regular, 4 = saturation/value display, 5 = hue display)
				else if (instance.type == QUAD_TYPE || instance.type == SATVAL_TYPE || instance.type == HUE_TYPE)
				{
					float3 worldCentre = float3(instance.a * Scale + Offset, 0);
					float4 size = float4(instance.b.xy * Scale, 1, 1);
					worldVertPos = worldCentre + v.vertex * size;
					if (instance.type == 4) o.col = instance.param; // Override col to store hue value
				}
				// Triangle
				else if (instance.type == TRIANGLE_TYPE)
				{
					uint uintInput = asuint(instance.param);
					float cx = f16tof32(uintInput >> 16);
					float cy = f16tof32(uintInput);

					float2 posA = instance.a * Scale + Offset;
					float2 posB = instance.b * Scale + Offset;
					float2 posC = float2(cx, cy) * Scale + Offset;
					float2 pos = lerp(lerp(posA, posB, v.uv[1]), posC, v.uv[0]);
					worldVertPos = float3(pos, 0);
				}
				// Diamond
				else if (instance.type == DIAMOND_TYPE)
				{
					float3 worldCentre = float3(instance.a * Scale + Offset, 0);
					float2 size = instance.b.xy * Scale;
					worldVertPos = float4(worldCentre + v.vertex * size, 0, 1);
					o.sizeData = size;
				}
				
				o.worldPos = worldVertPos;
				o.vertex = WorldToClipPos(worldVertPos);
				return o;
			}

			bool inBounds(float2 pos, float2 boundsMin, float2 boundsMax)
			{
				return pos.x >= boundsMin && pos.x <= boundsMax.x && pos.y >= boundsMin.y && pos.y <= boundsMax.y;
			}

			float distanceToLineSegment(float2 p, float2 a1, float2 a2)
			{
				float2 lineDelta = a2 - a1;
				float sqrLineLength = dot(lineDelta, lineDelta);

				if (sqrLineLength == 0) {
					return a1;
				}

				float2 pointDelta = p - a1;
				float t = saturate(dot(pointDelta, lineDelta) / sqrLineLength);
				float2 pointOnLineSeg = a1 + lineDelta * t;
				return length(p - pointOnLineSeg);
			}

			float4 lineDraw(v2f i)
			{
				float pixelSize = ddx(i.worldPos.x);
				float len = i.sizeData.x;
				float thickness = i.sizeData.y;

				float sum = 0;

				// Do 3x3 grid of samples for antialising
				for (int yo = -1; yo <= 1; yo++)
				{
					for (int xo = -1; xo <= 1; xo++)
					{
						float2 offset = float2(xo, yo) * pixelSize / 3;
						float2 samplePos = i.worldPos + offset;

						float dst = distanceToLineSegment(samplePos, i.worldPosA, i.worldPosB);
						// Ad-hoc
						sum += 1 - smoothstep(0, pixelSize, dst - thickness);
						//if (dst < thickness) sum++;
					}
				}

				float alpha = sum / (3 * 3);
				return float4(i.col.rgb, i.col.a * alpha);
			}

			float4 circleDraw(v2f i)
			{
				// Calculate distance from centre of quad (dst > 1 is outside circle)
				float2 centreOffset = (i.uv.xy - 0.5) * 2;
				float sqrDst = dot(centreOffset, centreOffset);
				float dst = sqrt(sqrDst);

				// Smoothly blend from 0 to 1 alpha across edge of circle
				float delta = fwidth(dst);
				float alpha = 1 - smoothstep(1 - delta, 1 + delta, sqrDst);

				return float4(i.col.rgb, alpha * i.col.a);
			}

			float4 circleOutlineDraw(v2f i)
			{
				float innerRadiusT = i.sizeData.x;

				// Calculate distance from centre of quad (dst > 1 is outside circle)
				float2 centreOffset = (i.uv.xy - 0.5) * 2;
				float sqrDst = dot(centreOffset, centreOffset);
				float dst = sqrt(sqrDst);

				// Smoothly blend from 0 to 1 alpha across edge of circle
				float delta = fwidth(dst);
				float alpha = max(dst - 1, innerRadiusT - dst);
				alpha = 1-smoothstep(-delta, +delta, alpha);

				return float4(i.col.rgb, alpha * i.col.a);
			}

			float4 quadDraw(v2f i)
			{
				return i.col;
			}

			float4 hueQuadDraw(v2f i)
			{
				float3 hsv = float3(i.uv.y, 1, 1);
				float3 col = hsv_to_rgb(hsv);
				return float4(col, 1);
			}

			float4 satValQuadDraw(v2f i)
			{
				float hue = i.col[0];
				float3 hsv = float3(hue, i.uv[0], i.uv[1]);
				float3 col = hsv_to_rgb(hsv);
				return float4(col, 1);
			}

			float4 diamondDraw(v2f i)
			{
				float2 size = i.sizeData;
				float2 p = abs(i.uv - 0.5) * size;

				if (size.x < size.y)
				{
					size.xy = size.yx;
					p.xy = p.yx;
				}

				p.x = size.x * 0.5 - p.x;
				float a = p.x > p.y;
				return float4(i.col.rgb, i.col.a * a);
			}


			float4 triangleDraw(v2f i)
			{
				return i.col;
			}

			float4 frag(v2f i) : SV_Target
			{
				// Mask
				if (!inBounds(i.worldPos, i.maskMinMax.xy * Scale + Offset, i.maskMinMax.zw * Scale + Offset)) return 0;

				if (i.type == LINE_TYPE) return lineDraw(i);
				else if (i.type == POINT_TYPE) return circleDraw(i);
				else if (i.type == POINT_OUTLINE_TYPE) return circleOutlineDraw(i);
				else if (i.type == QUAD_TYPE) return quadDraw(i);
				else if (i.type == TRIANGLE_TYPE) return triangleDraw(i);
				else if (i.type == SATVAL_TYPE) return satValQuadDraw(i);
				else if (i.type == HUE_TYPE) return hueQuadDraw(i);
				else if (i.type == DIAMOND_TYPE) return diamondDraw(i);
				return float4(0,0,0,1);
			}
			ENDCG
		}
	}
}
