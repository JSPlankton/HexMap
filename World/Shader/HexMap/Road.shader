Shader "JS/Env/Road"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (0.61,0.61,0.61,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+1" }
		LOD 200
		Offset -1, -1
        
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            // func switch
            #pragma multi_compile _ HEX_MAP_EDIT_MODE

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 posWS : TEXCOORD1;
                float2 visibility : TEXCOORD2;
                float4 posCS : SV_POSITION;
            };

            #include "Assets/World/Shader/Library/CommonInput.hlsl"
            #include "Assets/World/Shader/Library/HexCellData.hlsl"

            sampler2D _MainTex;

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            half4 _Color;
            CBUFFER_END

            v2f vert (AttributesTerrainLighting v)
            {
                v2f o;
                o.posCS = UnityObjectToClipPos(v.positionOS);
                o.posWS = mul(unity_ObjectToWorld, v.positionOS).xyz;
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);

	            float4 cell0 = GetCellData(v, 0);
	            float4 cell1 = GetCellData(v, 1);

			    o.visibility.x = cell0.x * v.color.x + cell1.x * v.color.y;
			    o.visibility.x = lerp(0.25, 1, o.visibility.x);
			    o.visibility.y = cell0.y * v.color.x + cell1.y * v.color.y;
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                half4 noise = tex2D(_MainTex, i.posWS.xz * 0.025);
                half blend = i.uv.x;
                blend *= noise.x + 0.5;
                blend = smoothstep(0.4, 0.7, blend);

                _Color.a = blend;
                half4 finalCol = _Color * ((noise.y * 0.75 + 0.25) * i.visibility.x);

                float explored = i.visibility.y;
                finalCol.a *= explored;
                
                return finalCol;
            }
            ENDCG
        }
    }
}
