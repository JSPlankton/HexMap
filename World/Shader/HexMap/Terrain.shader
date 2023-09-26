Shader "JS/Env/TerrainLit"
{
    Properties
    {
        _BaseMaps("Terrain Texture Array", 2DArray) = "white" {}
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)
        [Normal]_BumpMaps("Terrain Bump Texture Array", 2DArray) = "white" {}
        
        _GridTex("Grid Texture", 2D) = "white" {}
        
        _Metallic("Metallic",Range(0.0,1.0)) = 1.0
        _Smoothness("_Smoothness",Range(0.0,1.0)) = 1.0
        
        _BackgroundColor ("Background Color", Color) = (0,0,0)
        [Toggle(SHOW_MAP_DATA)] _ShowMapData ("Show Map Data", Float) = 0
    }

    SubShader
    {
        // Universal Pipeline tag is required. If Universal render pipeline is not set in the graphics settings
        // this Subshader will fail. One can add a subshader below or fallback to Standard built-in to make this
        // material work with both Universal Render Pipeline and Builtin Unity Pipeline
        Tags{"RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "UniversalMaterialType" = "Lit" "IgnoreProjector" = "True" "ShaderModel"="4.5"}
        LOD 300

        // ------------------------------------------------------------------
        //  Forward pass. Shades all light in a single pass. GI + emission + Fog
        Pass
        {
            Name "ForwardLit"
            Tags{"LightMode" = "UniversalForward"}

//            ZWrite[_ZWrite]
            Cull[_Cull]

            HLSLPROGRAM
            #pragma exclude_renderers gles gles3 glcore
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            #include "Assets/World/Shader/Library/CommonInput.hlsl"
            #include "Assets/World/Shader/Library/StandardLighting.hlsl"

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            
            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #pragma multi_compile _ DOTS_INSTANCING_ON

            // func switch
            #pragma multi_compile _ GRID_ON
            #pragma multi_compile _ HEX_MAP_EDIT_MODE

            #pragma shader_feature SHOW_MAP_DATA

            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment
            

            struct Varyings
            {
                float2 uv                       : TEXCOORD0;
                float3 positionWS               : TEXCOORD1;
                float3 normalWS                 : TEXCOORD2;
                half4 tangentWS                 : TEXCOORD3;
                float3 terrain                  : TEXCOORD4;
                float4 visibility               : TEXCOORD5;
                #if defined(SHOW_MAP_DATA)
				float mapData                   : TEXCOORD6;
			    #endif
                float4 positionCS               : SV_POSITION;
                float4 color                    : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            #include "Assets/World/Shader/Library/HexCellData.hlsl"

            TEXTURE2D_ARRAY(_BaseMaps);
            SAMPLER(sampler_BaseMaps);

            TEXTURE2D_ARRAY(_BumpMaps);
            SAMPLER(sampler_BumpMaps);

            TEXTURE2D(_GridTex);
            SAMPLER(sampler_GridTex);
            
            float4 _BaseMaps_ST;
            float4 _BumpMaps_ST;

            half3 _BackgroundColor;

            float4 GetTerrainColor (Varyings input, int index) {
		        float3 uvw = float3(input.positionWS.xz * 0.02 * _BaseMaps_ST.xy, input.terrain[index]);
		        float4 c = _BaseMaps.Sample(sampler_BaseMaps, uvw);
		        return c * (input.color[index] * input.visibility[index]);
	        }

            float3 GetTerrainNormal (Varyings input, int index) {
                float3 uvw = float3(input.positionWS.xz * 0.02 * _BumpMaps_ST.xy, input.terrain[index]);
                half3 NormalTS = UnpackNormalScale(_BumpMaps.Sample(sampler_BumpMaps, uvw), 1.0);
		        return NormalTS;
	        }

            float4 GetTerrainNormalRGB (Varyings input, int index) {
                float3 uvw = float3(input.positionWS.xz * 0.02 * _BumpMaps_ST.xy, input.terrain[index]);
                return _BumpMaps.Sample(sampler_BumpMaps, uvw);
	        }

            // Used in Standard (Physically Based) shader
            Varyings LitPassVertex(AttributesTerrainLighting input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.uv = input.texcoord;
                output.normalWS = normalInput.normalWS;
                real sign = input.tangentOS.w * GetOddNegativeScale();
                half4 tangentWS = half4(normalInput.tangentWS.xyz, sign);
                output.tangentWS = tangentWS;
                
                output.positionWS = vertexInput.positionWS;
                output.positionCS = vertexInput.positionCS;

		        float4 cell0 = GetCellData(input, 0);
		        float4 cell1 = GetCellData(input, 1);
		        float4 cell2 = GetCellData(input, 2);

		        output.terrain.x = cell0.w;
		        output.terrain.y = cell1.w;
		        output.terrain.z = cell2.w;

                output.visibility.x = cell0.x;
			    output.visibility.y = cell1.x;
			    output.visibility.z = cell2.x;
                output.visibility.xyz = lerp(0.25, 1, output.visibility.xyz);
                output.visibility.w =
				cell0.y * input.color.x + cell1.y * input.color.y + cell2.y * input.color.z;

                #if defined(SHOW_MAP_DATA)
				output.mapData = cell0.z * input.color.x + cell1.z * input.color.y +
					cell2.z * input.color.z;
			    #endif

                
                output.color = input.color;

                return output;
            }

            void LitPassFragment(Varyings input
                , out half4 outColor : SV_Target0
            #ifdef _WRITE_RENDERING_LAYERS
                , out float4 outRenderingLayers : SV_Target1
            #endif
            )
            {
                UNITY_SETUP_INSTANCE_ID(input);
                //输入数据
                float2 uv = input.uv;
                float3 worldPos = input.positionWS;
                half3 viewDir = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half3 worldNormal = normalize(input.normalWS);
                half3 worldTangent = normalize(input.tangentWS.xyz);
                half3 worldBinnormal = normalize(cross(worldNormal, worldTangent) * input.tangentWS.w);
                half3x3 TBN = half3x3(worldTangent, worldBinnormal, worldNormal);

                half3 NormalTS = GetTerrainNormal(input, 0) +
				    GetTerrainNormal(input, 1) +
				    GetTerrainNormal(input, 2);

                half4 NormalRGB = GetTerrainNormalRGB(input, 0) +
				GetTerrainNormalRGB(input, 1) +
				GetTerrainNormalRGB(input, 2);

                worldNormal = normalize(mul(NormalTS,TBN));
                
                //材质参数
			    half3 baseColor =
				    GetTerrainColor(input, 0) +
				    GetTerrainColor(input, 1) +
				    GetTerrainColor(input, 2);

                half roughness = (1 - NormalRGB.b) * _Smoothness;
                roughness = max(roughness,0.001f);
                half metallic = NormalRGB.a * _Metallic;
                //BRDF
                half3 diffuseColor = lerp(baseColor * _BaseColor, float3(0.0,0.0,0.0), metallic);
                half3 specularColor = lerp(float3(0.04,0.04,0.04), baseColor, metallic);
                
                //格子绘制
            	float4 gridColor = 1;
                #if defined(GRID_ON)
                    float2 gridUV = worldPos.xz;
                    gridUV.x *= 1 / (4 * 8.66025404);
                    gridUV.y *= 1 / (2 * 15.0);
                    float4 gridColor = _GridTex.Sample(sampler_GridTex, gridUV);
                    diffuseColor *= gridColor;
                #endif
                
                float explored = input.visibility.w;
                diffuseColor *= explored;
                specularColor *= explored;

                float ao = 1;
                ao *= explored;

                #if defined(SHOW_MAP_DATA)
				diffuseColor = input.mapData * gridColor;
				#endif

                //主光源
                half3 directLighting = half3(0, 0, 0);
                DirectLighting_float(diffuseColor, specularColor, roughness, worldPos, worldNormal, viewDir, directLighting);

                half3 indirectLighting = half3(0,0,0);
                IndirectLighting_float(diffuseColor,specularColor,roughness,worldPos,worldNormal,viewDir,ao,0,indirectLighting);

                outColor = half4(directLighting + indirectLighting, 1.0f);

                outColor.xyz += (_BackgroundColor * (1 - explored));
            }
            

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            #pragma exclude_renderers gles gles3 glcore
            #pragma target 4.5

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            // -------------------------------------
            // Universal Pipeline keywords

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

            // This is used during shadow map generation to differentiate between directional and punctual light shadows, as they use different formulas to apply Normal Bias
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"}

            ZWrite On
            ColorMask R
            Cull[_Cull]

            HLSLPROGRAM
            #pragma exclude_renderers gles gles3 glcore
            #pragma target 4.5

            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        // This pass is used when drawing to a _CameraNormalsTexture texture
        Pass
        {
            Name "DepthNormals"
            Tags{"LightMode" = "DepthNormals"}

            ZWrite On
            Cull[_Cull]

            HLSLPROGRAM
            #pragma exclude_renderers gles gles3 glcore
            #pragma target 4.5

            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local _PARALLAXMAP
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A

            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            // Universal Pipeline keywords
            #pragma multi_compile_fragment _ _WRITE_RENDERING_LAYERS

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing
            #pragma multi_compile _ DOTS_INSTANCING_ON

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitDepthNormalsPass.hlsl"
            ENDHLSL
        }

        // This pass it not used during regular rendering, only for lightmap baking.
        Pass
        {
            Name "Meta"
            Tags{"LightMode" = "Meta"}

            Cull Off

            HLSLPROGRAM
            #pragma exclude_renderers gles gles3 glcore
            #pragma target 4.5

            #pragma vertex UniversalVertexMeta
            #pragma fragment UniversalFragmentMetaLit

            #pragma shader_feature EDITOR_VISUALIZATION
            #pragma shader_feature_local_fragment _SPECULAR_SETUP
            #pragma shader_feature_local_fragment _EMISSION
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            #pragma shader_feature_local_fragment _ _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED

            #pragma shader_feature_local_fragment _SPECGLOSSMAP

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitMetaPass.hlsl"

            ENDHLSL
        }

    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
