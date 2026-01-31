using System;
using UnityEngine;

namespace GGJ
{
    /// <summary>
    /// 金币投放事件
    /// </summary>
    [Serializable]
    public class CoinSpawnEvent : GameEvent
    {
        public int CoinCount { get; private set; }
        
        public CoinSpawnEvent(int coinCount)
        {
            EventName = ">>> Bug 投放 <<<";
            EventDescription = "Bug 越多，工作量越饱和，绩效越高\n所以 Bug 越多，绩效越高";
            CoinCount = coinCount;
        }
        
        public override void Execute()
        {
            Debug.Log($"[GameEvent] 执行金币投放事件，数量: {CoinCount}");
            CoinSpawner.Instance.SpawnCoins(CoinCount);
        }
        
        public override void Preview()
        {
            Debug.Log($"[GameEvent] 即将投放 {CoinCount} 个金币！");
        }
    }
}