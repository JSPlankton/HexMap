using UnityEngine;
using UnityEngine.UI;
using System.IO;
using TMPro;
using UnityEngine.Rendering.Universal;

namespace JS.HexMap
{
    public class HexCell : MonoBehaviour
    {
        public HexCoordinates Coordinates { get; set; }
        public RectTransform UIRect { get; set; }
        public HexGridChunk Chunk { get; set; }
        public int ColumnIndex { get; set; }
        public bool IsVisible {
            get {
                return visibility > 0 && Explorable;
            }
        }
        
        //视野高度
        public int ViewElevation {
            get {
                return elevation >= waterLevel ? elevation : waterLevel;
            }
        }
        
        //单元格能否被探索
        public bool Explorable { get; set; }
        
        public HexCellShaderData ShaderData { get; set; }
        public HexUnit Unit { get; set; }
        public HexCell PathFrom { get; set; }
        public int SearchHeuristic { get; set; }
        public int SearchPriority {
            get {
                return distance + SearchHeuristic;
            }
        }
        
        public HexCell NextWithSamePriority { get; set; }
        
        public int SearchPhase { get; set; }

        public int TerrainTypeIndex {
            get {
                return terrainTypeIndex;
            }
            set {
                if (terrainTypeIndex != value) {
                    terrainTypeIndex = value;
                    ShaderData.RefreshTerrain(this);
                }
            }
        }
        
        public int Index { get; set; }
        
        private int terrainTypeIndex;
        private int distance;

        public int Distance {
            get {
                return distance;
            }
            set
            {
                distance = value;
            }
        }
        
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
                ShaderData.ViewElevationChanged(this);
                RefreshPosition();
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
        
        void RefreshPosition () {
            Vector3 position = transform.localPosition;
            position.y = elevation * HexMetrics.elevationStep;
            position.y +=
                (HexMetrics.SampleNoise(position).y * 2f - 1f) *
                HexMetrics.elevationPerturbStrength;
            transform.localPosition = position;

            Vector3 uiPosition = UIRect.localPosition;
            uiPosition.z = -position.y;
            UIRect.localPosition = uiPosition;
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
                ShaderData.ViewElevationChanged(this);
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
        
        public int SpecialIndex {
            get {
                return specialIndex;
            }
            set {
                if (specialIndex != value && !HasRiver) {
                    specialIndex = value;
                    RemoveRoads();
                    RefreshSelfOnly();
                }
            }
        }
        
        int specialIndex;
        
        public bool IsSpecial {
            get {
                return specialIndex > 0;
            }
        }
        
        int visibility;
        public void IncreaseVisibility () {
            visibility += 1;
            if (visibility == 1) {
                IsExplored = true;
                ShaderData.RefreshVisibility(this);
            }
        }

        public void DecreaseVisibility () {
            visibility -= 1;
            if (visibility == 0) {
                ShaderData.RefreshVisibility(this);
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
        
        public bool IsExplored {
            get {
                return explored && Explorable;
            }
            private set {
                explored = value;
            }
        }
        
        bool explored;
        
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
            specialIndex = 0;

            //相邻单元格上已经有流入方向的河流时，移除它并设置新的流入河流
            neighbor.RemoveIncomingRiver();
            neighbor.hasIncomingRiver = true;
            neighbor.incomingRiver = direction.Opposite();
            neighbor.specialIndex = 0;
            
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
                !IsSpecial && !GetNeighbor(direction).IsSpecial &&
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
            if (Chunk)
            {
                Chunk.Refresh();
                for (int i = 0; i < neighbors.Length; i++) {
                    HexCell neighbor = neighbors[i];
                    if (neighbor != null && neighbor.Chunk != Chunk) {
                        neighbor.Chunk.Refresh();
                    }
                }
                if (Unit) {
                    Unit.ValidateLocation();
                }
            }
        }
        
        void RefreshSelfOnly () {
            Chunk.Refresh();
            if (Unit) {
                Unit.ValidateLocation();
            }
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
        
        public void Save (BinaryWriter writer) {
            writer.Write((byte)terrainTypeIndex);
            writer.Write((byte)(elevation + 127));
            writer.Write((byte)waterLevel);
            writer.Write((byte)urbanLevel);
            writer.Write((byte)farmLevel);
            writer.Write((byte)plantLevel);
            writer.Write((byte)specialIndex);
            writer.Write(walled);

            if (hasIncomingRiver) {
                writer.Write((byte)(incomingRiver + 128));
            }
            else {
                writer.Write((byte)0);
            }

            if (hasOutgoingRiver) {
                writer.Write((byte)(outgoingRiver + 128));
            }
            else {
                writer.Write((byte)0);
            }

            int roadFlags = 0;
            for (int i = 0; i < roads.Length; i++) {
                if (roads[i]) {
                    roadFlags |= 1 << i;
                }
            }
            writer.Write((byte)roadFlags);
            writer.Write(IsExplored);
        }

        public void Load (BinaryReader reader, int header) {
            terrainTypeIndex = reader.ReadByte();
            elevation = reader.ReadByte();
            if (header >= 4) {
                elevation -= 127;
            }
            RefreshPosition();
            waterLevel = reader.ReadByte();
            urbanLevel = reader.ReadByte();
            farmLevel = reader.ReadByte();
            plantLevel = reader.ReadByte();
            specialIndex = reader.ReadByte();
            walled = reader.ReadBoolean();

            byte riverData = reader.ReadByte();
            if (riverData >= 128) {
                hasIncomingRiver = true;
                incomingRiver = (HexDirection)(riverData - 128);
            }
            else {
                hasIncomingRiver = false;
            }

            riverData = reader.ReadByte();
            if (riverData >= 128) {
                hasOutgoingRiver = true;
                outgoingRiver = (HexDirection)(riverData - 128);
            }
            else {
                hasOutgoingRiver = false;
            }

            int roadFlags = reader.ReadByte();
            for (int i = 0; i < roads.Length; i++) {
                roads[i] = (roadFlags & (1 << i)) != 0;
            }
            
            IsExplored = header >= 3 ? reader.ReadBoolean() : false;
            ShaderData.RefreshTerrain(this);
            ShaderData.RefreshVisibility(this);
        }
        
        public void SetLabel (string text) {
            if (UIRect.TryGetComponent(out TextMeshProUGUI textMeshProUGUI))
            {
                textMeshProUGUI.text = text;
            }
        }

        public void DisableHighlight () {
            Image highlight = UIRect.GetChild(0).GetComponent<Image>();
            highlight.enabled = false;
        }
	
        public void EnableHighlight (Color color) {
            Image highlight = UIRect.GetChild(0).GetComponent<Image>();
            highlight.color = color;
            highlight.enabled = true;
        }
        
        public void ResetVisibility () {
            if (visibility > 0) {
                visibility = 0;
                ShaderData.RefreshVisibility(this);
            }
        }
        
        public void SetMapData (float data) {
            ShaderData.SetMapData(this, data);
        }
    }
}
