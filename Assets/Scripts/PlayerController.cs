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
        
        /// <summary> 面具列表：索引 0 = 当前戴的，1.. = 背包（越靠后越古早）。当前戴的也在本列表中。 </summary>
        [LabelText("面具列表(0=当前 1..=背包)")]
        public List<MaskType> maskBag = new List<MaskType> { MaskType.None };
        [LabelText("背包容量(包含当前带的)")]
        public int bagCapacity = 3;

        /// <summary> 当前戴在脸上的面具，即 maskBag[0]。 </summary>
        public MaskType currentMask => (maskBag != null && maskBag.Count > 0) ? maskBag[0] : MaskType.None;
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
        
        public void SetCurrentMask(MaskType mask)
        {
            if (maskBag == null)
            {
                maskBag = new List<MaskType>();
            }

            if (maskBag.Count == 0)
            {
                maskBag.Add(mask);
            }
            else
            {
                maskBag[0] = mask;
            }
            
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
            // 按距离从近到远尝试：相邻格、对角、再远一圈
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

        /// <summary> 将指定类型的面具生成在附近空地上（掉落）。 </summary>
        private void DropMaskAtNearbyEmpty(MaskType maskType)
        {
            if (maskType == MaskType.None) return;
            var pos = FindNearbyEmptyPosition();
            var mask = Instantiate(GameCfg.Instance.MaskPrefab, pos, Quaternion.identity).GetComponent<MaskObject>();
            mask.Init(maskType, Vector2.zero, null);
        }
        
        public void GetMask(MaskType mask)
        {
            Debug.Log($"Player {PlayerIdx} got mask {mask}");
            if (maskBag == null) maskBag = new List<MaskType> { MaskType.None };
            
            // 新面具插到最前（成为当前），原当前自然退到 index 1
            maskBag.Insert(0, mask);
            
            // 若原先没有面具，列表可能是 [None]，插入后为 [mask, None]，去掉末尾的 None
            if (maskBag.Count > 1 && maskBag[maskBag.Count - 1] == MaskType.None)
            {
                maskBag.RemoveAt(maskBag.Count - 1);
            }
            
            // 背包满时丢弃最古早的（列表最后一个）
            while (maskBag.Count > bagCapacity)
            {
                var drop = maskBag[maskBag.Count - 1];
                maskBag.RemoveAt(maskBag.Count - 1);
                DropMaskAtNearbyEmpty(drop);
            }
            SetCurrentMask(maskBag[0]);
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
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(vec.y, vec.x) * Mathf.Rad2Deg - 90);
        }

        public void RemoveCurMask()
        {
            if (maskBag == null || maskBag.Count <= 1)
            {
                if (maskBag != null)
                {
                    maskBag.Clear();
                }
                maskBag = new List<MaskType> { MaskType.None };
                SetCurrentMask(MaskType.None);
            }
            else
            {
                maskBag.RemoveAt(0);
                SetCurrentMask(maskBag[0]);
            }
            UpdateUI?.Invoke();
        }
        
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
            SetCurrentMask(MaskType.None);
            UpdateUI?.Invoke();
        }

        public bool CanEat(PlayerController other)
        {
            if (other == this) return false;
            return currentMask.GetCfg().CanEat.Contains(other.currentMask);
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
