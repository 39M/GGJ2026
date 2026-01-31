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
        [LabelText("只能吃大金币")]
        public bool OnlyEatBigCoin;

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
        [Header("吃掉玩家")]
        [LabelText("偷分比例(0~1，从被吃者扣取并加给吃者)")]
        [Range(0f, 1f)]
        public float EatStealRatio = 0.5f;
        [LabelText("吃人者眩晕时间(秒)")]
        public float EatStunDuration = 1.5f;
        [LabelText("被吃者弹开距离(格，防止短时间反复触发)")]
        public float EatPushDistance = 2f;
        public Dictionary<MaskType, MaskCfg> MaskDefine;
        
        public MaskCfg GetMaskCfg(MaskType type) => MaskDefine[type];
    }
}