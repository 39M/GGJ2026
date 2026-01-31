using System;
using System.Collections.Generic;
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
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            Debug.Log("MaskObject Triggered: " + other.name);
            
            var coin = other.GetComponentInParent<Coin>();
            if (coin != null) return;
            var pc = other.GetComponentInParent<PlayerController>();
            if (pc != null && pc != owner)
            {
                // 打印面具被哪个玩家吃了
                Debug.Log($"Mask {mask} eaten by Player {pc.PlayerIdx}");
                pc.GetMask(mask);
            }

            if (pc == null || pc != owner)
            {
                //打印丢出去的面具碰到了其他玩家
                Debug.Log($"Mask {mask} hit Player {(pc != null ? pc.PlayerIdx.ToString() : "None")}");
                Destroy(gameObject);
            }
        }
    }
}