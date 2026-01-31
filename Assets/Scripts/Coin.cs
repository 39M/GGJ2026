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

        private void OnTriggerEnter2D(Collider2D other)
        {
            var pc = other.GetComponentInParent<PlayerController>();
            if (pc == null) return;
            if (!pc.CanEatCoin(coinType)) return;
            pc.GetScore(score);
            Destroy(gameObject);
        }
    }
}