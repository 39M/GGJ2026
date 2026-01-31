using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GGJ
{
    public class InputManager : MonoSingleton<InputManager>
    {
        public InputSystem_Actions InputSystemActions;

        // 游戏手柄分配
        private Dictionary<Gamepad, int> gamepadToPlayer = new Dictionary<Gamepad, int>();
        private Gamepad player1Gamepad;
        private Gamepad player2Gamepad;
        private Gamepad player3Gamepad;
        private Gamepad player4Gamepad;

        private void Start()
        {
            InitInput();
        }

        void InitInput()
        {
            InputSystemActions = new InputSystem_Actions();
            InputSystemActions.Enable();

            // 检测连接的游戏手柄并分配给玩家
            DetectAndAssignGamepads();

            // 绑定输入事件
            BindInputEvents();
        }

        // 检测并分配游戏手柄给玩家
        private void DetectAndAssignGamepads()
        {
            var gamepads = Gamepad.all;
            Debug.Log($"[InputManager] 检测到 {gamepads.Count} 个游戏手柄");

            // 为Player1和Player2分配游戏手柄
            if (gamepads.Count > 0)
            {
                player1Gamepad = gamepads[0];
                gamepadToPlayer[player1Gamepad] = 0;
                Debug.Log($"[InputManager] 游戏手柄1 '{player1Gamepad.displayName}' (ID: {player1Gamepad.deviceId}) 分配给 Player1");
            }

            if (gamepads.Count > 1)
            {
                player2Gamepad = gamepads[1];
                gamepadToPlayer[player2Gamepad] = 1;
                Debug.Log($"[InputManager] 游戏手柄2 '{player2Gamepad.displayName}' (ID: {player2Gamepad.deviceId}) 分配给 Player2");
            }

            if (gamepads.Count > 2)
            {
                player3Gamepad = gamepads[2];
                gamepadToPlayer[player3Gamepad] = 2;
                Debug.Log($"[InputManager] 游戏手柄2 '{player3Gamepad.displayName}' (ID: {player3Gamepad.deviceId}) 分配给 Player3");
            }

            if (gamepads.Count > 3)
            {
                player4Gamepad = gamepads[3];
                gamepadToPlayer[player4Gamepad] = 3;
                Debug.Log($"[InputManager] 游戏手柄2 '{player4Gamepad.displayName}' (ID: {player4Gamepad.deviceId}) 分配给 Player4");
            }
        }

        // 绑定输入事件
        private void BindInputEvents()
        {
            // Player1 输入绑定（键盘 + 游戏手柄1）
            InputSystemActions.Player_1.Up.started += ctx => OnMoveInput(ctx, Direction.Up, 0);
            InputSystemActions.Player_1.Down.started += ctx => OnMoveInput(ctx, Direction.Down, 0);
            InputSystemActions.Player_1.Left.started += ctx => OnMoveInput(ctx, Direction.Left, 0);
            InputSystemActions.Player_1.Right.started += ctx => OnMoveInput(ctx, Direction.Right, 0);
            InputSystemActions.Player_1.Attack.started += ctx => OnAttackInput(ctx, 0);
            InputSystemActions.Player_1.Switch.started += ctx => OnSwitchInput(ctx, 0);

            // Player2 输入绑定（键盘 + 游戏手柄2）
            InputSystemActions.Player_2.Up.started += ctx => OnMoveInput(ctx, Direction.Up, 1);
            InputSystemActions.Player_2.Down.started += ctx => OnMoveInput(ctx, Direction.Down, 1);
            InputSystemActions.Player_2.Left.started += ctx => OnMoveInput(ctx, Direction.Left, 1);
            InputSystemActions.Player_2.Right.started += ctx => OnMoveInput(ctx, Direction.Right, 1);
            InputSystemActions.Player_2.Attack.started += ctx => OnAttackInput(ctx, 1);
            InputSystemActions.Player_2.Switch.started += ctx => OnSwitchInput(ctx, 1);

            InputSystemActions.Player_3.Up.started += ctx => OnMoveInput(ctx, Direction.Up, 1);
            InputSystemActions.Player_3.Down.started += ctx => OnMoveInput(ctx, Direction.Down, 1);
            InputSystemActions.Player_3.Left.started += ctx => OnMoveInput(ctx, Direction.Left, 1);
            InputSystemActions.Player_3.Right.started += ctx => OnMoveInput(ctx, Direction.Right, 1);
            InputSystemActions.Player_3.Attack.started += ctx => OnAttackInput(ctx, 1);
            InputSystemActions.Player_3.Switch.started += ctx => OnSwitchInput(ctx, 1);

            InputSystemActions.Player_4.Up.started += ctx => OnMoveInput(ctx, Direction.Up, 1);
            InputSystemActions.Player_4.Down.started += ctx => OnMoveInput(ctx, Direction.Down, 1);
            InputSystemActions.Player_4.Left.started += ctx => OnMoveInput(ctx, Direction.Left, 1);
            InputSystemActions.Player_4.Right.started += ctx => OnMoveInput(ctx, Direction.Right, 1);
            InputSystemActions.Player_4.Attack.started += ctx => OnAttackInput(ctx, 1);
            InputSystemActions.Player_4.Switch.started += ctx => OnSwitchInput(ctx, 1);
        }

        // 移动输入处理
        private void OnMoveInput(InputAction.CallbackContext ctx, Direction direction, int playerIndex)
        {
            // 检查是否来自正确的设备
            if (IsValidInputForPlayer(ctx, playerIndex))
            {
                OnMovePerform(direction, playerIndex);
            }
        }

        // 丢面具处理
        private void OnAttackInput(InputAction.CallbackContext ctx, int playerIndex)
        {
            if (IsValidInputForPlayer(ctx, playerIndex))
            {
                OnAttackStart(ctx, playerIndex);
            }
        }

        // 切换面具处理
        private void OnSwitchInput(InputAction.CallbackContext ctx, int playerIndex)
        {
            if (IsValidInputForPlayer(ctx, playerIndex))
            {
                OnSwitchMask(playerIndex);
            }
        }

        // 验证输入是否来自正确的设备
        private bool IsValidInputForPlayer(InputAction.CallbackContext ctx, int playerIndex)
        {
            var device = ctx.control.device;

            // 键盘输入总是有效的
            if (device is Keyboard)
                return true;

            // 检查游戏手柄输入
            if (device is Gamepad gamepad)
            {
                if (playerIndex == 0 && gamepad == player1Gamepad)
                    return true;
                if (playerIndex == 1 && gamepad == player2Gamepad)
                    return true;
                if (playerIndex == 2 && gamepad == player3Gamepad)
                    return true;
                if (playerIndex == 3 && gamepad == player4Gamepad)
                    return true;
            }

            return false;
        }

        // /// 获取连接的游戏手柄信息
        // public void LogConnectedGamepads()
        // {
        //     Debug.Log("=== 当前连接的游戏手柄 ===");
        //     if (player1Gamepad != null)
        //         Debug.Log($"Player1: {player1Gamepad.displayName} (ID: {player1Gamepad.deviceId})");
        //     else
        //         Debug.Log("Player1: 无游戏手柄连接");
        //         
        //     if (player2Gamepad != null)
        //         Debug.Log($"Player2: {player2Gamepad.displayName} (ID: {player2Gamepad.deviceId})");
        //     else
        //         Debug.Log("Player2: 无游戏手柄连接");
        // }

        // 获取指定玩家的游戏手柄
        public Gamepad GetPlayerGamepad(int playerIndex)
        {
            return playerIndex switch
            {
                0 => player1Gamepad,
                1 => player2Gamepad,
                2 => player3Gamepad,
                3 => player4Gamepad,
                _ => null
            };
        }

        // 获取游戏手柄对应的玩家索引
        // public int GetGamepadPlayerIndex(Gamepad gamepad)
        // {
        //     return gamepadToPlayer.TryGetValue(gamepad, out int playerIndex) ? playerIndex : -1;
        // }

        private void OnDestroy()
        {
            InputSystemActions.Disable();
        }

        private void OnAttackStart(InputAction.CallbackContext ctx, int playerIdx)
        {
            GameManager.Instance.GetPlayer(playerIdx).FireMask();

            // 攻击震动 - 强度较高，时长较长
            var gamepad = GetPlayerGamepad(playerIdx);
            gamepad?.SetMotorSpeeds(0.1f, 0.3f);
            StartCoroutine(StopVibrationAfter(gamepad, 0.1f));
        }

        private void OnSwitchMask(int playerIdx)
        {
            GameManager.Instance.GetPlayer(playerIdx).SwitchMask();

            // 切换震动 - 强度较轻，时长较短
            var gamepad = GetPlayerGamepad(playerIdx);
            gamepad?.SetMotorSpeeds(0.1f, 0.1f);
            StartCoroutine(StopVibrationAfter(gamepad, 0.1f));
        }

        // 在指定时间后停止震动
        private System.Collections.IEnumerator StopVibrationAfter(Gamepad gamepad, float delay)
        {
            yield return new WaitForSeconds(delay);
            gamepad?.SetMotorSpeeds(0f, 0f);
        }

        private void OnMovePerform(Direction dir, int playerIdx)
        {
            GameManager.Instance.GetPlayer(playerIdx).SetInput(dir);
        }
    }
}