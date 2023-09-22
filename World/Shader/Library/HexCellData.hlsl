#ifndef HEXCELLDATA_INCLUDE
#define HEXCELLDATA_INCLUDE

sampler2D _HexCellData;
float4 _HexCellData_TexelSize;

float4 GetCellData (AttributesTerrainLightingUV2 v, int index)
{
    float2 uv;
    uv.x = (v.terrain[index] + 0.5) * _HexCellData_TexelSize.x;
    float row = floor(uv.x);
    uv.x -= row;
    uv.y = (row + 0.5) * _HexCellData_TexelSize.y;
    float4 data = tex2Dlod(_HexCellData, float4(uv, 0, 0));
    data.w *= 255;
    
    return data;
}

float4 GetCellData (AttributesTerrainLighting v, int index)
{
    float2 uv;
    uv.x = (v.terrain[index] + 0.5) * _HexCellData_TexelSize.x;
    float row = floor(uv.x);
    uv.x -= row;
    uv.y = (row + 0.5) * _HexCellData_TexelSize.y;
    float4 data = tex2Dlod(_HexCellData, float4(uv, 0, 0));
    data.w *= 255;
    
    return data;
}

float4 GetCellData (float2 cellDataCoordinates) {
    float2 uv = cellDataCoordinates + 0.5;
    uv.x *= _HexCellData_TexelSize.x;
    uv.y *= _HexCellData_TexelSize.y;
    return tex2Dlod(_HexCellData, float4(uv, 0, 0));
}

#endif