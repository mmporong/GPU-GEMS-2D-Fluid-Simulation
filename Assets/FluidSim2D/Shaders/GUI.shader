// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'


Shader "FluidSim/GUI" 
{
	Properties 
	{
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}
	SubShader 
	{
    	Pass 
    	{
			ZTest Always

			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			
			sampler2D _MainTex;
			sampler2D _Obstacles;
			sampler2D _Temperature;
			float3 _FluidColor, _ObstacleColor;
		
			struct v2f 
			{
    			float4  pos : SV_POSITION;
    			float2  uv : TEXCOORD0;
			};

			v2f vert(appdata_base v)
			{
    			v2f OUT;
    			OUT.pos = UnityObjectToClipPos(v.vertex);
    			OUT.uv = v.texcoord.xy;
    			return OUT;
			}
			
			float4 frag(v2f IN) : COLOR
			{
			 	float density = tex2D(_MainTex, IN.uv).x;
			 	float temperature = tex2D(_Temperature, IN.uv).x;
			 	float obs = tex2D(_Obstacles, IN.uv).x;
			 	
			 	// 온도에 따른 색상 변화 (파란색 -> 보라색)
			 	float3 coldColor = float3(0, 0, 1); // 파란색
			 	float3 hotColor = float3(0.5, 0, 0.5); // 보라색
			 	float3 fluidColor = lerp(coldColor, hotColor, saturate(temperature * 0.1));
			 	
			 	float3 col = fluidColor * density;
			 	float3 result = lerp(col, _ObstacleColor, obs);
			 	
				return float4(result,1);
			}
			
			ENDCG

    	}
	}
}
