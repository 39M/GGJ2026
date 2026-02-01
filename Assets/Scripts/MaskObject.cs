using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GGJ
{
    public class MaskObject : MonoBehaviour
    {
        public MaskType mask;
        public TrailRenderer trail;
        public Rigidbody2D rig;
        public SpriteRenderer mainSprite;
        public Animation anim;
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
            //mainSprite.color = cfg.MainColor;
            rig.linearVelocity = speed;
            owner = own;
            _isFired = speed.sqrMagnitude > 0.01f;
            if (_isFired)
            {
                anim.Play("Mask_Fire");
                trail.gameObject.SetActive(true);
                trail.startColor = cfg.MainColor;
                _firedTime = Time.time;
                _firedBy = own; // 记录发射者，撞墙后也不清除，防止发射者再捡回
            }
            else
            {
                anim.Play("Mask_Show");
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

            // 墙只认小碰撞：大碰撞碰到墙不处理，等小碰撞碰到再停；停后移到附近空地，避免卡墙里
            if (other.CompareTag("Wall"))
            {
                if (isLargeCollider)
                    return;
                var dropPos = Utils.FindNearbyEmptyPosition((Vector2)transform.position - rig.linearVelocity * Time.fixedDeltaTime * 1.5f);
                rig.linearVelocity = Vector2.zero;
                owner = null;
                _isFired = false;
                if (largeCollider != null)
                    largeCollider.enabled = false;
                rig.MovePosition(dropPos);
                anim.Play("Mask_Normal");
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
            if (owner == null) pc.GetMask(mask); else pc.HurtMask(mask);
            Destroy(gameObject);
        }
    }
}