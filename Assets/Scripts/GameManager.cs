using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GGJ
{
    public class GameManager : MonoSingleton<GameManager>
    {
        public List<Transform> PlayerStart = new();
        public List<PlayerController> PlayerList = new();

        public PlayerController GetPlayer(int idx)
        {
            if (idx < 0 || idx >= PlayerList.Count) return null;
            return PlayerList[idx];
        }
        
        private void Start()
        {
            var idx = 0;
            foreach (var start in PlayerStart)
            {
                var player = Instantiate(GameCfg.Instance.PlayerPrefab.gameObject, start.position, Quaternion.identity).GetComponent<PlayerController>();
                UIManager.Instance.InitPlayer(player, idx);
                player.InitPlayer(idx);
                PlayerList.Add(player);
                idx++;
            }
            
            GameEventManager.Instance.StartGameEventManager();
        }

        public void Reload()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}