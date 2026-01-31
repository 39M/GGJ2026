using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GGJ
{

    [Serializable,HideReferenceObjectPicker,HideLabel]
    public class MaskCfg
    {
        [LabelText("测试颜色")]
        public Color TestColor = Color.white;
        [LabelText("基础速度")]
        public float Speed = 10;
        [LabelText("追逐速度")]
        public float EatSpeed = 10;
        [LabelText("得分倍率")]
        public float Score = 1;
        [LabelText("能飞")]
        public bool CanFly;
        [LabelText("能吃")] 
        public List<MaskType> CanEat;

        public LayerMask Layer => CanFly ? LayerMask.NameToLayer("Bird") : LayerMask.NameToLayer("Default");
    }
    
    // 使用示例
    [CreateAssetMenu(fileName = "GameCfg", menuName = "GGJ/GameCfg")]
    public class GameCfg : ResourceSingletonSO<GameCfg>
    {
        [LabelText("玩家Prefab")]
        public PlayerController PlayerPrefab;
        [LabelText("面具Prefab")]
        public MaskObject MaskPrefab;
        [LabelText("金币Prefab")]
        public Coin CoinPrefab;
        [LabelText("发射面具速度")]
        public float BulletSpeed;
        public Dictionary<MaskType, MaskCfg> MaskDefine;
        
        public MaskCfg GetMaskCfg(MaskType type) => MaskDefine[type];
    }
}