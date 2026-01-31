using UnityEngine;

namespace GGJ
{
    /// <summary>
    /// 挂在面具的子碰撞体上，把触发事件转发给父级 MaskObject，并标明是大碰撞还是小碰撞。
    /// </summary>
    public class MaskColliderForwarder : MonoBehaviour
    {
        [Tooltip("true=大碰撞(发射时命中玩家)，false=小碰撞(拾取/撞墙)")]
        public bool isLarge;

        private void OnTriggerEnter2D(Collider2D other)
        {
            var mask = GetComponentInParent<MaskObject>();
            if (mask != null)
                mask.OnColliderTriggered(other, isLarge);
        }
    }
}
