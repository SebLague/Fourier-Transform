Shader "Vis/Triangle"
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

			struct TriangleData
			{
				float2 posA;
				float2 posB;
				float2 posC;
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

			StructuredBuffer<TriangleData> InstanceData;
			uint InstanceOffset;
			float2 Offset;
			float Scale;

			v2f vert(appdata v, uint instanceID : SV_InstanceID)
			{
				TriangleData instance = InstanceData[instanceID + InstanceOffset];

				float2 posA = instance.posA * Scale + Offset;
				float2 posB = instance.posB * Scale + Offset;
				float2 posC = instance.posC * Scale + Offset;
				float2 pos = lerp(lerp(posA, posB, v.uv[1]), posC, v.uv[0]);
				float3 worldVertPos = float3(pos, 0);
				float3 objectVertPos = mul(unity_WorldToObject, float4(worldVertPos.xyz, 1));

				v2f o;
				o.vertex = UnityObjectToClipPos(objectVertPos);
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