Shader "Vis/UnlitPoint"
{
	SubShader
	{
		Tags { "Queue" = "Transparent" }
		ZWrite Off
		Cull Off
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"
			#include "DrawCommon.hlsl"

			struct PointData
			{
				float2 pos;
				float2 size;
				float4 col;
			};

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				float4 col : TEXCOORD1;
			};

			StructuredBuffer<PointData> InstanceData;
			uint InstanceOffset;
			float2 Offset;
			float Scale;

			v2f vert(appdata v, uint instanceID : SV_InstanceID)
			{
				PointData instance = InstanceData[instanceID + InstanceOffset];

				float3 worldPos = float3(instance.pos * Scale + Offset, 0);
				float4 size = float4(instance.size.xy * 2 * Scale, 1, 1);
				float3 worldVertPos = worldPos + mul(unity_ObjectToWorld, v.vertex * size);
				
				v2f o;
				o.vertex = WorldToClipPos(worldVertPos);
				o.uv = v.uv;
				o.col = instance.col;
			
				return o;
			}


			float4 frag(v2f i) : SV_Target
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


			ENDCG
		}
	}
}