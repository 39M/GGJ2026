using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GGJ
{
    public class PlayerUI : MonoBehaviour
    {
        [Header("背包面具列表")]
        [LabelText("面具槽容器")]
        public Transform maskSlotContainer;
        [LabelText("槽背景图(空则用默认白块)")]
        public Sprite slotBackgroundSprite;
        [LabelText("单个槽尺寸")]
        public float slotSize = 32f;
        [LabelText("槽间距")]
        public float slotSpacing = 4f;
        [LabelText("当前面具高亮缩放")]
        public float currentSlotScale = 1.2f;
        [LabelText("非当前面具透明度")]
        [Range(0.3f, 1f)]
        public float otherSlotAlpha = 0.7f;

        [Header("跟随玩家")]
        [LabelText("相机(空则用Main)")]
        public Camera followCamera;
        [LabelText("头顶偏移(世界坐标)")]
        public Vector3 headOffset = new Vector3(0, 1.2f, 0);

        public TextMeshProUGUI score;
        
        [Header("警告效果")]
        [LabelText("警告图标")]
        public GameObject warningIcon;
        [LabelText("警告文本")]
        public TextMeshProUGUI warningText;
        
        [HideInInspector]
        public PlayerController pc;

        private List<Image> _maskSlots = new List<Image>();
        private RectTransform _rect;
        private Canvas _canvas;

        public void Init(PlayerController p)
        {
            pc = p;
            pc.UpdateUI += UpdateUI;
            _rect = transform as RectTransform;
            _canvas = GetComponentInParent<Canvas>();
            if (followCamera == null) followCamera = Camera.main;
            BuildMaskSlots();
            UpdateUI();
        }

        private void BuildMaskSlots()
        {
            if (maskSlotContainer == null || pc == null) return;
            foreach (Transform t in maskSlotContainer)
                Destroy(t.gameObject);
            _maskSlots.Clear();
            int count = pc.bagCapacity;
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject($"MaskSlot_{i}");
                go.transform.SetParent(maskSlotContainer, false);
                var rect = go.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(slotSize, slotSize);
                var le = go.AddComponent<LayoutElement>();
                le.preferredWidth = le.preferredHeight = slotSize;
                var img = go.AddComponent<Image>();
                img.sprite = slotBackgroundSprite != null ? slotBackgroundSprite : Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
                img.color = Color.gray;
                _maskSlots.Add(img);
            }
            var layout = maskSlotContainer.GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
            {
                layout = maskSlotContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = slotSpacing;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = layout.childControlHeight = true;
                layout.childForceExpandWidth = layout.childForceExpandHeight = false;
            }
            layout.spacing = slotSpacing;
        }

        private void UpdateUI()
        {
            if (pc == null || _maskSlots.Count == 0) return;
            var bag = pc.maskBag;
            if (bag == null) return;
            int wornIdx = Mathf.Clamp(pc.currentWornIndex, 0, bag.Count - 1);
            for (int i = 0; i < _maskSlots.Count; i++)
            {
                var slot = _maskSlots[i];
                bool isCurrent = (i == wornIdx);
                if (i < bag.Count)
                {
                    var mt = bag[i];
                    slot.gameObject.SetActive(true);
                    var cfg = mt == MaskType.None ? null : mt.GetCfg();
                    var c = mt == MaskType.None ? Color.gray : cfg.TestColor;
                    if (!isCurrent) c.a = otherSlotAlpha;
                    slot.color = c;
                    if (mt != MaskType.None && cfg != null)
                    {
                        if (cfg.MaskIcon != null)
                            slot.sprite = cfg.MaskIcon;
                        else if (cfg.MaskSprite != null)
                            slot.sprite = cfg.MaskSprite;
                    }
                    slot.transform.localScale = isCurrent ? Vector3.one * currentSlotScale : Vector3.one;
                }
                else
                {
                    slot.gameObject.SetActive(false);
                }
            }
            if (score != null)
                score.text = "Score : " + pc.curScore;
            
            // 更新警告状态
            UpdateWarningState();
        }
        
        /// <summary>
        /// 更新警告状态显示
        /// </summary>
        private void UpdateWarningState()
        {
            if (pc == null) return;
            
            // 警告图标
            if (warningIcon != null)
            {
                warningIcon.SetActive(pc.IsMarked);
            }
            
            // 警告文本
            if (warningText != null)
            {
                warningText.gameObject.SetActive(pc.IsMarked);
                if (pc.IsMarked)
                {
                    warningText.text = "危险！下波垫底将被淘汰！";
                }
            }
            
            // 如果被淘汰，隐藏整个UI
            if (pc.IsEliminated)
            {
                gameObject.SetActive(false);
            }
        }

        private void LateUpdate()
        {
            if (pc == null || _rect == null || _canvas == null) return;
            var canvasRect = _canvas.transform as RectTransform;
            if (canvasRect.lossyScale.x <= 0f || canvasRect.lossyScale.y <= 0f) return;
            var parentRect = _rect.parent as RectTransform;
            if (parentRect == null) return;
            var cam = followCamera != null ? followCamera : Camera.main;
            if (cam == null) return;
            var worldPos = pc.transform.position + headOffset;
            var screenPos = cam.WorldToScreenPoint(worldPos);
            // Overlay + CanvasScaler 时，必须用 Canvas 根做屏幕→本地，再变换到父节点空间，否则会往左下大偏
            Camera useCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : (_canvas.worldCamera != null ? _canvas.worldCamera : cam);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, useCamera, out var canvasLocalPos);
            var worldPoint = canvasRect.TransformPoint(canvasLocalPos);
            var parentLocalPos = parentRect.InverseTransformPoint(worldPoint);
            _rect.anchoredPosition = parentLocalPos;
        }

        private void OnDestroy()
        {
            if (pc != null) pc.UpdateUI -= UpdateUI;
        }
    }
}
