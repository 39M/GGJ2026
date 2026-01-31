using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GGJ
{
    public class UIManager : MonoSingleton<UIManager>
    {
        public List<PlayerUI> PlayerUIList;
        public Button ReloadBtn;
        
        [Header("结算界面")]
        [Tooltip("结算界面面板")]
        public GameResultUI gameResultUI;

        public void InitPlayer(PlayerController pc, int idx)
        {
            if (idx < 0 || idx >= PlayerUIList.Count || PlayerUIList[idx] == null) Debug.LogError("PlayerUI列表不足");
            PlayerUIList[idx].Init(pc);
        }
        
        void Start()
        {
            ReloadBtn.onClick.AddListener(() => GameManager.Instance.Reload());
            
            // 隐藏结算界面
            if (gameResultUI != null)
            {
                gameResultUI.gameObject.SetActive(false);
            }
        }
        
        /// <summary>
        /// 显示游戏结算界面
        /// </summary>
        public void ShowGameResult(int winnerIndex)
        {
            if (gameResultUI != null)
            {
                gameResultUI.gameObject.SetActive(true);
                gameResultUI.ShowResult(winnerIndex);
            }
        }
        
        /// <summary>
        /// 隐藏游戏结算界面
        /// </summary>
        public void HideGameResult()
        {
            if (gameResultUI != null)
            {
                gameResultUI.gameObject.SetActive(false);
            }
        }
    }
}