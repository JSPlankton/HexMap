Shader "JS/Env/Terrain"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Terrain Texture Array", 2DArray) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque"}
        LOD 200

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            #pragma target 3.5

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float3 terrain : TEXCOORD2;
            };

            struct v2f
            {
                float4 posCS : SV_POSITION;
                float3 posWS : TEXCOORD0;
                float3 terrain : TEXCOORD1;
                float4 color : Color;

                SHADOW_COORDS(2)//仅仅是阴影
            };

            UNITY_DECLARE_TEX2DARRAY(_MainTex);
            float4 _MainTex_ST;
            CBUFFER_START(UnityPerMaterial)
            float4 _Color;
            CBUFFER_END

	        float4 GetTerrainColor (v2f i, int index) {
		        float3 uvw = float3(i.posWS.xz * 0.02, i.terrain[index]);
		        float4 c = UNITY_SAMPLE_TEX2DARRAY(_MainTex, uvw);
		        return c * i.color[index];
	        }

            v2f vert (appdata v)
            {
                v2f o;
                o.posCS = UnityObjectToClipPos(v.vertex);
                o.posWS = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.color = v.color;
	            o.terrain = v.terrain;
                TRANSFER_SHADOW(o);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
			    half4 col =
				    GetTerrainColor(i, 0) +
				    GetTerrainColor(i, 1) +
				    GetTerrainColor(i, 2);
                // UNITY_LIGHT_ATTENUATION(atten, i, i.posWS);
                // fixed shadow = SHADOW_ATTENUATION(i);
                // col.rgb *= i.color;
                // col *= shadow;
                
                return col * _Color;
                // return half4(i.terrain.xyz, 1);
            }
            ENDCG
        }

        
    }
}
