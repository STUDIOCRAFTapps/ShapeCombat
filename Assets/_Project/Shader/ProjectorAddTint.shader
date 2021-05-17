Shader "Projector/AdditiveTint" {
	Properties{
		_Color("Tint Color", Color) = (1,1,1,1)
		_ShadowTex("Cookie", 2D) = "white" {}
	}

		Subshader{
			Tags {
				"RenderType" = "Transparent"
				"Queue" = "Transparent+100"
			}
			Pass {
				ZWrite Off
				Offset -1, -1

		//Fog{ Mode Off }

		ColorMask RGB
		Blend One One

		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#pragma fragmentoption ARB_fog_exp2
		#pragma fragmentoption ARB_precision_hint_fastest
		#include "UnityCG.cginc"

		struct v2f
		{
			float4 pos : SV_POSITION;
			float4 uv : TEXCOORD0;
		};

		sampler2D _ShadowTex;
		float4x4 unity_Projector;

		v2f vert(appdata_tan v)
		{
			v2f o;
			o.pos = UnityObjectToClipPos(v.vertex);
			o.uv = mul(unity_Projector, v.vertex);
			return o;
		}

		fixed4 _Color;
		half4 frag(v2f i) : COLOR
		{
			half4 tex = tex2Dproj(_ShadowTex, i.uv);
			tex.a = 1 - tex.a;
			if (i.uv.w < 0)
			{
				tex = float4(0,0,0,1);
			}
			return _Color * tex;
		}
		ENDCG
	}
	}
}