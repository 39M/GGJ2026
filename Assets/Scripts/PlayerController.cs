using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;


namespace GGJ
{
    public enum Direction
    {
        None,
        Up,
        Down,
        Left,
        Right,
    }

    public enum MaskType
    {
        None,
        Tiger,
        Sheep,
        Bird
    }

    public static class Utils
    {
        public const float GridSize = 1; 
        
        private static Dictionary<Direction, Vector2> DirMap = new()
        {
            [Direction.None] = new Vector2(0, 0),
            [Direction.Up] = new Vector2(0, 1),
            [Direction.Down] = new Vector2(0, -1),
            [Direction.Left] = new Vector2(-1, 0),
            [Direction.Right] = new Vector2(1, 0),
        };

        public static void SetAllLayer(this GameObject target, int layer)
        {
            if (target == null) return;

            target.layer = layer;

            foreach (Transform child in target.transform)
            {
                if (child == null) continue;
                SetAllLayer(child.gameObject, layer);
            }
        }
        
        public static Vector2 GetVec(this Direction dir)
        {
            return DirMap[dir];
        }
        
        public static MaskCfg GetCfg(this MaskType mask)
        {
            return GameCfg.Instance.GetMaskCfg(mask);
        }
    }
    

    public class PlayerController : MonoBehaviour
    {
        public int PlayerIdx { get; private set; } = 0;
        public Rigidbody2D rig;
        public SpriteRenderer mainSprite;
        
        /// <summary> 面具列表：0号位、1号位… 切换键会按 0→1→2→… 循环戴。 </summary>
        [LabelText("面具列表")]
        public List<MaskType> maskBag = new List<MaskType> { MaskType.None };
        [LabelText("背包容量(包含当前戴的)")]
        public int bagCapacity = 3;
        /// <summary> 当前戴的是第几号位（0、1、2…），切换键会切到下一个号位。 </summary>
        [LabelText("当前佩戴槽位索引")]
        public int currentWornIndex = 0;

        /// <summary> 当前戴在脸上的面具，即 maskBag[currentWornIndex]。 </summary>
        public MaskType currentMask => (maskBag != null && maskBag.Count > 0) ? maskBag[Mathf.Clamp(currentWornIndex, 0, maskBag.Count - 1)] : MaskType.None;
        /// <summary> 背包里下一个面具（摘下当前后会戴上的），供 UI 等使用。 </summary>
        public MaskType bagPreviewMask => (maskBag != null && maskBag.Count > 1) ? maskBag[1] : MaskType.None;
        [LabelText("基础速度")] 
        public float speed = 0;
        [LabelText("追逐速度")] 
        public float eatSpeed = 0;
        [LabelText("得分倍率")] 
        public float scoreMulti = 1;
        [LabelText("移动方向")]
        public Direction curDirection = Direction.Up;
        [LabelText("当前得分")]
        public float curScore = 0;

        public Vector2 FinalSpeed => (DirHasFood() ? eatSpeed : speed) * curDirection.GetVec();

        public Action UpdateUI;
        
        /// <summary> 把指定面具设为当前佩戴槽位并应用属性（速度、层级等）。 </summary>
        public void SetCurrentMask(MaskType mask)
        {
            if (maskBag == null) maskBag = new List<MaskType>();
            while (maskBag.Count <= currentWornIndex)
            {
                Debug.LogWarning("Mask bag size less than current worn index, expanding bag with None masks.");
                maskBag.Add(MaskType.None);
            }
            maskBag[currentWornIndex] = mask;
            var cfg = mask.GetCfg();
            speed = cfg.Speed;
            eatSpeed = cfg.EatSpeed;
            scoreMulti = cfg.Score;
            gameObject.SetAllLayer(cfg.Layer);
            mainSprite.color = cfg.TestColor;
            UpdateUI?.Invoke();
        }

        /// <summary> 在玩家附近找一个空地位置（无碰撞体的点），用于掉落面具。 </summary>
        private Vector2 FindNearbyEmptyPosition()
        {
            var center = (Vector2)transform.position;
            var grid = Utils.GridSize;
            var offsets = new[]
            {
                new Vector2(grid, 0), new Vector2(-grid, 0), new Vector2(0, grid), new Vector2(0, -grid),
                new Vector2(grid, grid), new Vector2(-grid, grid), new Vector2(grid, -grid), new Vector2(-grid, -grid),
                new Vector2(2 * grid, 0), new Vector2(-2 * grid, 0), new Vector2(0, 2 * grid), new Vector2(0, -2 * grid),
            };
            foreach (var off in offsets)
            {
                var p = center + off;
                if (!Physics2D.OverlapPoint(p))
                    return p;
            }
            return center + new Vector2(grid, 0);
        }

        [LabelText("掉落面具最小距离(格)")]
        public float dropMinDistance = 3f;
        /// <summary> 找一个离玩家足够远的空位用于掉落面具，避免刚掉就被自己触发拾取。 </summary>
        private Vector2 FindDropPositionFarEnough()
        {
            var center = (Vector2)transform.position;
            var grid = Utils.GridSize;
            float minDist = dropMinDistance * grid;
            // 只尝试距离 >= dropMinDistance 格的点，先近后远
            var offsets = new[]
            {
                new Vector2(3 * grid, 0), new Vector2(-3 * grid, 0), new Vector2(0, 3 * grid), new Vector2(0, -3 * grid),
                new Vector2(3 * grid, 3 * grid), new Vector2(-3 * grid, 3 * grid), new Vector2(3 * grid, -3 * grid), new Vector2(-3 * grid, -3 * grid),
                new Vector2(2 * grid, 0), new Vector2(-2 * grid, 0), new Vector2(0, 2 * grid), new Vector2(0, -2 * grid),
                new Vector2(2 * grid, 2 * grid), new Vector2(-2 * grid, 2 * grid), new Vector2(2 * grid, -2 * grid), new Vector2(-2 * grid, -2 * grid),
            };
            foreach (var off in offsets)
            {
                if (off.magnitude < minDist) continue;
                var p = center + off;
                if (!Physics2D.OverlapPoint(p))
                    return p;
            }
            return center + new Vector2(3 * grid, 0);
        }

        /// <summary> 将指定类型的面具生成在较远空地上（掉落），地上面具无主人。 </summary>
        private void DropMaskAtNearbyEmpty(MaskType maskType)
        {
            if (maskType == MaskType.None) return;
            var pos = FindDropPositionFarEnough();
            var mask = Instantiate(GameCfg.Instance.MaskPrefab, pos, Quaternion.identity).GetComponent<MaskObject>();
            mask.Init(maskType, Vector2.zero, null);
        }
        
        public void GetMask(MaskType mask)
        {
            Debug.Log($"Player {PlayerIdx} got mask {mask}");
            if (maskBag == null) maskBag = new List<MaskType> { MaskType.None };
            // 新面具塞到 0 号位并立刻戴上，其余往后顺移
            maskBag.Insert(0, mask);
            if (maskBag.Count > 1 && maskBag[maskBag.Count - 1] == MaskType.None)
                maskBag.RemoveAt(maskBag.Count - 1);
            while (maskBag.Count > bagCapacity)
            {
                var drop = maskBag[maskBag.Count - 1];
                maskBag.RemoveAt(maskBag.Count - 1);
                DropMaskAtNearbyEmpty(drop);
            }
            currentWornIndex = 0;
            SetCurrentMask(maskBag[0]);
            UpdateUI?.Invoke();
        }

        /// <summary> 切换键：戴上下一个槽位的面具（0→1→2→… 循环）。 </summary>
        public void SwitchMask()
        {
            if (maskBag == null || maskBag.Count == 0) return;
            currentWornIndex = (currentWornIndex + 1) % maskBag.Count;
            SetCurrentMask(maskBag[currentWornIndex]);
            UpdateUI?.Invoke();
        }
        
        public void GetScore(float s)
        {
            curScore += s * scoreMulti;
            UpdateUI?.Invoke();
        }
        
        public void SetInput(Direction dir)
        {
            curDirection = dir;
            var vec = dir.GetVec();
            //transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(vec.y, vec.x) * Mathf.Rad2Deg - 90);
        }

        public void RemoveCurMask()
        {
            if (maskBag == null || maskBag.Count == 0) return;
            int idx = Mathf.Clamp(currentWornIndex, 0, maskBag.Count - 1);
            maskBag.RemoveAt(idx);
            currentWornIndex = 0;
            if (maskBag.Count == 0)
            {
                maskBag.Add(MaskType.None);
                SetCurrentMask(MaskType.None);
            }
            else
                SetCurrentMask(maskBag[0]);
            UpdateUI?.Invoke();
        }

        /// <summary> 发射当前戴的面具。 </summary>
        public void FireMask()
        {
            if (currentMask == MaskType.None) return;
            var mask = Instantiate(GameCfg.Instance.MaskPrefab, transform.position, Quaternion.identity).GetComponent<MaskObject>();
            mask.Init(currentMask, curDirection.GetVec() * GameCfg.Instance.BulletSpeed, this);
            RemoveCurMask();
        }

        public void InitPlayer(int idx)
        {
            PlayerIdx = idx;
            name = $"Player_{PlayerIdx}";
            maskBag = new List<MaskType> { MaskType.None };
            currentWornIndex = 0;
            SetCurrentMask(MaskType.None);
            UpdateUI?.Invoke();
        }

        public bool CanEat(PlayerController other)
        {
            if (other == this) return false;
            return currentMask.GetCfg().CanEat.Contains(other.currentMask);
        }

        /// <summary> 当前面具能否吃该类型金币：老虎只能吃大金币，其它吃小金币。 </summary>
        public bool CanEatCoin(CoinType coinType)
        {
            if (coinType == CoinType.Big)
                return currentMask == MaskType.Tiger;
            return currentMask != MaskType.Tiger;
        }

        //TODO 吃掉后的额外规则..死亡？复活？冷却？得分？
        public void DoEat(PlayerController other)
        {
            GetScore(other.curScore * 0.5f);
            RemoveCurMask();
            other.RemoveCurMask();
        }

        public bool DirHasFood()
        {
            foreach (var player in GameManager.Instance.PlayerList)
            {
                if (CanEat(player) && Vector2.Dot(player.transform.position, curDirection.GetVec()) > 0) return true;
            }
            return false;
        }
        

        #region Unity

        private void OnCollisionEnter2D(Collision2D other)
        {
            var pc = other.gameObject.GetComponentInParent<PlayerController>();
            if (pc != null && CanEat(pc))
            {
                DoEat(pc);
            }
        }

        private void FixedUpdate()
        {
            rig.linearVelocity = FinalSpeed;
        }

        #endregion

        
    }

}
