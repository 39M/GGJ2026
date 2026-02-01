using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GGJ
{
    public enum CoinType
    {
        Small,
        Big
    }

    public class Coin : MonoBehaviour
    {
        [LabelText("金币类型")]
        public CoinType coinType = CoinType.Small;
        [LabelText("得分")]
        public float score = 10;
        [LabelText("动画")]
        public Animation anim;
        
        private void Awake()
        {
            anim = GetComponent<Animation>();
            anim.Play("Coin_Show");
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var pc = other.GetComponentInParent<PlayerController>();
            if (pc == null) return;
            if (!pc.CanEatCoin(coinType)) return;
            pc.GetScore(score);
            anim.Play("Coin_Get");
            GetComponent<Collider2D>().enabled = false;
            Destroy(gameObject, 1);
        }
    }
}