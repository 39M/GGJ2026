using System.Collections.Generic;
using UnityEngine;

namespace GGJ
{
    /// <summary>
    /// 可插拔 AI：挂到玩家上时模拟输入，控制该玩家。用于新手教学或人数不足时的人机陪玩。
    /// 启用时由本组件驱动；InputManager 会跳过带本组件且启用的玩家。
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class MaskAIController : MonoBehaviour
    {
        [Header("决策间隔")]
        [Tooltip("移动决策间隔(秒)，避免每帧抖动")]
        public float thinkInterval = 0.15f;
        [Tooltip("发射/切换的思考间隔(秒)")]
        public float actionThinkInterval = 0.4f;

        [Header("目标与距离")]
        [Tooltip("视为「附近有危险」的敌人距离(格)")]
        public float fleeRadius = 4f;
        [Tooltip("追击玩家/金币的有效距离(格)")]
        public float chaseRadius = 12f;
        [Tooltip("朝玩家发射面具的最近距离(格)，太近不发射")]
        public float fireMinRange = 2f;
        [Tooltip("朝玩家发射面具的最远距离(格)")]
        public float fireMaxRange = 6f;

        [Header("随机")]
        [Tooltip("无目标时随机换向概率(每 thinkInterval)")]
        [Range(0f, 1f)]
        public float wanderChance = 0.3f;
        [Tooltip("随机切换面具概率(每 actionThinkInterval)")]
        [Range(0f, 1f)]
        public float switchMaskChance = 0.08f;

        [Header("防卡住")]
        [Tooltip("判断「前方有墙」的射线长度(格)，该距离内有墙则换方向")]
        public float wallCheckDistance = 0.6f;
        [Tooltip("若该时间内几乎没移动则视为卡住，强制换向(秒)")]
        public float stuckTimeout = 1.2f;
        [Tooltip("位移小于该值(格)视为没动")]
        public float stuckMoveThreshold = 0.15f;

        private PlayerController _pc;
        private float _nextThinkTime;
        private float _nextActionThinkTime;
        private Direction _wanderDir;
        private Vector2 _lastPosition;
        private float _lastStuckCheckTime;
        private static readonly Direction[] Cardinals = { Direction.Up, Direction.Down, Direction.Left, Direction.Right };

        private void Awake()
        {
            _pc = GetComponent<PlayerController>();
            if (_pc == null) _pc = GetComponentInChildren<PlayerController>();
            _wanderDir = Cardinals[Random.Range(0, Cardinals.Length)];
        }

        private void OnEnable()
        {
            _lastPosition = _pc != null ? (Vector2)_pc.transform.position : Vector2.zero;
            _lastStuckCheckTime = Time.time;
        }

        private void Update()
        {
            if (_pc == null) return;
            if (_pc.IsStunned) return;

            float t = Time.time;
            if (t >= _nextThinkTime)
            {
                _nextThinkTime = t + thinkInterval;
                DecideMove();
            }
            if (t >= _nextActionThinkTime)
            {
                _nextActionThinkTime = t + actionThinkInterval;
                DecideFire();
                DecideSwitchMask();
            }
        }

        private void DecideMove()
        {
            var me = (Vector2)_pc.transform.position;
            bool moved = (me - _lastPosition).magnitude >= stuckMoveThreshold * Utils.GridSize;
            if (moved)
            {
                _lastPosition = me;
                _lastStuckCheckTime = Time.time;
            }

            Direction dir = DecideDirection();
            dir = EnsureDirectionNotBlocked(dir);
            _pc.SetInput(dir);
        }

        private Direction DecideDirection()
        {
            var me = (Vector2)_pc.transform.position;
            float grid = Utils.GridSize;
            float fleeDist = fleeRadius * grid;
            float chaseDist = chaseRadius * grid;

            // 1. 逃跑：有人能吃我且距离近 → 选远离该玩家的方向
            PlayerController predator = null;
            float predatorDistSq = fleeDist * fleeDist;
            foreach (var other in GameManager.Instance.PlayerList)
            {
                if (other == _pc || other.IsStunned) continue;
                if (!other.CanEat(_pc)) continue;
                float dSq = ((Vector2)other.transform.position - me).sqrMagnitude;
                if (dSq < predatorDistSq)
                {
                    predatorDistSq = dSq;
                    predator = other;
                }
            }
            if (predator != null)
            {
                var away = me - (Vector2)predator.transform.position;
                if (away.sqrMagnitude > 0.01f)
                    return EnsureDirectionNotBlocked(VecToDirection(away.normalized));
            }

            // 2. 追击：我能吃某玩家且他在前方 → 朝他移动
            float bestChaseSq = chaseDist * chaseDist;
            PlayerController prey = null;
            foreach (var other in GameManager.Instance.PlayerList)
            {
                if (other == _pc || other.IsStunned) continue;
                if (!_pc.CanEat(other)) continue;
                var toOther = (Vector2)other.transform.position - me;
                float dSq = toOther.sqrMagnitude;
                if (dSq < bestChaseSq && dSq > 0.01f)
                {
                    bestChaseSq = dSq;
                    prey = other;
                }
            }
            if (prey != null)
                return EnsureDirectionNotBlocked(VecToDirection(((Vector2)prey.transform.position - me).normalized));

            // 3. 找最近可吃的金币
            Coin nearestCoin = null;
            float coinDistSq = bestChaseSq;
            var coins = FindObjectsByType<Coin>(FindObjectsSortMode.None);
            foreach (var c in coins)
            {
                if (c == null) continue;
                if (!_pc.CanEatCoin(c.coinType)) continue;
                float dSq = ((Vector2)c.transform.position - me).sqrMagnitude;
                if (dSq < coinDistSq)
                {
                    coinDistSq = dSq;
                    nearestCoin = c;
                }
            }
            if (nearestCoin != null)
                return EnsureDirectionNotBlocked(VecToDirection(((Vector2)nearestCoin.transform.position - me).normalized));

            // 4. 卡住检测：长时间几乎没动则强制换向
            var meNow = (Vector2)_pc.transform.position;
            if (Time.time - _lastStuckCheckTime >= stuckTimeout)
            {
                float moved = (meNow - _lastPosition).magnitude;
                if (moved < stuckMoveThreshold * Utils.GridSize)
                {
                    _wanderDir = Cardinals[Random.Range(0, Cardinals.Length)];
                    _lastPosition = meNow;
                    _lastStuckCheckTime = Time.time;
                }
            }

            // 5. 闲逛
            if (Random.value < wanderChance)
                _wanderDir = Cardinals[Random.Range(0, Cardinals.Length)];
            return EnsureDirectionNotBlocked(_wanderDir);
        }

        /// <summary> 若 dir 方向有墙则从可通行方向中选一个（优先接近原方向），避免顶墙不动。 </summary>
        private Direction EnsureDirectionNotBlocked(Direction dir)
        {
            float checkDist = wallCheckDistance * Utils.GridSize;
            var me = (Vector2)_pc.transform.position;
            var ignore = _pc.transform;

            bool blocked(Direction d) => Utils.HasWallInDirection(me, d, checkDist, ignore);
            if (!blocked(dir)) return dir;

            var preferred = dir.GetVec();
            Direction best = Direction.None;
            float bestDot = -2f;
            foreach (var d in Cardinals)
            {
                if (blocked(d)) continue;
                float dot = Vector2.Dot(preferred, d.GetVec());
                if (dot > bestDot) { bestDot = dot; best = d; }
            }
            if (best != Direction.None) return best;
            foreach (var d in Cardinals)
                if (!blocked(d)) return d;
            return dir;
        }

        private void DecideFire()
        {
            if (_pc.currentMask == MaskType.None) return;
            var me = (Vector2)_pc.transform.position;
            float grid = Utils.GridSize;
            float minSq = fireMinRange * fireMinRange * grid * grid;
            float maxSq = fireMaxRange * fireMaxRange * grid * grid;
            var dirVec = _pc.curDirection.GetVec();

            foreach (var other in GameManager.Instance.PlayerList)
            {
                if (other == _pc || other.IsStunned) continue;
                var toOther = (Vector2)other.transform.position - me;
                float dSq = toOther.sqrMagnitude;
                if (dSq < minSq || dSq > maxSq) continue;
                if (Vector2.Dot(toOther.normalized, dirVec) < 0.7f) continue; // 大致朝向我方
                _pc.FireMask();
                return;
            }
        }

        private void DecideSwitchMask()
        {
            if (_pc.maskBag == null || _pc.maskBag.Count <= 1) return;
            if (Random.value < switchMaskChance)
                _pc.SwitchMask();
        }

        private static Direction VecToDirection(Vector2 v)
        {
            if (v.sqrMagnitude < 0.01f) return Direction.None;
            float dx = v.x;
            float dy = v.y;
            if (Mathf.Abs(dx) >= Mathf.Abs(dy))
                return dx > 0 ? Direction.Right : Direction.Left;
            return dy > 0 ? Direction.Up : Direction.Down;
        }
    }
}
