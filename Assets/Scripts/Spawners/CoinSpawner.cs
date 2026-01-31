using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GGJ
{
    /// <summary>
    /// 金币生成器：负责在地图上随机生成金币，避免与已有物体重叠
    /// </summary>
    public class CoinSpawner : MonoSingleton<CoinSpawner>
    {
        [Header("生成设置")]
        [Tooltip("检测重叠时使用的层级")]
        public LayerMask overlapCheckLayers = ~0;

        /// <summary> 预设的金币位置列表（从关卡中收集） </summary>
        private List<Vector2> presetPositions = new List<Vector2>();

        /// <summary>
        /// 初始化时收集关卡中已有的金币位置
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
            CollectPresetPositions();
        }

        /// <summary>
        /// 收集关卡中所有预设金币的位置
        /// </summary>
        public void CollectPresetPositions()
        {
            presetPositions.Clear();
            
            var mapScanner = MapScanner.Instance;
            var existingCoins = FindObjectsByType<Coin>(FindObjectsSortMode.None);
            
            foreach (var coin in existingCoins)
            {
                Vector2 coinPos = coin.transform.position;
                
                // 将金币位置对齐到网格中心
                // if (mapScanner != null && mapScanner.mapData != null)
                // {
                //     var gridPos = mapScanner.WorldToGridPosition(coinPos);
                //     var alignedPos = mapScanner.GetCellCenterWorld(gridPos.x, gridPos.y);
                //     coin.transform.position = alignedPos; // 将现有金币移动到对齐位置
                //     presetPositions.Add(alignedPos);
                // }
                // else
                {
                    // 如果 MapScanner 未初始化，使用原始位置
                    presetPositions.Add(coinPos);
                }
            }
            
            Debug.Log($"[CoinSpawner] 收集了 {presetPositions.Count} 个预设金币位置（已对齐网格）");
        }

        /// <summary>
        /// 生成指定数量的金币
        /// </summary>
        /// <param name="count">金币数量</param>
        /// <returns>实际生成的金币列表</returns>
        public List<Coin> SpawnCoins(int count)
        {
            var config = GameCfg.Instance.EventConfig;
            
            switch (config.SpawnMode)
            {
                case CoinSpawnMode.PresetPositions:
                    return SpawnCoinsAtPresetPositions(count);
                case CoinSpawnMode.RandomGrid:
                default:
                    return SpawnCoinsAtRandomGrid(count);
            }
        }

        /// <summary>
        /// 在预设位置生成金币
        /// </summary>
        private List<Coin> SpawnCoinsAtPresetPositions(int count)
        {
            var spawnedCoins = new List<Coin>();
            var config = GameCfg.Instance.EventConfig;
            
            if (presetPositions.Count == 0)
            {
                Debug.LogWarning("[CoinSpawner] 没有预设金币位置！将使用随机格子模式");
                return SpawnCoinsAtRandomGrid(count);
            }
            
            float checkRadius = config.CoinCheckRadius;
            
            // 打乱预设位置顺序
            var shuffledPositions = presetPositions.OrderBy(_ => Random.value).ToList();
            
            int spawned = 0;
            foreach (var pos in shuffledPositions)
            {
                if (spawned >= count) break;
                
                // 检查该位置是否已有物体
                if (!IsPositionOccupied(pos, checkRadius))
                {
                    var coin = SpawnCoinAt(pos);
                    if (coin != null)
                    {
                        spawnedCoins.Add(coin);
                        spawned++;
                    }
                }
            }
            
            Debug.Log($"[CoinSpawner] 预设位置模式：成功生成 {spawned}/{count} 个金币（共 {presetPositions.Count} 个预设位置）");
            return spawnedCoins;
        }

        /// <summary>
        /// 在随机格子生成金币
        /// </summary>
        private List<Coin> SpawnCoinsAtRandomGrid(int count)
        {
            var spawnedCoins = new List<Coin>();
            var mapScanner = MapScanner.Instance;
            var config = GameCfg.Instance.EventConfig;
            
            if (mapScanner == null || mapScanner.mapData == null)
            {
                Debug.LogError("[CoinSpawner] MapScanner 未初始化！");
                return spawnedCoins;
            }
            
            // 获取所有可行走的格子
            var walkableCells = GetWalkableCells(mapScanner);
            
            if (walkableCells.Count == 0)
            {
                Debug.LogWarning("[CoinSpawner] 没有可用的生成位置！");
                return spawnedCoins;
            }
            
            int attempts = 0;
            int spawned = 0;
            int maxAttempts = config.MaxSpawnAttempts;
            float checkRadius = config.CoinCheckRadius;
            
            // 打乱格子顺序以实现随机
            var shuffledCells = walkableCells.OrderBy(_ => Random.value).ToList();
            int cellIndex = 0;
            
            while (spawned < count && attempts < maxAttempts && cellIndex < shuffledCells.Count)
            {
                var cell = shuffledCells[cellIndex];
                var worldPos = mapScanner.GetCellCenterWorld(cell.x, cell.y);
                
                var spawnPos = worldPos;
                
                // 检查该位置是否已有物体
                if (!IsPositionOccupied(spawnPos, checkRadius))
                {
                    var coin = SpawnCoinAt(spawnPos);
                    if (coin != null)
                    {
                        spawnedCoins.Add(coin);
                        spawned++;
                    }
                }
                
                attempts++;
                cellIndex++;
            }
            
            Debug.Log($"[CoinSpawner] 随机格子模式：成功生成 {spawned}/{count} 个金币，尝试次数: {attempts}");
            return spawnedCoins;
        }
        
        /// <summary>
        /// 在指定位置生成金币
        /// </summary>
        private Coin SpawnCoinAt(Vector2 position)
        {
            var coinPrefab = GameCfg.Instance.CoinPrefab;
            if (coinPrefab == null)
            {
                Debug.LogError("[CoinSpawner] 金币预制体未配置！");
                return null;
            }
            
            var coinObj = Instantiate(coinPrefab.gameObject, position, Quaternion.identity);
            return coinObj.GetComponent<Coin>();
        }
        
        /// <summary>
        /// 检查位置是否被占用
        /// </summary>
        /// <param name="position">目标位置</param>
        /// <param name="checkRadius">检测半径</param>
        private bool IsPositionOccupied(Vector2 position, float checkRadius)
        {
            var colliders = Physics2D.OverlapCircleAll(position, checkRadius, overlapCheckLayers);
            var mapScanner = MapScanner.Instance;
            
            // 获取目标位置的网格坐标
            Vector2Int targetGridPos = Vector2Int.zero;
            bool hasMapScanner = mapScanner != null && mapScanner.mapData != null;
            if (hasMapScanner)
            {
                targetGridPos = mapScanner.WorldToGridPosition(position);
            }
            
            foreach (var col in colliders)
            {
                // 有实体碰撞体（非Trigger），位置被占用
                if (!col.isTrigger)
                {
                    return true;
                }
                
                // 检查金币：只有在同一格子时才算占用
                var coin = col.GetComponentInParent<Coin>();
                if (coin != null)
                {
                    if (hasMapScanner)
                    {
                        var coinGridPos = mapScanner.WorldToGridPosition(coin.transform.position);
                        if (coinGridPos == targetGridPos)
                        {
                            return true; // 同一格子，被占用
                        }
                        // 不同格子，忽略（碰撞体辐射到旁边）
                    }
                    else
                    {
                        return true; // 无法判断格子，保守处理
                    }
                    continue;
                }
                
                // 检查玩家：同样只有在同一格子时才算占用
                var player = col.GetComponentInParent<PlayerController>();
                if (player != null)
                {
                    if (hasMapScanner)
                    {
                        var playerGridPos = mapScanner.WorldToGridPosition(player.transform.position);
                        if (playerGridPos == targetGridPos)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        return true;
                    }
                    continue;
                }
                
                // 检查面具：同样只有在同一格子时才算占用
                var mask = col.GetComponentInParent<MaskObject>();
                if (mask != null)
                {
                    if (hasMapScanner)
                    {
                        var maskGridPos = mapScanner.WorldToGridPosition(mask.transform.position);
                        if (maskGridPos == targetGridPos)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        return true;
                    }
                    continue;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// 获取所有可行走的格子
        /// </summary>
        private List<GridCell> GetWalkableCells(MapScanner scanner)
        {
            var cells = new List<GridCell>();
            var mapData = scanner.mapData;
            
            for (int x = 0; x < mapData.width; x++)
            {
                for (int y = 0; y < mapData.height; y++)
                {
                    var cell = mapData.grid[x, y];
                    if (cell != null && cell.isWalkable)
                    {
                        cells.Add(cell);
                    }
                }
            }
            
            return cells;
        }
        
        /// <summary>
        /// 清除场景中所有金币
        /// </summary>
        public void ClearAllCoins()
        {
            var coins = FindObjectsByType<Coin>(FindObjectsSortMode.None);
            foreach (var coin in coins)
            {
                Destroy(coin.gameObject);
            }
            Debug.Log($"[CoinSpawner] 清除了 {coins.Length} 个金币");
        }
    }
}