Shader "Custom/Camber Volume Height Map"
{
	Properties
	{
		// this world vertical range gets squashed into 0 to 1 alpha buffer 
		_StartY("Start Height", Float) = 0 
		_EndY("End Height", Float) = 2 //the lower this range the more accurate the color will be
	}

	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		ZWrite Off
		Blend Off

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float3 worldPos : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			float _StartY;
			float _EndY;

			v2f vert (appdata v)
			{
				v2f o;
				o.worldPos = mul (unity_ObjectToWorld, v.vertex);
                o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{	
				// all we're doing here is rendering white with alpha that respresents the height adjustment within the height of the camber volume
				// if there's A LOT of rotation then _EndY and _StartY should be minY and maxY of the mesh's bounding box 

				// don't do anything if the height range is less than  1mm 
				float heightRange = _EndY - _StartY;
				if (heightRange < 0.001f)
					return float4(0,0,0,0);

				float height = (i.worldPos.y - _StartY) / heightRange;
				fixed4 col = float4(1,1,1, height);
				col.rgb *= col.a;
				return col;
			}
			ENDCG
		}
	}
}
