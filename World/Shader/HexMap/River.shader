Shader "JS/Env/River"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (0.61,0.61,0.61,1)
    }
    SubShader
    {
        //河流始终在水体之上，这样瀑布效果才能正确
        Tags { "RenderType"="Transparent" "Queue"="Transparent+1" }
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
                float visibility : TEXCOORD1;
                float4 vertex : SV_POSITION;
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
                o.vertex = UnityObjectToClipPos(v.positionOS);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);

                float4 cell0 = GetCellData(v, 0);
		        float4 cell1 = GetCellData(v, 1);

		        o.visibility = cell0.x * v.color.x + cell1.x * v.color.y;
		        o.visibility = lerp(0.25, 1, o.visibility);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float river = River(i.uv, _MainTex);
                half4 finalCol = saturate(_Color + river);
                finalCol.rgb *= i.visibility;
                
                return finalCol;
            }
            ENDCG
        }
    }
}
