Shader "JS/Env/Estuaries"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (0.61,0.61,0.61,1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200
        
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "WaterCG.cginc"

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
                float3 posWS : TEXCOORD2;
                float visibility : TEXCOORD3;
                float4 posCS : SV_POSITION;
            };

            #include "Assets/World/Shader/Library/CommonInput.hlsl"
            #include "Assets/World/Shader/Library/HexCellData.hlsl"

            sampler2D _MainTex;

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            half4 _Color;
            CBUFFER_END

            v2f vert (AttributesTerrainLightingUV2 v)
            {
                v2f o;
                o.posCS = UnityObjectToClipPos(v.positionOS);
                o.posWS = mul(unity_ObjectToWorld, v.positionOS).xyz;
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.uv2 = v.texcoord2;

                float4 cell0 = GetCellData(v, 0);
			    float4 cell1 = GetCellData(v, 1);

			    o.visibility = cell0.x * v.color.x + cell1.x * v.color.y;
			    o.visibility = lerp(0.25, 1, o.visibility);
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
			    float shore = i.uv.y;
			    float foam = Foam(shore, i.posWS.xz, _MainTex);
			    float waves = Waves(i.posWS.xz, _MainTex);
			    waves *= 1 - shore;

                float shoreWater = max(foam, waves);
			    float river = River(i.uv2, _MainTex);
			    float water = lerp(shoreWater, river, i.uv.x);

                
                half4 finalCol = saturate(_Color + water);
                finalCol.rgb *= i.visibility;
                
                return finalCol;
            }
            ENDCG
        }
    }
}
