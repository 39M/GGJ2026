using System.Collections;
using TMPro;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Serialization;

namespace GGJ
{
    /// <summary>
    /// 游戏事件UI：显示倒计时、波次信息、事件提示
    /// </summary>
    public class GameEventUI : MonoBehaviour
    {
        [Header("UI组件")]
        [Tooltip("倒计时文本")]
        public TextMeshProUGUI countdownText;
        
        [Tooltip("波次文本")]
        public TextMeshProUGUI waveText;

        private GameObject eventHintNode;
        
        [Tooltip("事件提示（标题文本）")]
        public TextMeshProUGUI eventTitleText;
        
        [Tooltip("事件提示（描述文本）")]
        public TextMeshProUGUI eventDescriptionText;
        
        [Header("设置")]
        [Tooltip("事件提示显示时长")]
        public float eventHintDuration = 2f;
        
        [Tooltip("最后几秒变红警告")]
        public int warningSeconds = 5;
        
        [Tooltip("警告时的缩放动画")]
        public bool enableWarningAnimation = true;
        
        private float eventHintTimer = 0f;
        private Color normalColor = Color.white;
        private Color warningColor = Color.red;
        private Tween warningTween;
        
        private void Start()
        {
            // 订阅事件
            var eventSystem = GameEventManager.Instance;
            if (eventSystem != null)
            {
                eventSystem.OnWaveTimeUpdate += UpdateCountdown;
                eventSystem.OnWaveStart += OnWaveStart;
                eventSystem.OnWaveEnd += OnWaveEnd;
                eventSystem.OnEventPreview += OnEventPreview;
            }
            
            // 初始化UI
            if (eventTitleText != null)
            {
                eventHintNode = eventTitleText.transform.parent.gameObject;
                eventHintNode.SetActive(false);
            }
            
            if (countdownText != null)
            {
                normalColor = countdownText.color;
            }
        }
        
        private void OnDestroy()
        {
            // 停止动画
            warningTween?.Kill();
            
            // 取消订阅
            if (GameEventManager.Instance != null)
            {
                GameEventManager.Instance.OnWaveTimeUpdate -= UpdateCountdown;
                GameEventManager.Instance.OnWaveStart -= OnWaveStart;
                GameEventManager.Instance.OnWaveEnd -= OnWaveEnd;
                GameEventManager.Instance.OnEventPreview -= OnEventPreview;
            }
        }
        
        private void Update()
        {
            // 处理事件提示的显示时长
            if (eventHintTimer > 0)
            {
                eventHintTimer -= Time.deltaTime;
                if (eventHintTimer <= 0 && eventTitleText != null)
                {
                    HideEventHint();
                }
            }
        }
        
        /// <summary>
        /// 更新倒计时显示
        /// </summary>
        private void UpdateCountdown(float time)
        {
            if (countdownText == null) return;
            
            int seconds = Mathf.CeilToInt(time);
            countdownText.text = seconds.ToString();
            
            // 最后几秒变红警告
            if (seconds <= warningSeconds && seconds > 0)
            {
                countdownText.color = warningColor;
                
                // 缩放动画效果
                if (enableWarningAnimation && warningTween == null)
                {
                    warningTween = countdownText.transform
                        .DOScale(1.2f, 0.3f)
                        .SetLoops(-1, LoopType.Yoyo)
                        .SetEase(Ease.InOutSine);
                }
            }
            else
            {
                countdownText.color = normalColor;
                
                // 停止动画
                if (warningTween != null)
                {
                    warningTween.Kill();
                    warningTween = null;
                    countdownText.transform.localScale = Vector3.one;
                }
            }
        }
        
        /// <summary>
        /// 波次开始回调
        /// </summary>
        private void OnWaveStart(int wave)
        {
            if (waveText != null)
            {
                waveText.text = $"{wave}";
                
                // 波次开始动画
                waveText.transform.localScale = Vector3.one * 1.5f;
                waveText.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
            }
            
            // 重置倒计时颜色
            if (countdownText != null)
            {
                countdownText.color = normalColor;
                warningTween?.Kill();
                warningTween = null;
                countdownText.transform.localScale = Vector3.one;
            }
        }
        
        /// <summary>
        /// 波次结束回调
        /// </summary>
        private void OnWaveEnd(int wave)
        {
            // 波次结束可以添加特效
            Debug.Log($"[GameEventUI] 波次 {wave} 结束");
        }
        
        /// <summary>
        /// 事件预告回调
        /// </summary>
        private void OnEventPreview(GameEvent gameEvent)
        {
            ShowEventHint(gameEvent);
        }
        
        /// <summary>
        /// 显示事件提示
        /// </summary>
        public void ShowEventHint(GameEvent gameEvent)
        {
            if (eventTitleText == null) return;
            
            eventTitleText.text = gameEvent.EventName;
            eventDescriptionText.text = gameEvent.EventDescription;
            eventHintNode.SetActive(true);
            eventHintTimer = eventHintDuration;
            
            // 淡入动画
            var canvasGroup = eventHintNode.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0;
            canvasGroup.DOFade(1f, 0.3f);
            
            // 缩放动画
            canvasGroup.transform.localScale = Vector3.one * 0.5f;
            canvasGroup.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
        }
        
        /// <summary>
        /// 隐藏事件提示
        /// </summary>
        private void HideEventHint()
        {
            if (eventTitleText == null) return;
            
            // 淡出动画
            eventHintNode.GetComponent<CanvasGroup>().DOFade(0f, 0.3f).OnComplete(() =>
            {
                eventHintNode.SetActive(false);
            });
        }
    }
}