using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace GGJ
{
    /// <summary>
    /// 游戏结算界面
    /// </summary>
    public class GameResultUI : MonoBehaviour
    {
        [Header("UI组件")]
        [Tooltip("结算面板")]
        public CanvasGroup resultPanel;
        
        [Tooltip("胜利标题")]
        public TextMeshProUGUI titleText;
        
        [Tooltip("获胜玩家名称")]
        public TextMeshProUGUI winnerNameText;
        
        [Tooltip("获胜玩家分数")]
        public TextMeshProUGUI winnerScoreText;
        
        [Tooltip("排行榜容器")]
        public Transform rankingContainer;
        
        [Tooltip("排行榜条目预制体")]
        public GameObject rankingItemPrefab;
        
        [Tooltip("重新开始按钮")]
        public Button restartButton;
        
        [Tooltip("返回主菜单按钮")]
        public Button menuButton;
        
        [Header("动画设置")]
        [Tooltip("面板出现时间")]
        public float panelFadeInDuration = 0.5f;
        
        [Tooltip("排行榜条目出现间隔")]
        public float rankingItemDelay = 0.2f;
        
        private List<GameObject> _rankingItems = new List<GameObject>();
        
        private void Awake()
        {
            // 绑定按钮事件
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartClicked);
            }
            
            if (menuButton != null)
            {
                menuButton.onClick.AddListener(OnMenuClicked);
            }
        }
        
        /// <summary>
        /// 显示结算结果
        /// </summary>
        public void ShowResult(int winnerIndex)
        {
            // 清除旧的排行榜
            ClearRanking();
            
            // 设置标题
            if (titleText != null)
            {
                titleText.text = "游戏结束！";
            }
            
            // 设置获胜者信息
            var winner = GameManager.Instance?.GetPlayer(winnerIndex);
            if (winner != null)
            {
                if (winnerNameText != null)
                {
                    winnerNameText.text = $"玩家 {winnerIndex + 1} 获胜！";
                }
                
                if (winnerScoreText != null)
                {
                    winnerScoreText.text = $"最终分数: {winner.curScore:F0}";
                }
            }
            else
            {
                if (winnerNameText != null)
                {
                    winnerNameText.text = "平局！";
                }
                
                if (winnerScoreText != null)
                {
                    winnerScoreText.text = "";
                }
            }
            
            // 生成排行榜
            GenerateRanking();
            
            // 播放出现动画
            PlayShowAnimation();
        }
        
        /// <summary>
        /// 生成排行榜
        /// </summary>
        private void GenerateRanking()
        {
            if (rankingContainer == null || EliminationManager.Instance == null) return;
            
            var ranking = EliminationManager.Instance.GetPlayerRanking();
            
            for (int i = 0; i < ranking.Count; i++)
            {
                var player = ranking[i];
                GameObject item;
                
                if (rankingItemPrefab != null)
                {
                    item = Instantiate(rankingItemPrefab, rankingContainer);
                }
                else
                {
                    // 简单创建排行榜条目
                    item = CreateSimpleRankingItem(rankingContainer);
                }
                
                // 设置排行榜条目内容
                var texts = item.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length >= 1)
                {
                    string status = EliminationManager.Instance.IsPlayerAlive(player.PlayerIdx) ? "" : " (已淘汰)";
                    texts[0].text = $"#{i + 1} 玩家{player.PlayerIdx + 1}: {player.curScore:F0}分{status}";
                }
                
                // 初始隐藏，用于动画
                var canvasGroup = item.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = item.AddComponent<CanvasGroup>();
                }
                canvasGroup.alpha = 0;
                
                _rankingItems.Add(item);
            }
        }
        
        /// <summary>
        /// 创建简单的排行榜条目
        /// </summary>
        private GameObject CreateSimpleRankingItem(Transform parent)
        {
            var item = new GameObject("RankingItem");
            item.transform.SetParent(parent, false);
            
            var rect = item.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300, 40);
            
            var layout = item.AddComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.padding = new RectOffset(10, 10, 5, 5);
            
            // 添加背景
            var bg = item.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.3f);
            
            // 添加文本
            var textObj = new GameObject("Text");
            textObj.transform.SetParent(item.transform, false);
            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.fontSize = 24;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Left;
            
            var textRect = textObj.GetComponent<RectTransform>();
            textRect.sizeDelta = new Vector2(280, 30);
            
            return item;
        }
        
        /// <summary>
        /// 清除排行榜
        /// </summary>
        private void ClearRanking()
        {
            foreach (var item in _rankingItems)
            {
                if (item != null)
                {
                    Destroy(item);
                }
            }
            _rankingItems.Clear();
        }
        
        /// <summary>
        /// 播放显示动画
        /// </summary>
        private void PlayShowAnimation()
        {
            // 面板淡入
            if (resultPanel != null)
            {
                resultPanel.alpha = 0;
                resultPanel.transform.localScale = Vector3.one * 0.8f;
                
                resultPanel.DOFade(1, panelFadeInDuration);
                resultPanel.transform.DOScale(1, panelFadeInDuration).SetEase(Ease.OutBack);
            }
            
            // 排行榜条目依次出现
            for (int i = 0; i < _rankingItems.Count; i++)
            {
                var item = _rankingItems[i];
                var canvasGroup = item.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    float delay = panelFadeInDuration + i * rankingItemDelay;
                    canvasGroup.DOFade(1, 0.3f).SetDelay(delay);
                    
                    item.transform.localPosition += new Vector3(-50, 0, 0);
                    item.transform.DOLocalMoveX(item.transform.localPosition.x + 50, 0.3f)
                        .SetDelay(delay)
                        .SetEase(Ease.OutCubic);
                }
            }
        }
        
        /// <summary>
        /// 重新开始按钮点击
        /// </summary>
        private void OnRestartClicked()
        {
            GameManager.Instance?.Reload();
        }
        
        /// <summary>
        /// 返回主菜单按钮点击
        /// </summary>
        private void OnMenuClicked()
        {
            // TODO: 实现返回主菜单逻辑
            GameManager.Instance?.Reload();
        }
    }
}