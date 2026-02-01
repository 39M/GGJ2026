using System;
using System.Collections.Generic;
using DamageNumbersPro;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace GGJ
{

    [Serializable,HideReferenceObjectPicker,HideLabel]
    public class MaskCfg
    {
        [FormerlySerializedAs("TestColor")] [LabelText("主颜色")]
        public Color MainColor = Color.white;
        [LabelText("面具图片(场景/玩家戴面具时显示，不填则沿用预制体原有图)")]
        public Sprite MaskSprite;
        [LabelText("面具图标(UI 背包槽显示，不填则用面具图片)")]
        public Sprite MaskIcon;
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
        [LabelText("能吃大金币")]
        public bool CanEatBigCoin = true;
        [LabelText("能吃小金币")]
        public bool CanEatSmallCoin = true;

        [LabelText("掉落金币间隔(秒)，0=不掉落")]
        public float DropCoinInterval;
        [LabelText("每次掉落金币数(分数)")]
        public float DropCoinAmount;
        [LabelText("掉落金币Prefab(不填则用全局CoinPrefab，功能与场景金币一致)")]
        public Coin DropCoinPrefab;

        public LayerMask Layer => CanFly ? LayerMask.NameToLayer("Bird") : LayerMask.NameToLayer("Default");
    }
    
    /// <summary>
    /// 金币生成模式
    /// </summary>
    public enum CoinSpawnMode
    {
        [LabelText("随机格子")]
        RandomGrid,         // 在可行走格子中随机生成
        [LabelText("预设位置")]
        PresetPositions     // 从关卡预设的 Coin 位置中随机生成
    }
    
    /// <summary>
    /// 游戏事件配置
    /// </summary>
    [Serializable, HideReferenceObjectPicker]
    public class GameEventConfig
    {
        [LabelText("每波持续时间（秒）（请在关卡里 Override）")]
        public float WaveDuration = 30f;

        [LabelText("生成模式")]
        public CoinSpawnMode SpawnMode = CoinSpawnMode.PresetPositions;
        
        [LabelText("第一波金币数量")]
        public int FirstWaveCoins = 15;
        
        [LabelText("每波金币数量")]
        public int CoinsPerWave = 10;
        
        [LabelText("大金币生成比例")]
        [Range(0f, 1f)]
        public float BigCoinRatio = 0.1f;
        
        [LabelText("避免重叠的检测半径")]
        public float CoinCheckRadius = 0.4f;
        
        [LabelText("单次生成最大尝试次数")]
        public int MaxSpawnAttempts = 100;
    }
    
    // 使用示例
    [CreateAssetMenu(fileName = "GameCfg", menuName = "GGJ/GameCfg")]
    public class GameCfg : ResourceSingletonSO<GameCfg>
    {
        [TabGroup("基础配置")]
        [LabelText("玩家Prefab")]
        public PlayerController PlayerPrefab;
        
        [TabGroup("基础配置")]
        [LabelText("面具Prefab")]
        public MaskObject MaskPrefab;
        
        [TabGroup("基础配置")]
        [LabelText("金币Prefab")]
        public Coin CoinPrefab;

        [TabGroup("基础配置")]
        [LabelText("玩家外观(最多4个，索引0~3对应玩家1~4；不填则用Prefab默认)")]
        public Sprite[] PlayerSprites = new Sprite[4];
        
        [TabGroup("基础配置")]
        [LabelText("大金币Prefab")]
        public Coin BigCoinPrefab;

        [TabGroup("基础配置")]
        [LabelText("获得分数眺字")]
        public DamageNumber GetScoreNumber;
        
        [TabGroup("基础配置")]
        [LabelText("发射面具速度")]
        public float BulletSpeed;

        [TabGroup("基础配置")]
        [LabelText("发射面具冷却(秒)：该时间内不能再次发射")]
        public float FireMaskCooldown = 0.5f;

        [TabGroup("基础配置")]
        [LabelText("发射者拾取冷却(秒)：该时间内发射者不能拾取自己发射的面具，避免墙边立刻捡回")]
        public float FiredByPickupBlockDuration = 2f;
        
        [TabGroup("基础配置")]
        [Header("吃掉玩家")]
        [LabelText("偷分比例(0~1，从被吃者扣取并加给吃者)")]
        [Range(0f, 1f)]
        public float EatStealRatio = 0.5f;
        
        [TabGroup("基础配置")]
        [LabelText("眩晕时间(秒)，")]
        public float EatStunDuration = 1.5f;
        
        [TabGroup("基础配置")]
        [LabelText("被吃者弹开距离(格，防止短时间反复触发)")]
        public float EatPushDistance = 2f;
        
        [TabGroup("面具配置")]
        public Dictionary<MaskType, MaskCfg> MaskDefine;
        
        [TabGroup("事件配置")]
        [LabelText("游戏事件设置")]
        public GameEventConfig EventConfig = new GameEventConfig();
        
        public MaskCfg GetMaskCfg(MaskType type) => MaskDefine[type];
    }
}