Shader "Custom/Height Map Volume Based"
{
	Properties
	{
		_MainTex("Base (RGB)", 2D) = "white" {}
		_HeightMapBuffer("Height Map Buffer", 2D) = "white" {}
		_Height_1("Height 1", Float) = 0
		_Height_2("Height 2", Float) = 2
		_Height_3("Height 3", Float) = 2.06
		_Height_4("Height 4", Float) = 2.08
		_Height_5("Height 5", Float) = 2.1
		_Height_6("Height 6", Float) = 2.3
		_Fade("Fade", Float) = 0.01
		_Color_1("Tint Color 1", Color) = (1,0,1,1) //purple 230 0 255
		_Color_2("Tint Color 2", Color) = (0,0,1,1) //blue 0 2 255
		_Color_3("Tint Color 3", Color) = (0,1,0,1) //green 0 255 86
		_Color_4("Tint Color 4", Color) = (1,0.5,0,1) //orange 255 153 0
		_Color_5("Tint Color 5", Color) = (1,0,0,1) //red  255 0 0 
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
		float _Height_4;
		float _Height_5;
		float _Height_6;
		float _Fade;
		fixed4 _Color_1;
		fixed4 _Color_2;
		fixed4 _Color_3;
		fixed4 _Color_4;
		fixed4 _Color_5;
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

		void surf(Input IN, inout SurfaceOutput o)
		{
			//IN.worldPos = mul(_CustomMatrix, float4(IN.worldPos + _Translation, 0));

			IN.worldPos.y += _Height_2;
			IN.worldPos.y -= _CamberOrigin.y - _HeightBufferRange;
			IN.worldPos.y -= tex2D(_HeightMapBuffer, IN.screenPos.xy / IN.screenPos.w).a * _HeightBufferRange;

			half4 c = tex2D(_MainTex, IN.uv_MainTex);
			float h = (_Height_2 - IN.worldPos.y) / (_Height_2 - _Height_1);
			float t;
			t = h;
			fixed4 tintColor; 
			tintColor = _Color_default;//default black
			float curr_h = IN.worldPos.y;
			
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
			else if (curr_h >= _Height_3 - _Fade && curr_h < _Height_3 + _Fade ) {
				float h = (_Height_3 + _Fade - IN.worldPos.y) / ( 2 *_Fade);
			    tintColor = lerp(_Color_3.rgba, _Color_2.rgba, h);
			}
			else if (curr_h >= _Height_3 + _Fade && curr_h < _Height_4 - _Fade ) {
				tintColor = _Color_3.rgba;
			}
			else if (curr_h >= _Height_4 - _Fade && curr_h < _Height_4 + _Fade ) {
				float h = (_Height_4 + _Fade - IN.worldPos.y) / ( 2 *_Fade);
			    tintColor = lerp(_Color_4.rgba, _Color_3.rgba, h);
			}
			else if (curr_h >= _Height_4 + _Fade && curr_h < _Height_5 - _Fade ) {
				tintColor = _Color_4.rgba;
			}
			else if (curr_h >= _Height_5 - _Fade && curr_h < _Height_5 + _Fade ) {
				float h = (_Height_5 + _Fade - IN.worldPos.y) / ( 2 *_Fade);
			    tintColor = lerp(_Color_5.rgba, _Color_4.rgba, h);
			}
			else if (curr_h >= _Height_5 + _Fade && curr_h < _Height_6 - _Fade ) {
				tintColor = _Color_5.rgba;
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