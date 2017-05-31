Shader "Custom/Height Map Single Parallel"
{
	Properties
	{
		_MainTex("Base (RGB)", 2D) = "white" {}
		_HeightMapBuffer("Height Map Buffer", 2D) = "white" {}
		_Height_1("Height 1", Float) = 0
		_Height_2("Height 2", Float) = 2
		_Height_3("Height 3", Float) = 2.06

		_Fade("Fade", Float) = 0.01

		_Color_1("Tint Color 1", Color) = (1,0,1,1) //purple 230 0 255
		_Color_2("Tint Color 2", Color) = (0,0,1,1) //blue 0 2 255
		_Color_3("Tint Color 3", Color) = (0,1,0,1) //green 0 255 86

		_Color_default("Tint Color default", Color) = (0,0,0,1) //black 0 0 0

		_HeightBufferRange("Height Buffer Range", Float) = 2
		_StartingHeightOffset("StartingHeightOffset", Float) = 0
	}

		SubShader
	{
		Tags{ "RenderType" = "Opaque" }

		CGPROGRAM
// Upgrade NOTE: excluded shader from OpenGL ES 2.0 because it uses non-square matrices
#pragma exclude_renderers gles
		#pragma surface surf Lambert

		sampler2D _MainTex;
		sampler2D _HeightMapBuffer;
		float3 _ReferenceOrigin;
		float3 _ReferenceNormal;
		float3 _CamberOrigin;
		float _PlaneSeparationDistance;
		//fixed4 _ColorMin;
		//fixed4 _ColorMax;
		//float _HeightMin;
		//float _HeightMax;

		float _Height_1;
		float _Height_2;
		float _Height_3;

		float _Fade;

		fixed4 _Color_1;
		fixed4 _Color_2;
		fixed4 _Color_3;

		fixed4 _Color_default;

		float _HeightBufferRange;
		float _StartingHeightOffset;

		float min_height = 0;
		float max_height = 0;
		
		struct Input
		{
			float2 uv_MainTex;
			float3 worldPos;
			float4 screenPos;
		};

		// This one doesn't really work. Tried to do the math using the two planes and treat it almost like a parallel camera frustum
		void surf(Input IN, inout SurfaceOutput o)
		{
			// Normalizing the IN.worldPos from a grid cube into our oblique volume to find its height in volume's space
			float h = _PlaneSeparationDistance;
			float3 n = _ReferenceNormal;
			float3 v = IN.worldPos - _ReferenceOrigin;
			float d = v.x*n.x + v.y*n.y + v.z*n.z;

			// lerp factor
			float t = d / h;
			float3 pos = _ReferenceOrigin * t + _CamberOrigin * (1.0 - t);
			pos.y += _Height_3;
			IN.worldPos = pos;

			half4 c = tex2D(_MainTex, IN.uv_MainTex);
			h = (_Height_2 - IN.worldPos.y) / (_Height_2 - _Height_1);
			t = h;
			fixed4 tintColor; 
			tintColor = _Color_default;//default black
			float curr_h = IN.worldPos.y;

			//TODO: if statements are really expensive in shaders. Find another way, if possible.
			if (curr_h >= _Height_1 && curr_h < _Height_2 - _Fade) {
				tintColor = _Color_1.rgba;
			}
			else if (curr_h >= _Height_2 - _Fade && curr_h < _Height_2 + _Fade ) {
				float h = (_Height_2 + _Fade - IN.worldPos.y) / ( 2 *_Fade);
			    tintColor = lerp(_Color_2.rgba, _Color_1.rgba, h);
			}
			else if (curr_h >= _Height_2 + _Fade && curr_h < _Height_3 - _Fade ) {
				tintColor = _Color_2.rgba;
			}
			else if (curr_h >= _Height_3 - _Fade) {
				float h = (_Height_3 + _Fade - IN.worldPos.y) / ( 2 *_Fade);
			    tintColor = lerp(_Color_3.rgba, _Color_2.rgba, h);
			}
			
			//fixed4 tintColor = lerp(_ColorMin.rgba, _ColorMax.rgba, t);
			o.Albedo = c.rgb * tintColor.rgb;
			o.Alpha = c.a * tintColor.a;
			//o.Albedo = tintColor.rgb;
			//o.Alpha = tintColor.a;

			if (curr_h < min_height) {
				min_height = curr_h;
			}
			if (curr_h > max_height) {
				max_height = curr_h;
			}
		}
		ENDCG
	}
	Fallback "Diffuse"
}