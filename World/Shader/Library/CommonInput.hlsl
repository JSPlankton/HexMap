#ifndef COMMON_INPUT_INCLUDE
#define COMMON_INPUT_INCLUDE

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"

struct AttributesTerrainLighting
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    float2 texcoord     : TEXCOORD0;
    float3 terrain      : TEXCOORD2;
    float4 color        : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct AttributesTerrainLightingUV2
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    float2 texcoord     : TEXCOORD0;
    float2 texcoord2     : TEXCOORD1;
    float3 terrain      : TEXCOORD2;
    float4 color        : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

#endif