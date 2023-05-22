using UnityEngine;

namespace JS.HexMap
{
    public static class HexMetrics {
        public const float outerToInner = 0.866025404f;
        public const float innerToOuter = 1f / outerToInner;
        //六边形外径
        public const float outerRadius = 10f;
        //六边形内径
        public const float innerRadius = outerRadius * 0.866025404f;
        
        public const float solidFactor = 0.8f;
	
        public const float blendFactor = 1f - solidFactor;
        //海拔高度单位高度
        public const float elevationStep = 3f;
            
        public static Vector3[] corners = {
            new Vector3(0f, 0f, outerRadius),
            new Vector3(innerRadius, 0f, 0.5f * outerRadius),
            new Vector3(innerRadius, 0f, -0.5f * outerRadius),
            new Vector3(0f, 0f, -outerRadius),
            new Vector3(-innerRadius, 0f, -0.5f * outerRadius),
            new Vector3(-innerRadius, 0f, 0.5f * outerRadius),
            new Vector3(0f, 0f, outerRadius)
        };
        
        public const int terracesPerSlope = 2;

        public const int terraceSteps = terracesPerSlope * 2 + 1;
        
        public const float horizontalTerraceStepSize = 1f / terraceSteps;
	
        public const float verticalTerraceStepSize = 1f / (terracesPerSlope + 1);
        
        public static Texture2D noiseSource;
        //不规则扰动程度
        public const float cellPerturbStrength = 4f;
        
        public const float noiseScale = 0.003f;
        
        public const float elevationPerturbStrength = 1.5f;
        
        public const int chunkSizeX = 5, chunkSizeZ = 5;
        
        public const float streamBedElevationOffset = -1.75f;

        public const float waterElevationOffset = -0.5f;
        
        public const float waterFactor = 0.6f;
        
        public const float waterBlendFactor = 1f - waterFactor;

        public static Vector4 SampleNoise (Vector3 position) {
            return noiseSource.GetPixelBilinear(
                position.x * noiseScale,
                position.z * noiseScale
            );
        }
	
        public static Vector3 TerraceLerp (Vector3 a, Vector3 b, int step) {
            float h = step * HexMetrics.horizontalTerraceStepSize;
            a.x += (b.x - a.x) * h;
            a.z += (b.z - a.z) * h;
            float v = ((step + 1) / 2) * HexMetrics.verticalTerraceStepSize;
            a.y += (b.y - a.y) * v;
            return a;
        }
        
        public static Color TerraceLerp (Color a, Color b, int step) {
            float h = step * HexMetrics.horizontalTerraceStepSize;
            return Color.Lerp(a, b, h);
        }
        
        public static HexEdgeType GetEdgeType (int elevation1, int elevation2) {
            if (elevation1 == elevation2) {
                return HexEdgeType.Flat;
            }
            int delta = elevation2 - elevation1;
            if (delta == 1 || delta == -1) {
                return HexEdgeType.Slope;
            }
            return HexEdgeType.Cliff;
        }

        public static Vector3 GetFirstCorner (HexDirection direction) {
            return corners[(int)direction];
        }

        public static Vector3 GetSecondCorner (HexDirection direction) {
            return corners[(int)direction + 1];
        }
        
        public static Vector3 GetFirstSolidCorner (HexDirection direction) {
            return corners[(int)direction] * solidFactor;
        }

        public static Vector3 GetSecondSolidCorner (HexDirection direction) {
            return corners[(int)direction + 1] * solidFactor;
        }
        
        public static Vector3 GetBridge (HexDirection direction) {
            return (corners[(int)direction] + corners[(int)direction + 1]) *
                   blendFactor;
        }
        
        public static Vector3 GetSolidEdgeMiddle (HexDirection direction) {
            return
                (corners[(int)direction] + corners[(int)direction + 1]) *
                (0.5f * solidFactor);
        }
        
        public static Vector3 Perturb (Vector3 position) {
            Vector4 sample = HexMetrics.SampleNoise(position);
            position.x += (sample.x * 2f - 1f) * HexMetrics.cellPerturbStrength;
            position.z += (sample.z * 2f - 1f) * HexMetrics.cellPerturbStrength;
            return position;
        }
        
        public static Vector3 GetFirstWaterCorner (HexDirection direction) {
            return corners[(int)direction] * waterFactor;
        }

        public static Vector3 GetSecondWaterCorner (HexDirection direction) {
            return corners[(int)direction + 1] * waterFactor;
        }
        
        public static Vector3 GetWaterBridge (HexDirection direction) {
            return (corners[(int)direction] + corners[(int)direction + 1]) *
                   waterBlendFactor;
        }
    }
}