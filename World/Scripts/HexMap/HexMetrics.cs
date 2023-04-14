using UnityEngine;

namespace JS.HexMap
{
    public static class HexMetrics {
        //六边形外径
        public const float outerRadius = 10f;
        //六边形内径
        public const float innerRadius = outerRadius * 0.866025404f;
        
        public const float solidFactor = 0.75f;
	
        public const float blendFactor = 1f - solidFactor;
        //海拔高度单位高度
        public const float elevationStep = 5f;
            
        public static Vector3[] corners = {
            new Vector3(0f, 0f, outerRadius),
            new Vector3(innerRadius, 0f, 0.5f * outerRadius),
            new Vector3(innerRadius, 0f, -0.5f * outerRadius),
            new Vector3(0f, 0f, -outerRadius),
            new Vector3(-innerRadius, 0f, -0.5f * outerRadius),
            new Vector3(-innerRadius, 0f, 0.5f * outerRadius),
            new Vector3(0f, 0f, outerRadius)
        };

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
    }
}