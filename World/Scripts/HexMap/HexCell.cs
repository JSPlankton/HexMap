using UnityEngine;

namespace JS.HexMap
{
    public class HexCell : MonoBehaviour
    {
        public HexCoordinates coordinates;
        public RectTransform uiRect;
        public HexGridChunk chunk;
        
        //单元格颜色
        public Color Color {
            get {
                return color;
            }
            set {
                if (color == value) {
                    return;
                }
                color = value;
                Refresh();
            }
        }
        Color color;
        
        //生成地形特征物等级
        public int UrbanLevel {
            get {
                return urbanLevel;
            }
            set {
                if (urbanLevel != value) {
                    urbanLevel = value;
                    RefreshSelfOnly();
                }
            }
        }
        public int FarmLevel {
            get {
                return farmLevel;
            }
            set {
                if (farmLevel != value) {
                    farmLevel = value;
                    RefreshSelfOnly();
                }
            }
        }

        public int PlantLevel {
            get {
                return plantLevel;
            }
            set {
                if (plantLevel != value) {
                    plantLevel = value;
                    RefreshSelfOnly();
                }
            }
        }
        

        int urbanLevel, farmLevel, plantLevel;
        
        [SerializeField]
        HexCell[] neighbors;
        [SerializeField]
        bool[] roads;

        public int Elevation {
            get {
                return elevation;
            }
            set {
                if (elevation == value) {
                    return;
                }
                elevation = value;
                Vector3 position = transform.localPosition;
                position.y = value * HexMetrics.elevationStep;
                position.y +=
                    (HexMetrics.SampleNoise(position).y * 2f - 1f) *
                    HexMetrics.elevationPerturbStrength;
                transform.localPosition = position;

                Vector3 uiPosition = uiRect.localPosition;
                uiPosition.z = -position.y;
                uiRect.localPosition = uiPosition;
                
                ValidateRivers();
                //如果单元格高度差过大，需要切断道路
                for (int i = 0; i < roads.Length; i++) {
                    if (roads[i] && GetElevationDifference((HexDirection)i) > 1) {
                        SetRoad(i, false);
                    }
                }
                
                Refresh();
            }
        }
        //海拔高度等级
        int elevation = int.MinValue;
        
        //水位高度
        public int WaterLevel {
            get {
                return waterLevel;
            }
            set {
                if (waterLevel == value) {
                    return;
                }
                waterLevel = value;
                ValidateRivers();
                Refresh();
            }
        }
	
        int waterLevel;
        
        public bool Walled {
            get {
                return walled;
            }
            set {
                if (walled != value) {
                    walled = value;
                    Refresh();
                }
            }
        }
	
        bool walled;
        
        public bool IsUnderwater {
            get {
                return waterLevel > elevation;
            }
        }
        
        public Vector3 Position {
            get {
                return transform.localPosition;
            }
        }

        #region 河流
        //河流 流入流出   
        bool hasIncomingRiver, hasOutgoingRiver;
        //河流流向 流入流出
        HexDirection incomingRiver, outgoingRiver;
        public bool HasIncomingRiver {
            get {
                return hasIncomingRiver;
            }
        }

        public bool HasOutgoingRiver {
            get {
                return hasOutgoingRiver;
            }
        }

        public HexDirection IncomingRiver {
            get {
                return incomingRiver;
            }
        }

        public HexDirection OutgoingRiver {
            get {
                return outgoingRiver;
            }
        }
        
        public bool HasRiver {
            get {
                return hasIncomingRiver || hasOutgoingRiver;
            }
        }
        
        public bool HasRiverBeginOrEnd {
            get {
                return hasIncomingRiver != hasOutgoingRiver;
            }
        }
        
        public float RiverSurfaceY {
            get {
                return
                    (elevation + HexMetrics.waterElevationOffset) *
                    HexMetrics.elevationStep;
            }
        }
        
        public float WaterSurfaceY {
            get {
                return
                    (waterLevel + HexMetrics.waterElevationOffset) *
                    HexMetrics.elevationStep;
            }
        }
        
        public float StreamBedY {
            get {
                return
                    (elevation + HexMetrics.streamBedElevationOffset) *
                    HexMetrics.elevationStep;
            }
        }
        
        public HexDirection RiverBeginOrEndDirection
        {
            get { return hasIncomingRiver ? incomingRiver : outgoingRiver; }
        }
        
        public bool HasRiverThroughEdge (HexDirection direction) {
            return
                hasIncomingRiver && incomingRiver == direction ||
                hasOutgoingRiver && outgoingRiver == direction;
        }
        
        public void RemoveOutgoingRiver () {
            if (!hasOutgoingRiver) {
                return;
            }
            hasOutgoingRiver = false;
            RefreshSelfOnly();
            
            HexCell neighbor = GetNeighbor(outgoingRiver);
            neighbor.hasIncomingRiver = false;
            neighbor.RefreshSelfOnly();
        }
        
        public void RemoveIncomingRiver () {
            if (!hasIncomingRiver) {
                return;
            }
            hasIncomingRiver = false;
            RefreshSelfOnly();

            HexCell neighbor = GetNeighbor(incomingRiver);
            neighbor.hasOutgoingRiver = false;
            neighbor.RefreshSelfOnly();
        }
        
        public void RemoveRiver () {
            RemoveOutgoingRiver();
            RemoveIncomingRiver();
        }
        
        public void SetOutgoingRiver (HexDirection direction) {
            //当前方向已存在河流
            if (hasOutgoingRiver && outgoingRiver == direction) {
                return;
            }
            //当前方向存在单元格，河流不能向高处流动，判断高度
            HexCell neighbor = GetNeighbor(direction);
            if (!IsValidRiverDestination(neighbor)) {
                return;
            }
            //清除上一个流出方向的河流，且当流入方向与当前流出方向重叠时，清除流入方向河流
            RemoveOutgoingRiver();
            if (hasIncomingRiver && incomingRiver == direction) {
                RemoveIncomingRiver();
            }
            //设置流出方向河流
            hasOutgoingRiver = true;
            outgoingRiver = direction;

            //相邻单元格上已经有流入方向的河流时，移除它并设置新的流入河流
            neighbor.RemoveIncomingRiver();
            neighbor.hasIncomingRiver = true;
            neighbor.incomingRiver = direction.Opposite();
            
            //河流可以冲散道路，Refresh在道路里做
            SetRoad((int)direction, false);
        }
        
        #endregion

        #region 道路

        public bool HasRoadThroughEdge (HexDirection direction) {
            return roads[(int)direction];
        }
        
        public bool HasRoads {
            get {
                for (int i = 0; i < roads.Length; i++) {
                    if (roads[i]) {
                        return true;
                    }
                }
                return false;
            }
        }
        
        public void AddRoad (HexDirection direction) {
            if (
                !roads[(int)direction] && !HasRiverThroughEdge(direction) &&
                GetElevationDifference(direction) <= 1
            ) {
                SetRoad((int)direction, true);
            }
        }

        public void RemoveRoads () {
            for (int i = 0; i < neighbors.Length; i++) {
                if (roads[i]) {
                    roads[i] = false;
                    neighbors[i].roads[(int)((HexDirection)i).Opposite()] = false;
                    neighbors[i].RefreshSelfOnly();
                    RefreshSelfOnly();
                }
            }
        }
        
        void SetRoad (int index, bool state) {
            roads[index] = state;
            neighbors[index].roads[(int)((HexDirection)index).Opposite()] = state;
            neighbors[index].RefreshSelfOnly();
            RefreshSelfOnly();
        }
        
        public int GetElevationDifference (HexDirection direction) {
            int difference = elevation - GetNeighbor(direction).elevation;
            return difference >= 0 ? difference : -difference;
        }

        #endregion
        public HexCell GetNeighbor (HexDirection direction) {
            return neighbors[(int)direction];
        }
    
        public void SetNeighbor (HexDirection direction, HexCell cell) {
            neighbors[(int)direction] = cell;
            cell.neighbors[(int)direction.Opposite()] = this;
        }
        
        public HexEdgeType GetEdgeType (HexDirection direction) {
            return HexMetrics.GetEdgeType(
                elevation, neighbors[(int)direction].elevation
            );
        }
        
        public HexEdgeType GetEdgeType (HexCell otherCell) {
            return HexMetrics.GetEdgeType(
                elevation, otherCell.elevation
            );
        }

        void Refresh()
        {
            if (chunk)
            {
                chunk.Refresh();
                for (int i = 0; i < neighbors.Length; i++) {
                    HexCell neighbor = neighbors[i];
                    if (neighbor != null && neighbor.chunk != chunk) {
                        neighbor.chunk.Refresh();
                    }
                }
            }
        }
        
        void RefreshSelfOnly () {
            chunk.Refresh();
        }
        
        void ValidateRivers () {
            if (
                hasOutgoingRiver &&
                !IsValidRiverDestination(GetNeighbor(outgoingRiver))
            ) {
                RemoveOutgoingRiver();
            }
            if (
                hasIncomingRiver &&
                !GetNeighbor(incomingRiver).IsValidRiverDestination(this)
            ) {
                RemoveIncomingRiver();
            }
        }
        
        bool IsValidRiverDestination (HexCell neighbor) {
            return neighbor && (
                elevation >= neighbor.elevation || waterLevel == neighbor.elevation
            );
        }
    }
}
