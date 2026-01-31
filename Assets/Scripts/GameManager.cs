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
                var player = Instantiate(GameCfg.Instance.PlayerPrefab.gameObject, start.position, Quaternion.identity).GetComponent<PlayerController>();
                UIManager.Instance.InitPlayer(player, idx);
                player.InitPlayer(idx);
                PlayerList.Add(player);
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