Shader "Vis/UnlitQuad"
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

			struct QuadData
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

			StructuredBuffer<QuadData> InstanceData;
			uint InstanceOffset;
			float2 Offset;
			float Scale;

			v2f vert(appdata v, uint instanceID : SV_InstanceID)
			{
				QuadData instance = InstanceData[instanceID + InstanceOffset];

				float3 worldCentre = float3(instance.pos * Scale + Offset, 0);
				float4 size = float4(instance.size * Scale, 1, 1);
				float3 worldVertPos = worldCentre + v.vertex * size;

				v2f o;
				o.vertex = WorldToClipPos(worldVertPos);

				o.uv = v.uv;
				o.col = instance.col;
			
				return o;
			}


			float4 frag(v2f i) : SV_Target
			{
				return i.col;
			}


			ENDCG
		}
	}
}