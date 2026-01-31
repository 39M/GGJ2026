using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GGJ
{
    public class GameManager : MonoSingleton<GameManager>
    {
        [Header("教学场景设置")]
        [Tooltip("是否为教学场景(如果不是,所有检测逻辑都不会运行)")]
        public bool enableTutorialCheck = true;
        public float MaxWaveTime = 10f;

        public List<Transform> PlayerStart = new();
        public List<PlayerController> PlayerList = new();

        [Header("AI 玩家")]
        [Tooltip("使用 AI 控制的玩家索引（0-based），如 [1] 表示第 2 个玩家为人机；运行时动态挂 MaskAIController")]
        public List<int> AIPlayerIndices = new();
    
        public PlayerController GetPlayer(int idx)
        {
            if (idx < 0 || idx >= PlayerList.Count) return null;
            return PlayerList[idx];
        }
        
        private void Start()
        {
            GameCfg.Instance.EventConfig.WaveDuration = MaxWaveTime;
            var idx = 0;
            foreach (var start in PlayerStart)
            {
                var go = Instantiate(GameCfg.Instance.PlayerPrefab.gameObject, start.position, Quaternion.identity);
                var player = go.GetComponent<PlayerController>();
                UIManager.Instance.InitPlayer(player, idx);
                player.InitPlayer(idx);
                PlayerList.Add(player);

                if (AIPlayerIndices.Contains(idx))
                {
                    var ai = go.GetComponent<MaskAIController>();
                    if (ai == null) ai = go.AddComponent<MaskAIController>();
                    ai.enabled = true;
                }

                idx++;
            }
            
            // 初始化淘汰管理器
            EliminationManager.Instance.InitializePlayers();
            
            GameEventManager.Instance.StartGameEventManager();
        }

        public void Reload()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}