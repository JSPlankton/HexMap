using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;

namespace JS.HexMap
{
    public class HexMapEditor : MonoBehaviour
    {
        public HexGrid hexGrid;
        
        private int activeTerrainTypeIndex;
        private int activeElevation;
        private int activeWaterLevel;
        private int activeUrbanLevel, activeFarmLevel, activePlantLevel, activeSpecialIndex;

        private bool applyElevation;
        private bool applyWaterLevel;
        private bool applyUrbanLevel, applyFarmLevel, applyPlantLevel, applySpecialIndex;
        private int brushSize;
        
        //河流编辑模式
        enum OptionalToggle {
            Ignore, Yes, No
        }
	
        OptionalToggle riverMode, roadMode, walledMode;
        
        bool isDrag;
        HexDirection dragDirection;
        HexCell previousCell;

        void Update () {
            if (
                Input.GetMouseButton(0) &&
                !EventSystem.current.IsPointerOverGameObject()
            ) {
                HandleInput();
            }
            else {
                previousCell = null;
            }
        }

        void HandleInput () {
            Ray inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(inputRay, out hit)) {
                HexCell currentCell = hexGrid.GetCell(hit.point);
                if (previousCell && previousCell != currentCell) {
                    ValidateDrag(currentCell);
                }
                else {
                    isDrag = false;
                }
                EditCells(currentCell);
                previousCell = currentCell;
            }
            else {
                previousCell = null;
            }
        }
        
        void ValidateDrag (HexCell currentCell) {
            for (
                dragDirection = HexDirection.NE;
                dragDirection <= HexDirection.NW;
                dragDirection++
            ) {
                if (previousCell.GetNeighbor(dragDirection) == currentCell) {
                    isDrag = true;
                    return;
                }
            }
            isDrag = false;
        }
        
        public void SetApplyElevation (bool toggle) {
            applyElevation = toggle;
        }
        
        public void SetBrushSize (float size) {
            brushSize = (int)size;
        }
        
        void EditCell (HexCell cell) {
            if (cell) {
                if (activeTerrainTypeIndex >= 0) {
                    cell.TerrainTypeIndex = activeTerrainTypeIndex;
                }
                if (applyElevation) {
                    cell.Elevation = activeElevation;
                }
                if (applyWaterLevel) {
                    cell.WaterLevel = activeWaterLevel;
                }
                if (applySpecialIndex) {
                    cell.SpecialIndex = activeSpecialIndex;
                }
                if (applyUrbanLevel) {
                    cell.UrbanLevel = activeUrbanLevel;
                }
                if (applyFarmLevel) {
                    cell.FarmLevel = activeFarmLevel;
                }
                if (applyPlantLevel) {
                    cell.PlantLevel = activePlantLevel;
                }
                if (riverMode == OptionalToggle.No) {
                    cell.RemoveRiver();
                }
                if (roadMode == OptionalToggle.No) {
                    cell.RemoveRoads();
                }
                if (walledMode != OptionalToggle.Ignore) {
                    cell.Walled = walledMode == OptionalToggle.Yes;
                }
                if (isDrag) {
                    HexCell otherCell = cell.GetNeighbor(dragDirection.Opposite());
                    if (otherCell) {
                        if (riverMode == OptionalToggle.Yes) {
                            otherCell.SetOutgoingRiver(dragDirection);
                        }
                        if (roadMode == OptionalToggle.Yes) {
                            otherCell.AddRoad(dragDirection);
                        }
                    }
                }
            }
        }
        
        void EditCells (HexCell center) 
        {
            int centerX = center.coordinates.X;
            int centerZ = center.coordinates.Z;
            
            for (int r = 0, z = centerZ - brushSize; z <= centerZ; z++, r++) {
                for (int x = centerX - r; x <= centerX + brushSize; x++) {
                    EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
                }
            }
            for (int r = 0, z = centerZ + brushSize; z > centerZ; z--, r++) {
                for (int x = centerX - brushSize; x <= centerX + r; x++) {
                    EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
                }
            }
        }
        
        public void SetElevation (float elevation) {
            activeElevation = (int)elevation;
        }
        
        public void ShowUI (bool visible) {
            hexGrid.ShowUI(visible);
        }
        
        public void SetRiverMode (int mode) {
            riverMode = (OptionalToggle)mode;
        }
        
        public void SetRoadMode (int mode) {
            roadMode = (OptionalToggle)mode;
        }
        
        public void SetWalledMode (int mode) {
            walledMode = (OptionalToggle)mode;
        }
        
        public void SetApplyWaterLevel (bool toggle) {
            applyWaterLevel = toggle;
        }
	
        public void SetWaterLevel (float level) {
            activeWaterLevel = (int)level;
        }
        public void SetApplyUrbanLevel (bool toggle) {
            applyUrbanLevel = toggle;
        }
	
        public void SetUrbanLevel (float level) {
            activeUrbanLevel = (int)level;
        }
        
        public void SetApplyFarmLevel (bool toggle) {
            applyFarmLevel = toggle;
        }

        public void SetFarmLevel (float level) {
            activeFarmLevel = (int)level;
        }

        public void SetApplyPlantLevel (bool toggle) {
            applyPlantLevel = toggle;
        }

        public void SetPlantLevel (float level) {
            activePlantLevel = (int)level;
        }

        public void SetApplySpecialIndex (bool toggle) {
            applySpecialIndex = toggle;
        }

        public void SetSpecialIndex (float index) {
            activeSpecialIndex = (int)index;
        }
        
        public void SetTerrainTypeIndex (int index) {
            activeTerrainTypeIndex = index;
        }

    }
}
