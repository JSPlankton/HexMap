using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

namespace JS.HexMap
{
    public class HexGrid : MonoBehaviour
    {
        /// <summary>
        /// Amount of cells in the X dimension.
        /// </summary>
        public int CellCountX { get; private set; }
        /// <summary>
        /// Amount of cells in the Z dimension.
        /// </summary>
        public int CellCountZ { get; private set; }
        /// <summary>
        /// Whether east-west Wrapping is enabled.
        /// </summary>
        public bool Wrapping { get; private set; }
        
        public int chunkCountX, chunkCountZ;

        public HexCell cellPrefab;
        public TextMeshProUGUI cellLabelPrefab;
        public HexGridChunk chunkPrefab;
        
        public int seed;
        
        HexCell[] cells;
        HexGridChunk[] chunks;
        int searchFrontierPhase;

        public Texture2D noiseSource;
        
        HexCellPriorityQueue searchFrontier;
        
        HexCell currentPathFrom, currentPathTo;
        bool currentPathExists;
        
        List<HexUnit> units = new List<HexUnit>();
        public HexUnit unitPrefab;
        
        HexCellShaderData cellShaderData;
        //单元格按列分组
        Transform[] columns;
        //当前中心列索引
        int currentCenterColumnIndex = -1;
        
        public bool HasPath {
            get {
                return currentPathExists;
            }
        }

        void Awake () {
            CellCountX = 20;
            CellCountZ = 15;
            HexMetrics.noiseSource = noiseSource;
            HexMetrics.InitializeHashGrid(seed);
            HexUnit.unitPrefab = unitPrefab;
            cellShaderData = gameObject.AddComponent<HexCellShaderData>();
            cellShaderData.Grid = this;
            CreateMap(CellCountX, CellCountZ, Wrapping);
        }
        
        void OnEnable () {
            if (!HexMetrics.noiseSource) {
                HexMetrics.noiseSource = noiseSource;
                HexMetrics.InitializeHashGrid(seed);
                HexUnit.unitPrefab = unitPrefab;
                HexMetrics.wrapSize = Wrapping ? CellCountX : 0;
                ResetVisibility();
            }
        }
        
        public bool CreateMap (int x, int z, bool Wrapping) {
            if (
                x <= 0 || x % HexMetrics.chunkSizeX != 0 ||
                z <= 0 || z % HexMetrics.chunkSizeZ != 0
            ) {
                Debug.LogError("Unsupported map size.");
                return false;
            }
            ClearPath();
            ClearUnits();
            
            if (columns != null) {
                for (int i = 0; i < columns.Length; i++) {
                    Destroy(columns[i].gameObject);
                }
            }

            CellCountX = x;
            CellCountZ = z;
            this.Wrapping = Wrapping;
            currentCenterColumnIndex = -1;
            HexMetrics.wrapSize = Wrapping ? CellCountX : 0;
            chunkCountX = CellCountX / HexMetrics.chunkSizeX;
            chunkCountZ = CellCountZ / HexMetrics.chunkSizeZ;
            cellShaderData.Initialize(CellCountX, CellCountZ);
            CreateChunks();
            CreateCells();
            
            return true;
        }
        
        /// <summary>
        /// 传入坐标的当前列 ： 默认摄像机位置为中心的点
        /// </summary>
        /// <param name="xPosition"></param>
        public void CenterMap (float xPosition) {
            int centerColumnIndex = (int)
                (xPosition / (HexMetrics.innerDiameter * HexMetrics.chunkSizeX));
		
            if (centerColumnIndex == currentCenterColumnIndex) {
                return;
            }
            currentCenterColumnIndex = centerColumnIndex;
            
            int minColumnIndex = centerColumnIndex - chunkCountX / 2;
            int maxColumnIndex = centerColumnIndex + chunkCountX / 2;
            
            Vector3 position;
            position.y = position.z = 0f;
            for (int i = 0; i < columns.Length; i++) {
                if (i < minColumnIndex) {
                    position.x = chunkCountX *
                                 (HexMetrics.innerDiameter * HexMetrics.chunkSizeX);
                }
                else if (i > maxColumnIndex) {
                    position.x = chunkCountX *
                                 -(HexMetrics.innerDiameter * HexMetrics.chunkSizeX);
                }
                else {
                    position.x = 0f;
                }
                columns[i].localPosition = position;
            }
        }
        
        void CreateCells () {
            cells = new HexCell[CellCountZ * CellCountX];

            for (int z = 0, i = 0; z < CellCountZ; z++) {
                for (int x = 0; x < CellCountX; x++) {
                    CreateCell(x, z, i++);
                }
            }
        }
        
        void CreateChunks () {
            columns = new Transform[chunkCountX];
            for (int x = 0; x < chunkCountX; x++) {
                columns[x] = new GameObject("Column").transform;
                columns[x].SetParent(transform, false);
            }
            
            chunks = new HexGridChunk[chunkCountX * chunkCountZ];

            for (int z = 0, i = 0; z < chunkCountZ; z++) {
                for (int x = 0; x < chunkCountX; x++) {
                    HexGridChunk chunk = chunks[i++] = Instantiate(chunkPrefab);
                    chunk.transform.SetParent(columns[x], false);
                }
            }
        }

        void CreateCell (int x, int z, int i) {
            Vector3 position;
            position.x = (x + z * 0.5f - z / 2)  * HexMetrics.innerDiameter;
            position.y = 0f;
            position.z = z * (HexMetrics.outerRadius * 1.5f);

            HexCell cell = cells[i] = Instantiate<HexCell>(cellPrefab);
            cell.transform.localPosition = position;
            cell.Coordinates = HexCoordinates.FromOffsetCoordinates(x, z);
            cell.Index = i;
            cell.ColumnIndex = x / HexMetrics.chunkSizeX;
            cell.ShaderData = cellShaderData;
            
            if (Wrapping) {
                cell.Explorable = z > 0 && z < CellCountZ - 1;
            }
            else {
                cell.Explorable =
                    x > 0 && z > 0 && x < CellCountX - 1 && z < CellCountZ - 1;
            }

            if (x > 0) {
                cell.SetNeighbor(HexDirection.W, cells[i - 1]);
                if (Wrapping && x == CellCountX - 1) {
                    cell.SetNeighbor(HexDirection.E, cells[i - x]);
                }
            }
            if (z > 0) {
                if ((z & 1) == 0) {
                    cell.SetNeighbor(HexDirection.SE, cells[i - CellCountX]);
                    if (x > 0) {
                        cell.SetNeighbor(HexDirection.SW, cells[i - CellCountX - 1]);
                    }
                    else if (Wrapping) {
                        cell.SetNeighbor(HexDirection.SW, cells[i - 1]);
                    }
                }
                else {
                    cell.SetNeighbor(HexDirection.SW, cells[i - CellCountX]);
                    if (x < CellCountX - 1) {
                        cell.SetNeighbor(HexDirection.SE, cells[i - CellCountX + 1]);
                    }
                    else if (Wrapping) {
                        cell.SetNeighbor(
                            HexDirection.SE, cells[i - CellCountX * 2 + 1]
                        );
                    }
                }
            }
            
            TextMeshProUGUI label = Instantiate<TextMeshProUGUI>(cellLabelPrefab);
            label.rectTransform.anchoredPosition =
                new Vector2(position.x, position.z);
            // label.text = cell.Coordinates.ToStringOnSeparateLines();
            cell.UIRect = label.rectTransform;
            
            cell.Elevation = 0;
            
            AddCellToChunk(x, z, cell);
        }
        
        void AddCellToChunk (int x, int z, HexCell cell) {
            int chunkX = x / HexMetrics.chunkSizeX;
            int chunkZ = z / HexMetrics.chunkSizeZ;
            HexGridChunk chunk = chunks[chunkX + chunkZ * chunkCountX];
            
            int localX = x - chunkX * HexMetrics.chunkSizeX;
            int localZ = z - chunkZ * HexMetrics.chunkSizeZ;
            chunk.AddCell(localX + localZ * HexMetrics.chunkSizeX, cell);
        }
        
        public HexCell GetCell (Vector3 position) {
            position = transform.InverseTransformPoint(position);
            HexCoordinates Coordinates = HexCoordinates.FromPosition(position);
            int index = Coordinates.X + Coordinates.Z * CellCountX + Coordinates.Z / 2;
            return cells[index];
        }
        
        public HexCell GetCell (HexCoordinates Coordinates) {
            int z = Coordinates.Z;
            if (z < 0 || z >= CellCountZ) {
                return null;
            }
            int x = Coordinates.X + z / 2;
            if (x < 0 || x >= CellCountX) {
                return null;
            }
            return cells[x + z * CellCountX];
        }
        
        public HexCell GetCell (int xOffset, int zOffset) {
            return cells[xOffset + zOffset * CellCountX];
        }
	
        public HexCell GetCell (int cellIndex) {
            return cells[cellIndex];
        }
        
        public void ShowUI (bool visible) {
            for (int i = 0; i < chunks.Length; i++) {
                chunks[i].ShowUI(visible);
            }
        }
        
        public void Save (BinaryWriter writer) {
            writer.Write(CellCountX);
            writer.Write(CellCountZ);
            writer.Write(Wrapping);
            for (int i = 0; i < cells.Length; i++) {
                cells[i].Save(writer);
            }
            writer.Write(units.Count);
            for (int i = 0; i < units.Count; i++) {
                units[i].Save(writer);
            }
        }

        public void Load (BinaryReader reader, int header) {
            ClearPath();
            ClearUnits();
            int x = 20, z = 15;
            if (header >= 1) {
                x = reader.ReadInt32();
                z = reader.ReadInt32();
            }
            bool Wrapping = header >= 5 ? reader.ReadBoolean() : false;
            if (x != CellCountX || z != CellCountZ || this.Wrapping != Wrapping) {
                if (!CreateMap(x, z, Wrapping)) {
                    return;
                }
            }
            
            bool originalImmediateMode = cellShaderData.ImmediateMode;
            cellShaderData.ImmediateMode = true;
            
            for (int i = 0; i < cells.Length; i++) {
                cells[i].Load(reader, header);
            }
            for (int i = 0; i < chunks.Length; i++) {
                chunks[i].Refresh();
            }
            if (header >= 2) {
                int unitCount = reader.ReadInt32();
                for (int i = 0; i < unitCount; i++) {
                    HexUnit.Load(reader, this);
                }
            }
            
            cellShaderData.ImmediateMode = originalImmediateMode;
        }

        #region 寻路
        public void FindPath (HexCell fromCell, HexCell toCell, HexUnit unit) {
            ClearPath();
            
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            currentPathFrom = fromCell;
            currentPathTo = toCell;
            currentPathExists = Search(fromCell, toCell, unit);
            sw.Stop();
            Debug.Log($"寻路耗时{sw.ElapsedMilliseconds}ms");
            
            ShowPath(unit.Speed);
        }
        
        private void ShowPath (int speed) {
            if (currentPathExists) {
                HexCell current = currentPathTo;
                while (current != currentPathFrom) {
                    int turn = (current.Distance - 1) / speed;
                    current.SetLabel(turn.ToString());
                    current.EnableHighlight(Color.white);
                    current = current.PathFrom;
                }
            }
            currentPathFrom.EnableHighlight(Color.blue);
            currentPathTo.EnableHighlight(Color.red);
        }
        
        public void ClearPath () {
            if (currentPathExists) {
                HexCell current = currentPathTo;
                while (current != currentPathFrom) {
                    current.SetLabel(null);
                    current.DisableHighlight();
                    current = current.PathFrom;
                }
                current.DisableHighlight();
                currentPathExists = false;
            }
            else if (currentPathFrom) {
                currentPathFrom.DisableHighlight();
                currentPathTo.DisableHighlight();
            }
            
            currentPathFrom = currentPathTo = null;
        }
        
        public List<HexCell> GetPath () {
            if (!currentPathExists) {
                return null;
            }
            List<HexCell> path = ListPool<HexCell>.Get();
            for (HexCell c = currentPathTo; c != currentPathFrom; c = c.PathFrom) {
                path.Add(c);
            }
            path.Add(currentPathFrom);
            path.Reverse();
            return path;
        }

        private bool Search(HexCell fromCell, HexCell toCell, HexUnit unit)
        {
            int speed = unit.Speed;
            searchFrontierPhase += 2;
            if (searchFrontier == null) {
                searchFrontier = new HexCellPriorityQueue();
            }
            else {
                searchFrontier.Clear();
            }

            fromCell.SearchPhase = searchFrontierPhase;
            fromCell.Distance = 0;
            
            searchFrontier.Enqueue(fromCell);
            
            while (searchFrontier.Count > 0)
            {
                HexCell current = searchFrontier.Dequeue();
                current.SearchPhase += 1;
                if (current == toCell) {
                    return true;
                }
                
                int currentTurn = (current.Distance - 1) / speed;
                
                for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
                    HexCell neighbor = current.GetNeighbor(d);
                    if (neighbor == null || neighbor.SearchPhase > searchFrontierPhase) {
                        continue;
                    }

                    if (!unit.IsValidDestination(neighbor)) {
                        continue;
                    }
                    int moveCost = unit.GetMoveCost(current, neighbor, d);
                    if (moveCost < 0) {
                        continue;
                    }
                    
                    int distance = current.Distance + moveCost;
                    int turn = (distance - 1) / speed;
                    if (turn > currentTurn) {
                        distance = turn * speed + moveCost;
                    }
                    if (neighbor.SearchPhase < searchFrontierPhase) {
                        neighbor.SearchPhase = searchFrontierPhase;
                        neighbor.Distance = distance;
                        neighbor.PathFrom = current;
                        neighbor.SearchHeuristic =
                            neighbor.Coordinates.DistanceTo(toCell.Coordinates);
                        searchFrontier.Enqueue(neighbor);
                    }
                    else if (distance < neighbor.Distance) {
                        int oldPriority = neighbor.SearchPriority;
                        neighbor.Distance = distance;
                        neighbor.PathFrom = current;
                        searchFrontier.Change(neighbor, oldPriority);
                    }
                }
            }
            
            return false;
        }
        
        List<HexCell> GetVisibleCells (HexCell fromCell, int range) {
            List<HexCell> visibleCells = ListPool<HexCell>.Get();
            searchFrontierPhase += 2;
            if (searchFrontier == null) {
                searchFrontier = new HexCellPriorityQueue();
            }
            else {
                searchFrontier.Clear();
            }
            
            range += fromCell.ViewElevation;
            fromCell.SearchPhase = searchFrontierPhase;
            fromCell.Distance = 0;
            
            searchFrontier.Enqueue(fromCell);
            
            HexCoordinates fromCoordinates = fromCell.Coordinates;
            while (searchFrontier.Count > 0)
            {
                HexCell current = searchFrontier.Dequeue();
                current.SearchPhase += 1;
                visibleCells.Add(current);

                for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
                    HexCell neighbor = current.GetNeighbor(d);
                    if (neighbor == null || 
                        neighbor.SearchPhase > searchFrontierPhase  ||
                        !neighbor.Explorable) {
                        continue;
                    }
                    
                    int distance = current.Distance + 1;
                    if (distance + neighbor.ViewElevation > range ||
                        distance > fromCoordinates.DistanceTo(neighbor.Coordinates)) {
                        continue;
                    }

                    if (neighbor.SearchPhase < searchFrontierPhase) {
                        neighbor.SearchPhase = searchFrontierPhase;
                        neighbor.Distance = distance;
                        neighbor.SearchHeuristic = 0;
                        searchFrontier.Enqueue(neighbor);
                    }
                    else if (distance < neighbor.Distance) {
                        int oldPriority = neighbor.SearchPriority;
                        neighbor.Distance = distance;
                        searchFrontier.Change(neighbor, oldPriority);
                    }
                }
            }
            
            return visibleCells;
        }

        #endregion

        #region 单位

        void ClearUnits () {
            for (int i = 0; i < units.Count; i++) {
                units[i].Die();
            }
            units.Clear();
        }
        
        public void AddUnit (HexUnit unit, HexCell location, float orientation) {
            units.Add(unit);
            unit.Grid = this;
            unit.Location = location;
            unit.Orientation = orientation;
        }
        
        public void MakeChildOfColumn (Transform child, int columnIndex) {
            child.SetParent(columns[columnIndex], false);
        }
        
        public void RemoveUnit (HexUnit unit) {
            units.Remove(unit);
            unit.Die();
        }

        #endregion

        #region 移动

        public HexCell GetCell (Ray ray) {
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit)) {
                return GetCell(hit.point);
            }
            return null;
        }

        #endregion

        #region 可见图/视野

        public void IncreaseVisibility (HexCell fromCell, int range) {
            List<HexCell> cells = GetVisibleCells(fromCell, range);
            for (int i = 0; i < cells.Count; i++) {
                cells[i].IncreaseVisibility();
            }
            ListPool<HexCell>.Add(cells);
        }

        public void DecreaseVisibility (HexCell fromCell, int range) {
            List<HexCell> cells = GetVisibleCells(fromCell, range);
            for (int i = 0; i < cells.Count; i++) {
                cells[i].DecreaseVisibility();
            }
            ListPool<HexCell>.Add(cells);
        }
        
        public void ResetVisibility () {
            for (int i = 0; i < cells.Length; i++) {
                cells[i].ResetVisibility();
            }
            for (int i = 0; i < units.Count; i++) {
                HexUnit unit = units[i];
                IncreaseVisibility(unit.Location, unit.VisionRange);
            }
        }

        #endregion
    }
}
