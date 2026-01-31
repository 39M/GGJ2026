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

        [Header("玩家生成")]
        [Tooltip("玩家(人类)生成点位，按游玩人数取前 N 个使用")]
        public List<Transform> PlayerStart = new();
        [Tooltip("AI 生成点位，一般与 PlayerStart 数量一致；按游玩人数取前 N 个使用")]
        public List<Transform> AIStart = new();
        public List<PlayerController> PlayerList = new();

        [Header("玩家数量")]
        [Tooltip("游玩人数(1~4)：人类与 AI 各 Spawn 前 N 个点位；也可由选人界面通过 SetHumanPlayerCountForNextGame 设置")]
        [Min(1)]
        public int NumberOfHumanPlayers = 2;

        /// <summary> 选人/主菜单设置后生效，下一局使用该人数；0 表示使用 Inspector 的 NumberOfHumanPlayers。 </summary>
        public static int OverrideHumanPlayerCount { get; set; }

        /// <summary> 选人界面调用：设置下一局人类玩家数量(1~4)，再加载对局场景。 </summary>
        public static void SetHumanPlayerCountForNextGame(int count)
        {
            OverrideHumanPlayerCount = Mathf.Clamp(count, 1, 4);
        }

        public PlayerController GetPlayer(int idx)
        {
            if (idx < 0 || idx >= PlayerList.Count) return null;
            return PlayerList[idx];
        }
        
        private void Start()
        {
            GameCfg.Instance.EventConfig.WaveDuration = MaxWaveTime;

            int N = OverrideHumanPlayerCount > 0
                ? Mathf.Clamp(OverrideHumanPlayerCount, 1, 4)
                : Mathf.Clamp(NumberOfHumanPlayers, 1, 4);
            N = Mathf.Min(N, PlayerStart.Count, AIStart.Count);
            if (N <= 0) return;

            var playerSprites = GameCfg.Instance.PlayerSprites;
            int idx = 0;

            for (int i = 0; i < N; i++)
            {
                var start = PlayerStart[i];
                var go = Instantiate(GameCfg.Instance.PlayerPrefab.gameObject, start.position, Quaternion.identity);
                var player = go.GetComponent<PlayerController>();
                UIManager.Instance.InitPlayer(player, idx);
                player.InitPlayer(idx);
                PlayerList.Add(player);
                if (playerSprites != null && idx < playerSprites.Length && playerSprites[idx] != null && player.mainSprite != null)
                    player.mainSprite.sprite = playerSprites[idx];
                idx++;
            }

            for (int i = 0; i < N; i++)
            {
                var start = AIStart[i];
                var go = Instantiate(GameCfg.Instance.PlayerPrefab.gameObject, start.position, Quaternion.identity);
                var player = go.GetComponent<PlayerController>();
                UIManager.Instance.InitPlayer(player, idx);
                player.InitPlayer(idx);
                PlayerList.Add(player);
                if (playerSprites != null && idx < playerSprites.Length && playerSprites[idx] != null && player.mainSprite != null)
                    player.mainSprite.sprite = playerSprites[idx];
                var ai = go.GetComponent<MaskAIController>();
                if (ai == null) ai = go.AddComponent<MaskAIController>();
                ai.enabled = true;
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