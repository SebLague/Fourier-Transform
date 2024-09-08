Shader "Vis/UnlitSphere"
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

			struct PointData
			{
				float3 pos;
				float radius;
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

			v2f vert(appdata v, uint instanceID : SV_InstanceID)
			{
				PointData pointData = InstanceData[instanceID + InstanceOffset];
				float3 r = pointData.radius * 2;
				float3 worldVertPos = pointData.pos + mul(unity_ObjectToWorld, v.vertex * float4(r, 1));//
				float3 objectVertPos = mul(unity_WorldToObject, float4(worldVertPos.xyz, 1));

				v2f o;//
				o.vertex = UnityObjectToClipPos(objectVertPos);
				o.uv = v.uv;
				o.col = pointData.col;
			
				return o;
			}


			float4 frag(v2f i) : SV_Target
			{
				return float4(i.col.rgb, i.col.a);
			}


			ENDCG
		}
	}
}