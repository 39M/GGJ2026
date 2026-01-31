using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace GGJ
{

    [System.Serializable]
    public class GridCell
    {
        public int x;
        public int y;
        public bool hasCollision;
        public bool isWalkable => !hasCollision;

        public bool[] connections = new bool[4];
        public bool IsConnection(Direction dir) => dir != Direction.None && connections[(int)dir - 1];

        public GridCell(int x, int y)
        {
            this.x = x;
            this.y = y;
            hasCollision = false;
            connections = new bool[] { false, false, false, false };
        }
    }

    [System.Serializable]
    public class GridMapData
    {
        [SerializeField]
        public GridCell[,] grid;
        public int width;
        public int height;
        public Vector2 origin;
        public float cellSize;

        public GridCell GetCell(int x, int y)
        {
            if (x >= 0 && x < width && y >= 0 && y < height)
                return grid[x, y];
            return null;
        }
    }

    public class MapScanner : MonoSingleton<MapScanner>
    {
        [Header("扫描设置")] public LayerMask collisionLayer = ~0; // 默认扫描所有层
        public float cellSize = 1f;
        public Vector2 scanOrigin = Vector2.zero;
        public Vector2 scanSize = new Vector2(20, 20);

        [Header("调试显示")] public bool showGrid = true;
        public bool showCollisions = true;
        public bool showConnections = true;
        public Color walkableColor = Color.green * 0.4f;
        public Color blockedColor = Color.red * 0.4f;
        public Color connectionColor = Color.blue;

        [SerializeField]
        [Header("扫描结果")] public GridMapData mapData;

        
        //TODO 可以缓存数据
        protected override void Awake()
        {
            base.Awake();
            ScanMap();
        }

        [Button]
        public void ScanMap()
        {
            // 计算网格数量
            int gridWidth = Mathf.CeilToInt(scanSize.x / cellSize);
            int gridHeight = Mathf.CeilToInt(scanSize.y / cellSize);

            // 初始化地图数据
            mapData = new GridMapData
            {
                width = gridWidth,
                height = gridHeight,
                origin = scanOrigin,
                cellSize = cellSize,
                grid = new GridCell[gridWidth, gridHeight]
            };

            // 扫描每个网格的碰撞信息
            ScanCollisions();

            // 分析网格之间的连通关系
            AnalyzeConnections();

            // 裁剪边界，移除最外层碰撞之外的空白区域
            TrimBounds();

            Debug.Log($"地图扫描完成！网格: {mapData.width}x{mapData.height}，原点: {mapData.origin}，有碰撞的格子: {GetBlockedCellCount()}");
        }

        /// <summary>
        /// 裁剪边界，移除最外层碰撞之外的空白区域
        /// </summary>
        private void TrimBounds()
        {
            int minX = mapData.width;
            int maxX = -1;
            int minY = mapData.height;
            int maxY = -1;

            // 找到任意方向不连通的格子作为边界
            for (int x = 0; x < mapData.width; x++)
            {
                for (int y = 0; y < mapData.height; y++)
                {
                    var cell = mapData.grid[x, y];
                    
                    // 检查是否有任意方向不连通（表示是边界或有碰撞）
                    bool hasBlockedDirection = false;
                    for (int dir = 0; dir < 4; dir++)
                    {
                        if (!cell.connections[dir])
                        {
                            hasBlockedDirection = true;
                            break;
                        }
                    }
                    
                    // 有碰撞或有不连通方向的格子都算作有效区域
                    if (cell.hasCollision || hasBlockedDirection)
                    {
                        minX = Mathf.Min(minX, x + 1);
                        maxX = Mathf.Max(maxX, x - 1);
                        minY = Mathf.Min(minY, y + 1);
                        maxY = Mathf.Max(maxY, y - 1);
                    }
                }
            }

            // 如果没有找到边界格子，保持原样
            if (maxX < 0 || maxY < 0)
            {
                Debug.LogWarning("[MapScanner] 未找到任何边界格子，保持原始边界");
                return;
            }

            // 计算新的尺寸
            int newWidth = maxX - minX + 1;
            int newHeight = maxY - minY + 1;

            // 如果边界没有变化，不需要裁剪
            if (minX == 0 && minY == 0 && newWidth == mapData.width && newHeight == mapData.height)
            {
                return;
            }

            // 创建新的网格数组
            GridCell[,] newGrid = new GridCell[newWidth, newHeight];

            // 复制数据到新数组，并更新格子的坐标（保留连通关系）
            for (int x = 0; x < newWidth; x++)
            {
                for (int y = 0; y < newHeight; y++)
                {
                    var oldCell = mapData.grid[x + minX, y + minY];
                    newGrid[x, y] = new GridCell(x, y)
                    {
                        hasCollision = oldCell.hasCollision,
                        connections = (bool[])oldCell.connections.Clone()
                    };
                }
            }

            // 计算新的原点（世界坐标）
            Vector2 newOrigin = new Vector2(
                mapData.origin.x + minX * mapData.cellSize,
                mapData.origin.y + minY * mapData.cellSize
            );

            // 更新 mapData
            mapData.grid = newGrid;
            mapData.width = newWidth;
            mapData.height = newHeight;
            mapData.origin = newOrigin;

            Debug.Log($"[MapScanner] 边界裁剪完成: 原始({minX},{minY})-({maxX},{maxY}) -> 新尺寸 {newWidth}x{newHeight}, 新原点 {newOrigin}");
        }

        /// <summary>
        /// 扫描每个网格的碰撞情况
        /// </summary>
        private void ScanCollisions()
        {
            for (int x = 0; x < mapData.width; x++)
            {
                for (int y = 0; y < mapData.height; y++)
                {
                    Vector2 cellCenter = GetCellCenterWorld(x, y);
                    Collider2D[] colliders = Physics2D.OverlapBoxAll(cellCenter,
                        Vector2.one * cellSize * 0.75f, 0f, collisionLayer);

                    mapData.grid[x, y] = new GridCell(x, y)
                    {
                        hasCollision = colliders.Count(c=>!c.isTrigger) > 0
                    };
                }
            }
        }

        /// <summary>
        /// 分析网格之间的连通关系
        /// </summary>
        private void AnalyzeConnections()
        {
            Vector2[] directions = new[]
            {
                Direction.Up.GetVec(), 
                Direction.Down.GetVec(), 
                Direction.Left.GetVec(), 
                Direction.Right.GetVec() 
            };

            for (int x = 0; x < mapData.width; x++)
            {
                for (int y = 0; y < mapData.height; y++)
                {
                    GridCell currentCell = mapData.grid[x, y];

                    // 如果当前格子有碰撞，则没有连通关系
                    if (currentCell.hasCollision)
                    {
                        for (int dir = 0; dir < 4; dir++)
                        {
                            currentCell.connections[dir] = false;
                        }

                        continue;
                    }

                    // 检查四个方向的连通性
                    for (int dir = 0; dir < 4; dir++)
                    {
                        Vector2 cellCenter = GetCellCenterWorld(x, y);
                        Collider2D[] colliders = Physics2D.OverlapBoxAll(cellCenter + directions[dir] * 0.5f,
                            Vector2.one * cellSize * 0.5f, 0f, collisionLayer);
                        currentCell.connections[dir] = colliders.Count(c => !c.isTrigger) == 0;

                    }
                }
            }
        }

        /// <summary>
        /// 获取指定世界坐标处的网格
        /// </summary>
        public GridCell GetCellAtWorldPosition(Vector2 worldPos)
        {
            Vector2Int gridPos = WorldToGridPosition(worldPos);
            return mapData.GetCell(gridPos.x, gridPos.y);
        }

        /// <summary>
        /// 将世界坐标转换为网格坐标
        /// </summary>
        public Vector2Int WorldToGridPosition(Vector2 worldPos)
        {
            int x = Mathf.FloorToInt((worldPos.x - mapData.origin.x) / mapData.cellSize);
            int y = Mathf.FloorToInt((worldPos.y - mapData.origin.y) / mapData.cellSize);

            return new Vector2Int(x, y);
        }

        /// <summary>
        /// 将网格坐标转换为世界坐标（网格中心）
        /// </summary>
        public Vector2 GetCellCenterWorld(int x, int y)
        {
            return new Vector2(
                mapData.origin.x + x * mapData.cellSize + mapData.cellSize * 0.5f,
                mapData.origin.y + y * mapData.cellSize + mapData.cellSize * 0.5f
            );
        }

        /// <summary>
        /// 获取有碰撞的格子数量
        /// </summary>
        public int GetBlockedCellCount()
        {
            int count = 0;
            for (int x = 0; x < mapData.width; x++)
            {
                for (int y = 0; y < mapData.height; y++)
                {
                    if (mapData.grid[x, y].hasCollision)
                        count++;
                }
            }

            return count;
        }

        /// <summary>
        /// 检查两个网格是否连通
        /// </summary>
        public bool AreCellsConnected(int x1, int y1, int x2, int y2)
        {
            GridCell cell1 = mapData.GetCell(x1, y1);
            GridCell cell2 = mapData.GetCell(x2, y2);

            if (cell1 == null || cell2 == null || cell1.hasCollision || cell2.hasCollision)
                return false;

            // 判断是否相邻
            int dx = Mathf.Abs(x1 - x2);
            int dy = Mathf.Abs(y1 - y2);

            if (dx + dy != 1) // 不是直接相邻
                return false;

            // 检查连通方向
            if (x2 > x1) // 右边
                return cell1.connections[3];
            else if (x2 < x1) // 左边
                return cell1.connections[2];
            else if (y2 > y1) // 上边
                return cell1.connections[0];
            else if (y2 < y1) // 下边
                return cell1.connections[1];

            return false;
        }


        /// <summary>
        /// 在Scene视图中绘制调试信息
        /// </summary>
        void OnDrawGizmosSelected()
        {
            if (!showGrid || mapData == null || mapData.grid == null)
                return;

            for (int x = 0; x < mapData.width; x++)
            {
                for (int y = 0; y < mapData.height; y++)
                {
                    GridCell cell = mapData.grid[x, y];
                    if (cell == null) continue;

                    Vector2 center = GetCellCenterWorld(x, y);

                    // 绘制网格
                    Gizmos.color = Color.white;
                    Gizmos.DrawWireCube(center, Vector2.one * cellSize);

                    // 绘制碰撞信息
                    if (showCollisions)
                    {
                        Gizmos.color = cell.hasCollision ? blockedColor : walkableColor;
                        Gizmos.DrawCube(center, Vector2.one * cellSize * 0.9f);
                    }

                    // 绘制连通关系
                    if (showConnections && !cell.hasCollision)
                    {
                        Gizmos.color = connectionColor;

                        for (var dir = Direction.Up; dir <= Direction.Right; dir++)
                        {
                            if (cell.IsConnection(dir) )
                            {
                                Vector2 to = GetCellCenterWorld(x + (int)dir.GetVec().x, y + (int)dir.GetVec().y);
                                DrawArrow(center, to);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 绘制箭头（用于显示连通方向）
        /// </summary>
        private void DrawArrow(Vector2 from, Vector2 to)
        {
            Vector2 direction = (to - from).normalized * 0.3f;
            Vector2 perpendicular = new Vector2(-direction.y, direction.x) * 0.1f;

            Gizmos.DrawLine(from, from + direction);
            Gizmos.DrawLine(from + direction, from + perpendicular);
            Gizmos.DrawLine(from + direction, from - perpendicular);
        }
    }
    
}