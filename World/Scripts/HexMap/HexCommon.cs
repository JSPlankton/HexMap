using UnityEngine;

namespace JS.HexMap
{
    public enum HexDirection {
        NE, E, SE, SW, W, NW
    }
    
    public enum HexEdgeType {
        Flat, Slope, Cliff
    }
    
    public struct EdgeVertices {

        public Vector3 v1, v2, v3, v4, v5;
        
        public EdgeVertices (Vector3 corner1, Vector3 corner2) {
            v1 = corner1;
            v2 = Vector3.Lerp(corner1, corner2, 0.25f);
            v3 = Vector3.Lerp(corner1, corner2, 0.5f);
            v4 = Vector3.Lerp(corner1, corner2, 0.75f);
            v5 = corner2;
        }
        
        public EdgeVertices (Vector3 corner1, Vector3 corner2, float outerStep) {
            v1 = corner1;
            v2 = Vector3.Lerp(corner1, corner2, outerStep);
            v3 = Vector3.Lerp(corner1, corner2, 0.5f);
            v4 = Vector3.Lerp(corner1, corner2, 1f - outerStep);
            v5 = corner2;
        }
        
        public static EdgeVertices TerraceLerp (
            EdgeVertices a, EdgeVertices b, int step)
        {
            EdgeVertices result;
            result.v1 = HexMetrics.TerraceLerp(a.v1, b.v1, step);
            result.v2 = HexMetrics.TerraceLerp(a.v2, b.v2, step);
            result.v3 = HexMetrics.TerraceLerp(a.v3, b.v3, step);
            result.v4 = HexMetrics.TerraceLerp(a.v4, b.v4, step);
            result.v5 = HexMetrics.TerraceLerp(a.v5, b.v5, step);
            return result;
        }
    }

    /// <summary>
    /// 地形特征物旋转随机hash值
    /// </summary>
    public struct HexHash {
        public float a, b, c, d, e;

        public static HexHash Create () {
            HexHash hash;
            hash.a = Random.value * 0.999f;
            hash.b = Random.value * 0.999f;
            hash.c = Random.value * 0.999f;
            hash.d = Random.value * 0.999f;
            hash.e = Random.value * 0.999f;
            return hash;
        }
    }
}
