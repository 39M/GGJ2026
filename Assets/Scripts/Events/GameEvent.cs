using System;
using UnityEngine;

namespace GGJ
{
    /// <summary>
    /// 游戏事件基类
    /// </summary>
    [Serializable]
    public abstract class GameEvent
    {
        /// <summary> 事件名称 </summary>
        public string EventName { get; protected set; }
        
        /// <summary> 事件描述 </summary>
        public string EventDescription { get; protected set; }
        
        /// <summary> 事件执行 </summary>
        public abstract void Execute();
        
        /// <summary> 事件预告（可选，用于UI提示） </summary>
        public virtual void Preview() { }
    }
}