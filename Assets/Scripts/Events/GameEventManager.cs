using System;
using System.Collections;
using UnityEngine;

namespace GGJ
{
    /// <summary>
    /// 游戏事件系统：管理游戏内的各种事件和波次机制
    /// </summary>
    public class GameEventManager : MonoSingleton<GameEventManager>
    {
        [Header("运行状态")]
        [Tooltip("当前波次剩余时间")]
        public float currentWaveTime;
        
        [Tooltip("当前波次")]
        public int currentWave = 0;
        
        [Tooltip("是否正在运行")]
        public bool isRunning = false;
        
        // 事件回调
        /// <summary> 倒计时更新（参数：剩余时间） </summary>
        public Action<float> OnWaveTimeUpdate;
        /// <summary> 波次开始（参数：波次） </summary>
        public Action<int> OnWaveStart;
        /// <summary> 波次结束（参数：波次） </summary>
        public Action<int> OnWaveEnd;
        /// <summary> 事件执行 </summary>
        public Action<GameEvent> OnEventExecute;
        /// <summary> 事件预告 </summary>
        public Action<GameEvent> OnEventPreview;
        
        private Coroutine waveCoroutine;
        
        /// <summary> 获取事件配置 </summary>
        private GameEventConfig Config => GameCfg.Instance.EventConfig;
        
        private void Start()
        {
            // 等待 GameManager 初始化完成后启动
            StartCoroutine(WaitAndStartGameEventManager());
        }
        
        private IEnumerator WaitAndStartGameEventManager()
        {
            // 等待一帧，确保其他系统初始化完成
            yield return null;
            yield return null; // 多等一帧确保 MapScanner 也完成
            StartGameEventManager();
        }
        
        /// <summary>
        /// 开始游戏
        /// </summary>
        public void StartGameEventManager()
        {
            if (isRunning) return;
            
            Debug.Log("[GameEventSystem] 开始！");
            isRunning = true;
            currentWave = 0;
            
            // 开始第一波次
            StartNextWave();
        }
        
        /// <summary>
        /// 停止游戏
        /// </summary>
        public void StopGameEventManager()
        {
            if (!isRunning) return;
            
            Debug.Log("[GameEventSystem] 停止！");
            isRunning = false;
            
            if (waveCoroutine != null)
            {
                StopCoroutine(waveCoroutine);
                waveCoroutine = null;
            }
        }
        
        /// <summary>
        /// 暂停游戏
        /// </summary>
        public void PauseGame()
        {
            if (waveCoroutine != null)
            {
                StopCoroutine(waveCoroutine);
                waveCoroutine = null;
            }
        }
        
        /// <summary>
        /// 恢复游戏
        /// </summary>
        public void ResumeGame()
        {
            if (isRunning && waveCoroutine == null)
            {
                waveCoroutine = StartCoroutine(WaveCountdown());
            }
        }
        
        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// 开始下一波次
        /// </summary>
        private void StartNextWave()
        {
            currentWave++;
            currentWaveTime = Config.WaveDuration;
            
            Debug.Log($"[GameEventSystem] 波次 {currentWave} 开始！持续 {Config.WaveDuration} 秒");
            OnWaveStart?.Invoke(currentWave);
            
            // 执行金币投放事件
            ExecuteCoinSpawnEvent();
            
            // 启动倒计时
            if (waveCoroutine != null)
            {
                StopCoroutine(waveCoroutine);
            }
            waveCoroutine = StartCoroutine(WaveCountdown());
        }
        
        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// 波次倒计时协程
        /// </summary>
        private IEnumerator WaveCountdown()
        {
            while (currentWaveTime > 0)
            {
                yield return null;
                currentWaveTime -= Time.deltaTime;
                currentWaveTime = Mathf.Max(0, currentWaveTime);
                OnWaveTimeUpdate?.Invoke(currentWaveTime);
            }
            
            // 波次结束
            EndCurrentWave();
        }
        
        /// <summary>
        /// 结束当前波次
        /// </summary>
        private void EndCurrentWave()
        {
            Debug.Log($"[GameEventSystem] 波次 {currentWave} 结束！");
            OnWaveEnd?.Invoke(currentWave);
            
            // 检查游戏是否结束
            if (EliminationManager.Instance != null && EliminationManager.Instance.IsGameOver)
            {
                Debug.Log("[GameEventSystem] 游戏已结束，停止波次循环");
                return;
            }
            
            // 自动开始下一波次
            if (isRunning)
            {
                StartNextWave();
            }
        }
        
        /// <summary>
        /// 执行金币投放事件
        /// </summary>
        private void ExecuteCoinSpawnEvent()
        {
            int coinCount = (currentWave == 1) ? Config.FirstWaveCoins : Config.CoinsPerWave;
            var coinEvent = new CoinSpawnEvent(coinCount);
            
            // 触发预告
            OnEventPreview?.Invoke(coinEvent);
            coinEvent.Preview();
            
            // 执行事件
            OnEventExecute?.Invoke(coinEvent);
            coinEvent.Execute();
        }
        
        /// <summary>
        /// 手动触发事件（供外部调用）
        /// </summary>
        public void TriggerEvent(GameEvent gameEvent)
        {
            if (gameEvent == null) return;
            
            OnEventPreview?.Invoke(gameEvent);
            gameEvent.Preview();
            
            OnEventExecute?.Invoke(gameEvent);
            gameEvent.Execute();
        }
        
        /// <summary>
        /// 获取格式化的倒计时字符串
        /// </summary>
        public string GetFormattedTime()
        {
            int minutes = Mathf.FloorToInt(currentWaveTime / 60);
            int seconds = Mathf.FloorToInt(currentWaveTime % 60);
            return $"{minutes:00}:{seconds:00}";
        }
    }
}