Shader "Custom/Wave"
{
	Properties
	{
		_BaseColor("Base Color", Color) = (1,1,1,1)
		_OutlineColor("Outline Color", Color) = (1,1,1,0.5)
		_OffsetAmount("Offset Amount", Float) = 0.1
		_Speed("Oscillation Speed", Float) = 2.0
	}

	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		CULL Off
		ZWrite Off
		Pass
		{
			Name "BasePass"
			Tags { "LightMode" = "ForwardBase" }
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			struct appdata_t {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};
			struct v2f {
				float4 pos : SV_POSITION;
			};
			fixed4 _BaseColor;
			v2f vert(appdata_t v) {
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				return o;
			}
			fixed4 frag(v2f i) : SV_Target {
				return _BaseColor;
			}
			ENDCG
		}
		Pass
		{
			Name "OutlinePass"
			Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
			Blend SrcAlpha OneMinusSrcAlpha
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct appdata_t {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct v2f {
				float4 pos : SV_POSITION;
				float cycle : NORMAL;
			};

			float _OffsetAmount;
			float _Speed;
			fixed4 _OutlineColor;

			v2f vert(appdata_t v) {
				v2f o;
				float t = _Time.y * _Speed;
				float cycle = t - floor(t);
				float timeOscillation = sin(cycle*1.55) * _OffsetAmount;
				v.vertex.xyz += v.normal * timeOscillation;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.cycle = cycle;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target{
				float4 col = _OutlineColor;
				col.a *= (1 - i.cycle);
				return col;
			}

			ENDCG
		}

		Pass
		{
			Name "OutlinePass"
			Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
			Blend SrcAlpha OneMinusSrcAlpha
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct appdata_t {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
			};

			struct v2f {
				float4 pos : SV_POSITION;
				float cycle : NORMAL;
			};

			float _OffsetAmount;
			float _Speed;
			fixed4 _OutlineColor;

			v2f vert(appdata_t v) {
				v2f o;
				float t = _Time.y * _Speed + 0.5;
				float cycle = t - floor(t);
				float timeOscillation = sin(cycle * 1.55) * _OffsetAmount;
				v.vertex.xyz += v.normal * timeOscillation;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.cycle = cycle;
				return o;
			}

			fixed4 frag(v2f i) : SV_Target{
				float4 col = _OutlineColor;
				col.a *= (1 - i.cycle);
				return col;
			}

			ENDCG
		}
	}
}