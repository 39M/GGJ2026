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

        public static Direction Reverse(this Direction dir)
        {
            return dir switch
            {
                Direction.Up => Direction.Down,
                Direction.Down => Direction.Up,
                Direction.Left => Direction.Right,
                Direction.Right => Direction.Left,
                _ => dir
            };
        }
        
        public static MaskCfg GetCfg(this MaskType mask)
        {
            return GameCfg.Instance.GetMaskCfg(mask);
        }

        /// <summary> 在给定中心附近找一个空地：该点无碰撞体，且该点周围半径内无墙，避免掉落物/面具与墙重叠。 </summary>
        /// <param name="center">中心点</param>
        /// <param name="wallCheckRadius">候选点周围检测墙的半径，需略大于掉落物碰撞体（如面具约 0.5）</param>
        public static Vector2 FindNearbyEmptyPosition(Vector2 center, float wallCheckRadius = 0.5f)
        {
            var grid = GridSize;
            var offsets = new[]
            {
                new Vector2(grid, 0), new Vector2(-grid, 0), new Vector2(0, grid), new Vector2(0, -grid),
                new Vector2(grid, grid), new Vector2(-grid, grid), new Vector2(grid, -grid), new Vector2(-grid, -grid),
                new Vector2(2 * grid, 0), new Vector2(-2 * grid, 0), new Vector2(0, 2 * grid), new Vector2(0, -2 * grid),
            };
            foreach (var off in offsets)
            {
                var p = center + off;
                if (Physics2D.OverlapPoint(p)) continue;
                if (HasWallWithinRadius(p, wallCheckRadius)) continue;
                return p;
            }
            return center + new Vector2(grid, 0);
        }

        /// <summary> 该点半径内是否存在墙（Tag=Wall）。 </summary>
        public static bool HasWallWithinRadius(Vector2 point, float radius)
        {
            var hits = Physics2D.OverlapCircleAll(point, radius);
            foreach (var col in hits)
                if (col.CompareTag("Wall")) return true;
            return false;
        }

        /// <summary> 从 from 沿方向 dir 射线检测，maxDistance 内是否碰到墙。ignore 为 null 则不忽略任何碰撞体。 </summary>
        public static bool HasWallInDirection(Vector2 from, Direction dir, float maxDistance, Transform ignore = null)
        {
            var vec = dir.GetVec();
            if (vec.sqrMagnitude < 0.01f) return true;
            var hits = Physics2D.RaycastAll(from, vec, maxDistance);
            foreach (var hit in hits)
            {
                if (!hit.collider) continue;
                if (ignore != null && hit.collider.transform.IsChildOf(ignore)) continue;
                if (hit.collider.CompareTag("Wall")) return true;
            }
            return false;
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
        
        /// <summary> 是否被标记为垫底 </summary>
        public bool IsMarked { get; private set; } = false;
        
        /// <summary> 是否已被淘汰 </summary>
        public bool IsEliminated { get; private set; } = false;

        private float _stunEndTime;
        private float _dropCoinNextTime;
        private float _lastFireMaskTime = -999f;

        public bool IsStunned => Time.time < _stunEndTime;
        public Vector2 FinalSpeed => IsStunned ? Vector2.zero : (DirHasFood() ? eatSpeed : speed) * curDirection.GetVec();

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
            if (cfg.DropCoinInterval > 0f)
                _dropCoinNextTime = Time.time + cfg.DropCoinInterval;
            UpdateUI?.Invoke();
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
                if (Physics2D.OverlapPoint(p)) continue;
                if (Utils.HasWallWithinRadius(p, 0.6f)) continue;
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
        /// <summary> 鸟形态时若与墙重叠则不允许切换，避免切回地面层从墙里卡出。 </summary>
        private bool IsOverlappingWall(float radius = 0.4f)
        {
            var hits = Physics2D.OverlapCircleAll((Vector2)transform.position, radius);
            foreach (var col in hits)
                if (col.CompareTag("Wall")) return true;
            return false;
        }

        public void SwitchMask()
        {
            if (maskBag == null || maskBag.Count == 0) return;
            if (currentMask != MaskType.None && currentMask.GetCfg().CanFly && IsOverlappingWall())
                return;
            currentWornIndex = (currentWornIndex + 1) % maskBag.Count;
            SetCurrentMask(maskBag[currentWornIndex]);
            UpdateUI?.Invoke();
        }
        
        public void GetScore(float s)
        {
            curScore += s * scoreMulti;
            UpdateUI?.Invoke();
        }

        public void AddScore(float s)
        {
            curScore += s;
            UpdateUI?.Invoke();
        }

        public void LoseScore(float s)
        {
            curScore = Mathf.Max(0f, curScore - s);
            UpdateUI?.Invoke();
        }

        private void StartStun(float duration)
        {
            _stunEndTime = Time.time + duration;
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

        /// <summary> 发射时面具生成位置相对玩家的偏移(格)，沿发射方向，避免刚生成就与玩家重叠触发小碰撞。 </summary>
        public float fireSpawnOffset = 0.6f;

        /// <summary> 发射当前戴的面具。 </summary>
        public void FireMask()
        {
            if (currentMask == MaskType.None) return;
            float cd = GameCfg.Instance.FireMaskCooldown;
            if (cd > 0f && Time.time - _lastFireMaskTime < cd) return;
            var dir = curDirection.GetVec();
            var spawnPos = (Vector2)transform.position + dir * (Utils.GridSize * fireSpawnOffset);
            var mask = Instantiate(GameCfg.Instance.MaskPrefab, spawnPos, Quaternion.identity).GetComponent<MaskObject>();
            mask.owner = this;
            mask.Init(currentMask, dir * GameCfg.Instance.BulletSpeed, this);
            RemoveCurMask();
            _lastFireMaskTime = Time.time;
        }

        public void InitPlayer(int idx)
        {
            PlayerIdx = idx;
            name = $"Player_{PlayerIdx}";
            maskBag = new List<MaskType> { MaskType.None };
            currentWornIndex = 0;
            IsMarked = false;
            IsEliminated = false;
            SetCurrentMask(MaskType.None);
            UpdateUI?.Invoke();
        }
        
        /// <summary>
        /// 设置标记状态（垫底警告）
        /// </summary>
        public void SetMarked(bool marked)
        {
            IsMarked = marked;
            UpdateUI?.Invoke();
        }
        
        /// <summary>
        /// 设置淘汰状态
        /// </summary>
        public void SetEliminated(bool eliminated)
        {
            IsEliminated = eliminated;
            if (eliminated)
            {
                // 禁用碰撞
                var colliders = GetComponentsInChildren<Collider2D>();
                foreach (var col in colliders)
                {
                    col.enabled = false;
                }
            }
            UpdateUI?.Invoke();
        }

        public bool CanEat(PlayerController other)
        {
            if (other == this) return false;
            return currentMask.GetCfg().CanEat.Contains(other.currentMask);
        }

        /// <summary> 当前面具能否吃该类型金币：由 MaskCfg.OnlyEatBigCoin 配置，勾选则只吃大金币，否则只吃小金币。 </summary>
        public bool CanEatCoin(CoinType coinType)
        {
            bool eatsBigOnly = currentMask.GetCfg().OnlyEatBigCoin;
            if (coinType == CoinType.Big)
                return eatsBigOnly;
            return !eatsBigOnly;
        }

        [Tooltip("被吃后逃离方向检测墙的射线长度(格)，该距离内有墙则该方向不选。")]
        public float eatFleeWallCheckDistance = 2f;

        /// <summary> 从 fromPosition 沿方向 dir 射线检测，短距离内是否碰到墙（忽略玩家）。 </summary>
        private static bool HasWallInDirection(Vector2 fromPosition, Direction dir, float maxDistance, PlayerController ignoreA, PlayerController ignoreB)
        {
            var vec = dir.GetVec();
            if (vec.sqrMagnitude < 0.01f) return true;
            var hits = Physics2D.RaycastAll(fromPosition, vec, maxDistance);
            foreach (var hit in hits)
            {
                if (!hit.collider) continue;
                if (ignoreA != null && hit.collider.transform.IsChildOf(ignoreA.transform)) continue;
                if (ignoreB != null && hit.collider.transform.IsChildOf(ignoreB.transform)) continue;
                if (hit.collider.CompareTag("Wall")) return true;
            }
            return false;
        }

        /// <summary> 为被吃者选一个逃离吃人者的方向：优先与“远离吃人者”一致，且短距离内无墙。 </summary>
        private Direction ChooseFleeDirection(PlayerController eaten)
        {
            var away = (Vector2)(eaten.transform.position - transform.position);
            if (away.sqrMagnitude < 0.01f) away = Vector2.up;
            away.Normalize();
            float checkDist = eatFleeWallCheckDistance * Utils.GridSize;
            var cardinals = new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right };
            Direction best = Direction.Up;
            float bestDot = -2f;
            foreach (var d in cardinals)
            {
                if (HasWallInDirection((Vector2)eaten.transform.position, d, checkDist, this, eaten))
                    continue;
                float dot = Vector2.Dot(away, d.GetVec());
                if (dot > bestDot) { bestDot = dot; best = d; }
            }
            if (bestDot <= -2f)
            {
                foreach (var d in cardinals)
                {
                    float dot = Vector2.Dot(away, d.GetVec());
                    if (dot > bestDot) { bestDot = dot; best = d; }
                }
            }
            return best;
        }

        /// <summary> 吃人者 A 调用：A 眩晕、偷 B 的分数，B 扣分，B 重新选择逃离方向（短距离无墙）并更新朝向。不摘面具、不位移推开。 </summary>
        public void DoEat(PlayerController other)
        {
            var cfg = GameCfg.Instance;
            float stealAmount = other.curScore * cfg.EatStealRatio;
            stealAmount = Mathf.Min(stealAmount, other.curScore);

            AddScore(stealAmount);
            other.LoseScore(stealAmount);
            StartStun(cfg.EatStunDuration);

            other.curDirection = ChooseFleeDirection(other);
            var vec = other.curDirection.GetVec();
            other.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(vec.y, vec.x) * Mathf.Rad2Deg - 90);
            other.UpdateUI?.Invoke();
        }

        public bool DirHasFood()
        {
            foreach (var player in GameManager.Instance.PlayerList)
            {
                if (CanEat(player) && Vector2.Dot(player.transform.position, curDirection.GetVec()) > 0) return true;
            }
            return false;
        }
        

        /// <summary> 戴有“掉落金币”面具时，每隔间隔扣除分数并在脚下附近生成可拾取金币。 </summary>
        private void TryDropCoinByMask()
        {
            if (currentMask == MaskType.None) return;
            var cfg = currentMask.GetCfg();
            if (cfg.DropCoinInterval <= 0f || cfg.DropCoinAmount <= 0f) return;
            if (Time.time < _dropCoinNextTime) return;
            float amount = Mathf.Min(cfg.DropCoinAmount, curScore);
            if (amount <= 0f) return;
            var pos = Utils.FindNearbyEmptyPosition((Vector2)transform.position);
            var prefab = GameCfg.Instance.CoinPrefab;
            if (prefab == null) return;
            var coinObj = Instantiate(prefab.gameObject, pos, Quaternion.identity);
            var coin = coinObj.GetComponent<Coin>();
            if (coin != null)
            {
                coin.score = amount;
                coin.coinType = CoinType.Small;
            }
            LoseScore(amount);
            _dropCoinNextTime = Time.time + cfg.DropCoinInterval;
        }

        #region Unity

        private void Update()
        {
            TryDropCoinByMask();
        }

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
