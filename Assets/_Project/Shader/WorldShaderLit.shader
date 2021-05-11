// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/WorldShaderLit"
{
	Properties
	{
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo (RGB)", 2DArray) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
	}

	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 200
		Cull Back

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Diffuse fullforwardshadows vertex:vert

		// Use shader model 3.5 target, to get nicer looking lighting and texture array support
		#pragma target 3.5

		// texture arrays are not available everywhere,
		// only compile shader on platforms where they are
		#pragma require 2darray

		UNITY_DECLARE_TEX2DARRAY(_MainTex);

		struct Input
		{
			float2 uv_MainTex;
			float arrayIndex; // cannot start with “uv”
			float4 color: COLOR; // TODO could remove this if not using VertexColor and Texture2DArray together
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

		half4 LightingDiffuse(SurfaceOutput s, half3 lightDir, half atten)
		{
			s.Normal = normalize(s.Normal);

			half3 nld = normalize(lightDir);

			half diff = max(0, dot(nld, s.Normal));

			half4 c;
			c.rgb = (s.Albedo * diff * _LightColor0.rgb) * atten;
			c.a = s.Alpha;
			return c;
		}

		void vert(inout appdata_full v, out Input o)
		{
			o.uv_MainTex = v.texcoord.xy;
			o.arrayIndex = v.texcoord.z;
			o.color = v.color;
		}

		void surf(Input IN, inout SurfaceOutput o)
		{
			// Albedo comes from a texture tinted by color
			fixed4 c = UNITY_SAMPLE_TEX2DARRAY(_MainTex, float3(IN.uv_MainTex, IN.arrayIndex)) * _Color;
			o.Albedo = c.rgb * IN.color; // Combine normal color with the vertex color
			o.Alpha = c.a;
		}
		ENDCG
	}

	FallBack "Diffuse"
}
