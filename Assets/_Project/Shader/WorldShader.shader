Shader "Custom/WorldShader"{
	Properties{
		_MainTex("Tex", 2DArray) = "" {}
	}

	SubShader{
		Tags {
			"PreviewType" = "Plane"
			"RenderType" = "Opaque"
		}
		LOD 200
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.5
			#pragma multi_compile_fog
			#pragma require 2darray

			#include "UnityCG.cginc"

			struct appdata_t {
				float4 texcoord : TEXCOORD0;
				float4 vertex : POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f {
				float3 texcoord : TEXCOORD0;
				float4 vertex : SV_POSITION;
				UNITY_FOG_COORDS(1)
				UNITY_VERTEX_OUTPUT_STEREO
			};

			float4 _MainTex_ST;
			fixed4 _Color;

			v2f vert(appdata_t v) {
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.texcoord.xyz = v.texcoord.xyz;
				return o;
			}

			UNITY_DECLARE_TEX2DARRAY(_MainTex);

			half4 frag(v2f i) : SV_Target {
				fixed4 col = UNITY_SAMPLE_TEX2DARRAY(_MainTex, i.texcoord.xyz) * _Color;
				return col;
			}
			ENDCG
		}
	}
}