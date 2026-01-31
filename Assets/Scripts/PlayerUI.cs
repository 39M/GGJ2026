using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GGJ
{
    public class PlayerUI : MonoBehaviour
    {
        public Image bag;
        public TextMeshProUGUI score;
        public PlayerController pc;

        public void Init(PlayerController p)
        {
            pc = p;
            pc.UpdateUI += UpdateUI;
        }

        private void UpdateUI()
        {
            bag.color = pc.bagMask.GetCfg().TestColor;
            score.text = "Score : " + pc.curScore;
        }
    }
}