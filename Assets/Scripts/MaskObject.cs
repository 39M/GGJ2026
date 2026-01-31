using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GGJ
{
    public class MaskObject : MonoBehaviour
    {
        public MaskType mask;
        public Rigidbody2D rig;
        public SpriteRenderer mainSprite;
        [HideInInspector]
        public PlayerController owner;

        /// <summary> 发射者：谁发射的这枚面具，撞墙后也不清除。仅在该时间内禁止发射者拾取，避免墙边立刻捡回。 </summary>
        private PlayerController _firedBy;
        [Tooltip("大碰撞体子物体上的 Collider(发射时命中玩家用)，撞墙后由逻辑关闭。小碰撞在 SmallCollider 子物体上。")]
        public Collider2D largeCollider;

        private bool _isFired;
        private float _firedTime = -999f;

        private void Awake()
        {
            Init(mask);
        }

        public void Init(MaskType type, Vector2 speed = default, PlayerController own = null)
        {
            mask = type;
            var cfg = mask.GetCfg();
            if (cfg.MaskSprite != null)
                mainSprite.sprite = cfg.MaskSprite;
            mainSprite.color = cfg.TestColor;
            rig.linearVelocity = speed;
            owner = own;
            _isFired = speed.sqrMagnitude > 0.01f;
            if (_isFired)
            {
                _firedTime = Time.time;
                _firedBy = own; // 记录发射者，撞墙后也不清除，防止发射者再捡回
            }
            if (largeCollider != null)
                largeCollider.enabled = _isFired;
        }

        /// <summary> 由子物体 SmallCollider/LargeCollider 上的 MaskColliderForwarder 调用，isLargeCollider 由转发者标明。 </summary>
        public void OnColliderTriggered(Collider2D other, bool isLargeCollider)
        {
            if (other.CompareTag("Mask"))
                return;

            var coin = other.GetComponentInParent<Coin>();
            if (coin != null)
                return;

            // 墙只认小碰撞：大碰撞碰到墙不处理，等小碰撞碰到再停
            if (other.CompareTag("Wall"))
            {
                Debug.Log($"MaskObject: Hit Wall by {(isLargeCollider ? "LargeCollider" : "SmallCollider")}");
                if (isLargeCollider)
                {
                    Debug.Log("MaskObject: Ignoring wall hit on large collider.");
                    return;
                }
                rig.linearVelocity = Vector2.zero;
                owner = null;
                _isFired = false;
                Debug.Log("MaskObject: Stopped moving after hitting wall.");
                
                if (largeCollider != null)
                {
                    Debug.Log("MaskObject: Disabling large collider after hitting wall.");
                    largeCollider.enabled = false;
                }
                return;
            }

            var pc = other.GetComponentInParent<PlayerController>();
            if (pc == null)
            {
                Debug.Log("MaskObject: Triggered by non-player object, ignoring.");
                return;
            }

            if (pc == owner)
            {
                Debug.Log("MaskObject: Triggered by owner, ignoring.");
                return;
            }
            // 仅对发射者限时：配置的秒数内不能拾取（含刚发射同位置 + 墙边立刻捡回）。其他玩家无时间限制，打中即戴。
            float blockDuration = GameCfg.Instance.FiredByPickupBlockDuration;
            if (pc == _firedBy && Time.time - _firedTime < blockDuration)
            {
                Debug.Log("MaskObject: Triggered by firedBy within block duration, ignoring.");
                return;
            }

            bool shouldHit = isLargeCollider ? _isFired : !_isFired;
            if (!shouldHit)
            {
                Debug.Log("MaskObject: Collider type does not match state, ignoring.");
                return;
            }

            Debug.Log($"MaskObject: Picked up by player {pc.name} using {(isLargeCollider ? "LargeCollider" : "SmallCollider")}");
            pc.GetMask(mask);
            Destroy(gameObject);
        }
    }
}