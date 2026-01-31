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
        [Tooltip("发射者在此时间内不能拾取该面具(秒)，避免墙边立刻捡回；超时后可捡。掉落的面具 _firedBy 为 null 不受影响。")]
        public static float FiredByPickupBlockDuration = 2f;

        [Tooltip("大碰撞体(发射时命中玩家用)，不填则只有根上的小碰撞")]
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
            mainSprite.color = mask.GetCfg().TestColor;
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

        private void OnTriggerEnter2D(Collider2D other)
        {
            OnColliderTriggered(other, false);
        }

        public void OnColliderTriggered(Collider2D other, bool isLargeCollider)
        {
            if (other.CompareTag("Mask"))
                return;

            var coin = other.GetComponentInParent<Coin>();
            if (coin != null)
                return;

            if (other.CompareTag("Wall"))
            {
                if (!isLargeCollider)
                {
                    rig.linearVelocity = Vector2.zero;
                    owner = null;
                    _isFired = false;
                    if (largeCollider != null)
                        largeCollider.enabled = false;
                }
                return;
            }

            var pc = other.GetComponentInParent<PlayerController>();
            if (pc == null)
                return;
            if (pc == owner)
                return;
            // 发射者在 FiredByPickupBlockDuration 秒内不能拾取，避免墙边立刻捡回；超时后可捡。掉落的面具 _firedBy 为 null 不受影响。
            if (pc == _firedBy && Time.time - _firedTime < FiredByPickupBlockDuration)
                return;
            if (Time.time - _firedTime < 0.3f)
                return;

            bool shouldHit = isLargeCollider ? _isFired : !_isFired;
            if (!shouldHit)
                return;

            pc.GetMask(mask);
            Destroy(gameObject);
        }
    }
}