Shader "Custom/Height Map Volume Based New"
{
	Properties
	{
		_MainTex("Base (RGB)", 2D) = "white" {}
		_HeightMapBuffer("Height Map Buffer", 2D) = "white" {}

		_Bottom_Height("Bottom Height", Float) = -0.24
		_Target_Height_Min("Target Height Min", Float) = -0.07
		_Target_Height_Max("Target Height Max", Float) = 0.06
		_Top_Height("Top Height", Float) = 0.22

		_Color_default("Tint Color default", Color) = (0,0,0,1) 

		_Lower_Boundary_Color("Lower Boundary Color", Color) = (0,0,0,1)
		_Lower_Color("Lower Color", Color) = (1,0,1,1) 
		_Target_Color("Target Color", Color) = (0,0,1,1) 
		_Upper_Color("Upper Color", Color) = (0,1,0,1) 
		_Upper_Boundary_Color("Upper Boundary Color", Color) = (0,0,0,1)

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

		float _Bottom_Height;
		float _Target_Height_Min;
		float _Target_Height_Max;
		float _Top_Height;

		fixed4 _Lower_Boundary_Color;
		fixed4 _Lower_Color;
		fixed4 _Target_Color;
		fixed4 _Upper_Color;
		fixed4 _Upper_Boundary_Color;

		fixed4 _Color_default;
		float _StartingHeightOffset;

		float min_height = 0;
		float max_height = 0;

		float _HeightBufferRange;
		
		struct Input
		{
			float2 uv_MainTex;
			float3 worldPos;
			float4 screenPos;
		};

		void surf(Input IN, inout SurfaceOutput o)
		{
			// subtract the origin and the buffer height so that the green is on height of camber plane
			IN.worldPos.y -= _CamberOrigin.y - _HeightBufferRange;

			// read and subtract the value that's in the height map buffer texture (while sampling from the texture in screen space)
			IN.worldPos.y -= tex2D(_HeightMapBuffer, IN.screenPos.xy / IN.screenPos.w).a * _HeightBufferRange;

			half4 c = tex2D(_MainTex, IN.uv_MainTex);

			fixed4 tintColor; 
			tintColor = _Color_default;

			float currentHeight = IN.worldPos.y;

			float fade = (_Top_Height - _Bottom_Height) * 0.05;


			if (currentHeight < _Bottom_Height - fade) {	// Solid Dark Blue.
				tintColor = _Lower_Boundary_Color;
			}
			else if (currentHeight >= _Bottom_Height - fade && currentHeight < _Bottom_Height + fade) { // Fade Dark Blue To Solid Blue.
				float h = (_Bottom_Height + fade - IN.worldPos.y) / (2 * fade);
				tintColor = lerp(_Lower_Color.rgba, _Lower_Boundary_Color.rgba, h);
			}
			else if (currentHeight >= _Bottom_Height + fade && currentHeight < _Target_Height_Min - fade) {	// Solid Blue.
				tintColor = _Lower_Color;
			}
			else if (currentHeight >= _Target_Height_Min - fade && currentHeight < _Target_Height_Min + fade) {	// Fade Blue To Green.
				float h = (_Target_Height_Min + fade - IN.worldPos.y) / (2 * fade);
				tintColor = lerp(_Target_Color.rgba, _Lower_Color.rgba, h);
			}
			else if (currentHeight >= _Target_Height_Min + fade && currentHeight <= _Target_Height_Max - fade) {	// Solid Green.
				tintColor = _Target_Color;
			}
			else if (currentHeight >= _Target_Height_Max - fade && currentHeight < _Target_Height_Max + fade) {	// Fade Green To Red.
				float h = (_Target_Height_Max + fade - IN.worldPos.y) / (2 * fade);
				tintColor = lerp(_Upper_Color.rgba, _Target_Color.rgba, h);
			}
			else if (currentHeight >= _Target_Height_Max + fade && currentHeight < _Top_Height - fade) {	// Solid Red.
				tintColor = _Upper_Color;
			}
			else if (currentHeight >= _Top_Height - fade && currentHeight < _Top_Height + fade) {	// Fade Red To Dark Red.
				float h = (_Top_Height + fade - IN.worldPos.y) / (2 * fade);
				tintColor = lerp(_Upper_Boundary_Color.rgba, _Upper_Color.rgba, h);
			}
			else if (currentHeight >= _Top_Height + fade) {	// Solid Dark Red.
				tintColor = _Upper_Boundary_Color;
			}
			
			o.Albedo = c.rgb * tintColor.rgb;
			o.Alpha = c.a * tintColor.a;

			if (currentHeight < min_height) {
				min_height = currentHeight;
			}
			if (currentHeight > max_height) {
				max_height = currentHeight;
			}
		}
		ENDCG
	}
	Fallback "Diffuse"
}