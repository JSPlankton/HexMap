using UnityEngine;

namespace JS.HexMap
{
    public class HexCell : MonoBehaviour
    {
        public HexCoordinates coordinates;
    
        //单元格颜色
        public Color color;
        
        [SerializeField]
        HexCell[] neighbors;
        
        public int Elevation {
            get {
                return elevation;
            }
            set {
                elevation = value;
                Vector3 position = transform.localPosition;
                position.y = value * HexMetrics.elevationStep;
                transform.localPosition = position;
            }
        }
        //海拔高度等级
        int elevation;

        public HexCell GetNeighbor (HexDirection direction) {
            return neighbors[(int)direction];
        }
    
        public void SetNeighbor (HexDirection direction, HexCell cell) {
            neighbors[(int)direction] = cell;
            cell.neighbors[(int)direction.Opposite()] = this;
        }
    }
}
