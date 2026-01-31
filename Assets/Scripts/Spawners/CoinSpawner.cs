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
        
        /// <summary>
        /// 生成指定数量的金币
        /// </summary>
        /// <param name="count">金币数量</param>
        /// <returns>实际生成的金币列表</returns>
        public List<Coin> SpawnCoins(int count)
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
                
                // 添加小范围随机偏移，使金币位置不完全在格子中心
                var offset = new Vector2(
                    Random.Range(-0.3f, 0.3f),
                    Random.Range(-0.3f, 0.3f)
                );
                var spawnPos = worldPos + offset;
                
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
            
            Debug.Log($"[CoinSpawner] 成功生成 {spawned}/{count} 个金币，尝试次数: {attempts}");
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
        private bool IsPositionOccupied(Vector2 position, float checkRadius)
        {
            var colliders = Physics2D.OverlapCircleAll(position, checkRadius, overlapCheckLayers);
            
            foreach (var col in colliders)
            {
                // 有实体碰撞体（非Trigger），位置被占用
                if (!col.isTrigger)
                {
                    return true;
                }
                
                // 检查是否是金币、玩家或面具
                if (col.GetComponentInParent<Coin>() != null ||
                    col.GetComponentInParent<PlayerController>() != null ||
                    col.GetComponentInParent<MaskObject>() != null)
                {
                    return true;
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