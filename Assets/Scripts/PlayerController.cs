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
        
        [LabelText("当前面具")]
        public MaskType currentMask = MaskType.None;
        [LabelText("背包面具")] 
        public MaskType bagMask = MaskType.None;
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
            currentMask = mask;
            var cfg = mask.GetCfg();
            speed = cfg.Speed;
            eatSpeed = cfg.EatSpeed;
            scoreMulti = cfg.Score;
            gameObject.SetAllLayer(cfg.Layer);
            mainSprite.color = cfg.TestColor;
            UpdateUI?.Invoke();
        }
        
        public void GetMask(MaskType mask)
        {
            Debug.Log($"Player {PlayerIdx} got mask {mask}");
            bagMask = currentMask;
            SetCurrentMask(mask);
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
            SetCurrentMask(bagMask);
            bagMask = MaskType.None;
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
