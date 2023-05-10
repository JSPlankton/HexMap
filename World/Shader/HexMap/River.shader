Shader "JS/Env/River"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (0.61,0.61,0.61,1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        
        Blend SrcAlpha OneMinusSrcAlpha

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
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            half4 _Color;
            CBUFFER_END

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.uv2 = o.uv;

			    o.uv.x = o.uv.x * 0.0625 + _Time.y * 0.005;
			    o.uv.y -= _Time.y * 0.25;

                o.uv2.x = o.uv2.x * 0.0625 - _Time.y * 0.0052;
			    o.uv2.y -= _Time.y * 0.23;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                half4 noise = tex2D(_MainTex, i.uv);
                half4 noise2 = tex2D(_MainTex, i.uv2);
                
                half4 finalCol = saturate(_Color + (noise.r * noise2.a));
                
                return finalCol;
            }
            ENDCG
        }
    }
}
