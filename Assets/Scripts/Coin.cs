using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace GGJ
{
    public class Coin : MonoBehaviour
    {
        public float score = 10;
        private void OnTriggerEnter2D(Collider2D other)
        {
            var pc = other.GetComponentInParent<PlayerController>();
            if (pc != null)
            {
                pc.GetScore(score);
                Destroy(gameObject);
            }
        }
    }
}