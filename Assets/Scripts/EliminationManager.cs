using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Serialization;

namespace GGJ
{
    /// <summary>
    /// 淘汰管理器：负责末尾淘汰逻辑
    /// </summary>
    public class EliminationManager : MonoSingleton<EliminationManager>
    {
        [Header("淘汰设置")]
        [Tooltip("被淘汰玩家飞出屏幕的力度")]
        public float eliminateForce = 10f;
        
        [Tooltip("闪红频率（每秒闪烁次数）")]
        public float warningFlashRate = 4f;
        
        [Tooltip("闪红持续时间（秒）")]
        public float warningFlashDuration = 3f;
        
        [Tooltip("画面冻结持续时间（真实时间）")]
        public float freezeDuration = 2f;
        
        [Tooltip("镜头拉近目标尺寸（正交相机Size）")]
        public float zoomInSize = 5f;
        
        [Tooltip("镜头拉近时间")]
        public float zoomInDuration = 0.3f;
        
        [Tooltip("镜头恢复时间")]
        public float zoomOutDuration = 3f;
        
        /// <summary> 原始相机尺寸 </summary>
        private float _originalCameraSize;
        
        /// <summary> 原始时间缩放 </summary>
        private float _originalTimeScale = 1f;

        /// <summary> 当前被标记的玩家索引，-1表示无人被标记 </summary>
        private int _markedPlayerIndex = -1;
        
        /// <summary> 存活玩家列表 </summary>
        private List<PlayerController> _alivePlayers = new List<PlayerController>();
        
        /// <summary> 游戏是否结束 </summary>
        public bool IsGameOver { get; private set; } = false;
        
        /// <summary> 获胜玩家 </summary>
        public PlayerController Winner { get; private set; } = null;
        
        // 事件回调
        /// <summary> 玩家被标记（参数：玩家索引） </summary>
        public Action<int> OnPlayerMarked;
        /// <summary> 玩家取消标记（参数：玩家索引） </summary>
        public Action<int> OnPlayerUnmarked;
        /// <summary> 玩家被淘汰（参数：玩家索引） </summary>
        public Action<int> OnPlayerEliminated;
        /// <summary> 游戏结束（参数：获胜玩家索引） </summary>
        public Action<int> OnGameOver;
        
        private void Start()
        {
            // 订阅波次结束事件
            if (GameEventManager.Instance != null)
            {
                GameEventManager.Instance.OnWaveEnd += OnWaveEnd;
            }
        }
        
        private void OnDestroy()
        {
            if (GameEventManager.Instance != null)
            {
                GameEventManager.Instance.OnWaveEnd -= OnWaveEnd;
            }
            
            // 确保时间缩放恢复正常
            ResetTimeScale();
        }
        
        /// <summary>
        /// 重置时间缩放
        /// </summary>
        private void ResetTimeScale()
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }
        
        /// <summary>
        /// 初始化存活玩家列表
        /// </summary>
        public void InitializePlayers()
        {
            _alivePlayers.Clear();
            _alivePlayers.AddRange(GameManager.Instance.PlayerList);
            _markedPlayerIndex = -1;
            IsGameOver = false;
            Winner = null;
        }
        
        /// <summary>
        /// 波次结束时的处理
        /// </summary>
        private void OnWaveEnd(int wave)
        {
            if (IsGameOver) return;
            
            // 初始化存活玩家列表（如果还没有）
            if (_alivePlayers.Count == 0)
            {
                InitializePlayers();
            }
            
            // 执行淘汰检查
            CheckElimination();
        }
        
        /// <summary>
        /// 检查并执行淘汰逻辑
        /// </summary>
        private void CheckElimination()
        {
            if (_alivePlayers.Count <= 1)
            {
                // 游戏结束
                EndGame();
                return;
            }
            
            // 找到分数最低的玩家
            var lowestScorePlayer = FindLowestScorePlayer();
            
            if (lowestScorePlayer == null) return;
            
            int lowestPlayerIndex = lowestScorePlayer.PlayerIdx;
            
            // 检查是否有多人同分垫底（同分不标记）
            if (HasTiedLowestScore(lowestScorePlayer))
            {
                // 同分，取消之前的标记
                if (_markedPlayerIndex >= 0)
                {
                    CancelMark(_markedPlayerIndex);
                }
                Debug.Log($"[EliminationManager] 波次结束，最低分有多人同分，不标记");
                return;
            }
            
            // 检查是否是被标记的玩家仍然垫底
            if (_markedPlayerIndex >= 0 && _markedPlayerIndex == lowestPlayerIndex)
            {
                // 被标记的玩家连续两波垫底，淘汰！
                Debug.Log($"[EliminationManager] 玩家 {lowestPlayerIndex} 连续垫底，被淘汰！");
                EliminatePlayer(lowestScorePlayer);
            }
            else
            {
                // 取消之前的标记
                if (_markedPlayerIndex >= 0 && _markedPlayerIndex != lowestPlayerIndex)
                {
                    CancelMark(_markedPlayerIndex);
                }
                
                // 标记新的垫底玩家
                MarkPlayer(lowestScorePlayer);
            }
        }
        
        /// <summary>
        /// 查找分数最低的存活玩家
        /// </summary>
        private PlayerController FindLowestScorePlayer()
        {
            if (_alivePlayers.Count == 0) return null;
            
            PlayerController lowest = _alivePlayers[0];
            foreach (var player in _alivePlayers)
            {
                if (player.curScore < lowest.curScore)
                {
                    lowest = player;
                }
            }
            return lowest;
        }
        
        /// <summary>
        /// 检查是否有多人同分垫底
        /// </summary>
        private bool HasTiedLowestScore(PlayerController lowestPlayer)
        {
            int count = 0;
            foreach (var player in _alivePlayers)
            {
                if (Mathf.Approximately(player.curScore, lowestPlayer.curScore))
                {
                    count++;
                }
            }
            return count > 1;
        }
        
        /// <summary>
        /// 标记玩家（垫底警告）
        /// </summary>
        private void MarkPlayer(PlayerController player)
        {
            _markedPlayerIndex = player.PlayerIdx;
            player.SetMarked(true);
            OnPlayerMarked?.Invoke(player.PlayerIdx);
            Debug.Log($"[EliminationManager] 玩家 {player.PlayerIdx} 被标记为垫底，下波若仍垫底将被淘汰！");
            
            // 触发闪红效果
            StartCoroutine(FlashWarningEffect(player));
        }
        
        /// <summary>
        /// 取消玩家标记
        /// </summary>
        private void CancelMark(int playerIndex)
        {
            var player = GameManager.Instance.GetPlayer(playerIndex);
            if (player != null)
            {
                player.SetMarked(false);
            }
            OnPlayerUnmarked?.Invoke(playerIndex);
            _markedPlayerIndex = -1;
            Debug.Log($"[EliminationManager] 玩家 {playerIndex} 标记已取消");
        }
        
        /// <summary>
        /// 淘汰玩家
        /// </summary>
        private void EliminatePlayer(PlayerController player)
        {
            _markedPlayerIndex = -1;
            _alivePlayers.Remove(player);
            player.SetMarked(false);
            player.SetEliminated(true);
            
            OnPlayerEliminated?.Invoke(player.PlayerIdx);
            
            // 播放踢飞效果
            StartCoroutine(EliminateEffect(player));
            
            // 检查游戏是否结束
            if (_alivePlayers.Count <= 1)
            {
                StartCoroutine(DelayedEndGame(1f));
            }
        }
        
        /// <summary>
        /// 闪红警告效果协程
        /// </summary>
        private IEnumerator FlashWarningEffect(PlayerController player)
        {
            float elapsed = 0f;
            float flashInterval = 1f / warningFlashRate;
            bool isRed = false;
            Color originalColor = player.mainSprite.color;
            
            while (elapsed < warningFlashDuration && player.IsMarked)
            {
                isRed = !isRed;
                player.mainSprite.color = isRed ? Color.red : originalColor;
                yield return new WaitForSeconds(flashInterval);
                elapsed += flashInterval;
            }
            
            // 恢复原色（如果还是标记状态则保持半红）
            if (player.IsMarked)
            {
                player.mainSprite.color = Color.Lerp(originalColor, Color.red, 0.5f);
            }
            else
            {
                player.mainSprite.color = originalColor;
            }
        }
        
        /// <summary>
        /// 淘汰踢飞效果协程
        /// </summary>
        private IEnumerator EliminateEffect(PlayerController player)
        {
            // 禁用玩家输入
            player.enabled = false;
            
            var mainCamera = Camera.main;
            
            // 保存原始相机尺寸
            _originalCameraSize = mainCamera.orthographicSize;
            _originalTimeScale = Time.timeScale;
            
            // ========== 阶段1：时停 + 镜头拉近 ==========
            
            // 时间停止
            Time.timeScale = 0;
            Time.fixedDeltaTime = 0;
            Vector2 cameraCenter = mainCamera.transform.position;

            // 镜头拉近到被淘汰玩家（使用 unscaledTime 确保不受 timeScale 影响）
            Vector3 targetCameraPos = new Vector3(player.transform.position.x, player.transform.position.y, mainCamera.transform.position.z);
            Vector3 originalCameraPos = mainCamera.transform.position;
            
            mainCamera.transform.DOMove(targetCameraPos, zoomInDuration).SetUpdate(true).SetEase(Ease.OutCubic);
            mainCamera.DOOrthoSize(zoomInSize, zoomInDuration).SetUpdate(true).SetEase(Ease.OutCubic);
            
            // 慢动作持续时间
            yield return new WaitForSecondsRealtime(freezeDuration);
            
            // ========== 阶段2：踢飞动画 ==========
            
            // 计算踢飞方向（从屏幕中心向外）
            Vector2 playerPos = player.transform.position;
            Vector2 direction = (playerPos - cameraCenter).normalized;
            if (direction.sqrMagnitude < 0.01f)
            {
                direction = Vector2.up;
            }
            
            // 施加力踢飞（力度需要考虑慢动作）
            player.rig.linearVelocity = Vector2.zero;
            player.rig.AddForce(direction * eliminateForce, ForceMode2D.Impulse);
            
            // 旋转效果（使用 unscaledTime）
            player.transform.DORotate(new Vector3(0, 0, 720), 0.5f, RotateMode.FastBeyond360)
                .SetUpdate(true)
                .SetLoops(-1)
                .SetEase(Ease.Linear);
            
            // ========== 阶段3：恢复正常 ==========
            
            // 恢复时间缩放
            Time.timeScale = _originalTimeScale;
            Time.fixedDeltaTime = 0.02f;
            
            // 镜头恢复
            mainCamera.transform.DOMove(originalCameraPos, zoomOutDuration).SetUpdate(true).SetEase(Ease.OutCubic);
            mainCamera.DOOrthoSize(_originalCameraSize, zoomOutDuration).SetUpdate(true).SetEase(Ease.OutCubic);
            
            yield return new WaitForSecondsRealtime(zoomOutDuration);
            
            // 隐藏玩家
            player.gameObject.SetActive(false);
        }
        
        /// <summary>
        /// 延迟结束游戏
        /// </summary>
        private IEnumerator DelayedEndGame(float delay)
        {
            yield return new WaitForSeconds(delay);
            EndGame();
        }
        
        /// <summary>
        /// 结束游戏
        /// </summary>
        private void EndGame()
        {
            if (IsGameOver) return;
            
            IsGameOver = true;
            
            // 确保时间缩放恢复正常
            ResetTimeScale();
            
            // 停止事件系统
            GameEventManager.Instance?.StopGameEventManager();
            
            // 确定获胜者
            if (_alivePlayers.Count == 1)
            {
                Winner = _alivePlayers[0];
                Debug.Log($"[EliminationManager] 游戏结束！获胜者: 玩家 {Winner.PlayerIdx}，分数: {Winner.curScore}");
            }
            else if (_alivePlayers.Count == 0)
            {
                // 所有人都被淘汰（理论上不应该发生）
                Debug.Log("[EliminationManager] 游戏结束！无获胜者");
            }
            else
            {
                // 多人存活，选分数最高的
                Winner = _alivePlayers.OrderByDescending(p => p.curScore).First();
                Debug.Log($"[EliminationManager] 游戏结束！获胜者: 玩家 {Winner.PlayerIdx}，分数: {Winner.curScore}");
            }
            
            int winnerIndex = Winner != null ? Winner.PlayerIdx : -1;
            OnGameOver?.Invoke(winnerIndex);
            
            // 显示结算界面
            UIManager.Instance?.ShowGameResult(winnerIndex);
        }
        
        /// <summary>
        /// 获取玩家排名列表（按分数降序）
        /// </summary>
        public List<PlayerController> GetPlayerRanking()
        {
            var allPlayers = new List<PlayerController>(GameManager.Instance.PlayerList);
            return allPlayers.OrderByDescending(p => p.curScore).ToList();
        }
        
        /// <summary>
        /// 检查玩家是否存活
        /// </summary>
        public bool IsPlayerAlive(int playerIndex)
        {
            return _alivePlayers.Any(p => p.PlayerIdx == playerIndex);
        }
        
        /// <summary>
        /// 获取存活玩家数量
        /// </summary>
        public int GetAlivePlayerCount()
        {
            return _alivePlayers.Count;
        }
    }
}