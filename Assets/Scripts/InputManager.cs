using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GGJ
{
    public class InputManager : MonoSingleton<InputManager>
    {
        public InputSystem_Actions InputSystemActions;
        public InputSystem_Controller_2 Controller2Actions;
        public InputSystem_Controller_3 Controller3Actions;
        public InputSystem_Controller_4 Controller4Actions;

        // 游戏手柄分配
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
            Controller2Actions = new InputSystem_Controller_2();
            Controller3Actions = new InputSystem_Controller_3();
            Controller4Actions = new InputSystem_Controller_4();

            // 检测游戏手柄并配置设备绑定
            ConfigureDeviceBindings();

            // 绑定输入事件
            BindInputEvents();
        }
        
        /// <summary>
        /// 检测游戏手柄并配置每个玩家的设备绑定
        /// </summary>
        private void ConfigureDeviceBindings()
        {
            var gamepads = Gamepad.all;
            Debug.Log($"[InputManager] 检测到 {gamepads.Count} 个游戏手柄");

            // 分配游戏手柄
            player1Gamepad = gamepads.Count > 0 ? gamepads[0] : null;
            player2Gamepad = gamepads.Count > 1 ? gamepads[1] : null;
            player3Gamepad = gamepads.Count > 2 ? gamepads[2] : null;
            player4Gamepad = gamepads.Count > 3 ? gamepads[3] : null;

            // Player1：键盘 + 第一个游戏手柄
            if (player1Gamepad != null)
            {
                InputSystemActions.devices = new UnityEngine.InputSystem.InputDevice[] { Keyboard.current, player1Gamepad };
                Debug.Log($"[InputManager] 游戏手柄1 '{player1Gamepad.displayName}' (ID: {player1Gamepad.deviceId}) 分配给 Player1");
            }
            else
            {
                InputSystemActions.devices = new UnityEngine.InputSystem.InputDevice[] { Keyboard.current };
            }
            InputSystemActions.Enable();

            // Player2：键盘 + 第二个游戏手柄
            if (player2Gamepad != null)
            {
                Controller2Actions.devices = new UnityEngine.InputSystem.InputDevice[] { Keyboard.current, player2Gamepad };
                Debug.Log($"[InputManager] 游戏手柄2 '{player2Gamepad.displayName}' (ID: {player2Gamepad.deviceId}) 分配给 Player2");
            }
            else
            {
                Controller2Actions.devices = new UnityEngine.InputSystem.InputDevice[] { Keyboard.current };
            }
            Controller2Actions.Enable();

            // Player3：第三个游戏手柄
            if (player3Gamepad != null)
            {
                Controller3Actions.devices = new UnityEngine.InputSystem.InputDevice[] { player3Gamepad };
                Controller3Actions.Enable();
                Debug.Log($"[InputManager] 游戏手柄3 '{player3Gamepad.displayName}' (ID: {player3Gamepad.deviceId}) 分配给 Player3");
            }

            // Player4：第四个游戏手柄
            if (player4Gamepad != null)
            {
                Controller4Actions.devices = new UnityEngine.InputSystem.InputDevice[] { player4Gamepad };
                Controller4Actions.Enable();
                Debug.Log($"[InputManager] 游戏手柄4 '{player4Gamepad.displayName}' (ID: {player4Gamepad.deviceId}) 分配给 Player4");
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
            InputSystemActions.Player_1.Move2D.performed += ctx => OnMove2DInput(ctx, 0);

            // Player2 输入绑定（键盘 + 游戏手柄2）
            Controller2Actions.Player.Up.started += ctx => OnMoveInput(ctx, Direction.Up, 1);
            Controller2Actions.Player.Down.started += ctx => OnMoveInput(ctx, Direction.Down, 1);
            Controller2Actions.Player.Left.started += ctx => OnMoveInput(ctx, Direction.Left, 1);
            Controller2Actions.Player.Right.started += ctx => OnMoveInput(ctx, Direction.Right, 1);
            Controller2Actions.Player.Attack.started += ctx => OnAttackInput(ctx, 1);
            Controller2Actions.Player.Switch.started += ctx => OnSwitchInput(ctx, 1);
            Controller2Actions.Player.Move2D.performed += ctx => OnMove2DInput(ctx, 1);

            // Player3 输入绑定（游戏手柄3）
            if (player3Gamepad != null)
            {
                Controller3Actions.Player.Up.started += ctx => OnMoveInput(ctx, Direction.Up, 2);
                Controller3Actions.Player.Down.started += ctx => OnMoveInput(ctx, Direction.Down, 2);
                Controller3Actions.Player.Left.started += ctx => OnMoveInput(ctx, Direction.Left, 2);
                Controller3Actions.Player.Right.started += ctx => OnMoveInput(ctx, Direction.Right, 2);
                Controller3Actions.Player.Attack.started += ctx => OnAttackInput(ctx, 2);
                Controller3Actions.Player.Switch.started += ctx => OnSwitchInput(ctx, 2);
                Controller3Actions.Player.Move2D.performed += ctx => OnMove2DInput(ctx, 2);
            }

            // Player4 输入绑定（游戏手柄4）
            if (player4Gamepad != null)
            {
                Controller4Actions.Player.Up.started += ctx => OnMoveInput(ctx, Direction.Up, 3);
                Controller4Actions.Player.Down.started += ctx => OnMoveInput(ctx, Direction.Down, 3);
                Controller4Actions.Player.Left.started += ctx => OnMoveInput(ctx, Direction.Left, 3);
                Controller4Actions.Player.Right.started += ctx => OnMoveInput(ctx, Direction.Right, 3);
                Controller4Actions.Player.Attack.started += ctx => OnAttackInput(ctx, 3);
                Controller4Actions.Player.Switch.started += ctx => OnSwitchInput(ctx, 3);
                Controller4Actions.Player.Move2D.performed += ctx => OnMove2DInput(ctx, 3);
            }
        }

        // 移动输入处理
        private void OnMoveInput(InputAction.CallbackContext ctx, Direction direction, int playerIndex)
        {
            OnMovePerform(direction, playerIndex);
        }

        // 丢面具处理
        private void OnAttackInput(InputAction.CallbackContext ctx, int playerIndex)
        {
            OnAttackStart(ctx, playerIndex);
        }

        // 切换面具处理
        private void OnSwitchInput(InputAction.CallbackContext ctx, int playerIndex)
        {
            OnSwitchMask(playerIndex);
        }

        // 2D移动输入处理
        private void OnMove2DInput(InputAction.CallbackContext ctx, int playerIndex)
        {
            Vector2 moveVector = ctx.ReadValue<Vector2>();
            
            // 设置死区阈值，避免摇杆微小抖动
            const float deadZone = 0.2f;
            if (moveVector.magnitude < deadZone)
                return;
            
            // 根据2D轴值确定主要移动方向
            if (Mathf.Abs(moveVector.x) > Mathf.Abs(moveVector.y))
            {
                // 水平移动优先
                if (moveVector.x > 0)
                    OnMovePerform(Direction.Right, playerIndex);
                else if (moveVector.x < 0)
                    OnMovePerform(Direction.Left, playerIndex);
            }
            else if (Mathf.Abs(moveVector.y) > 0)
            {
                // 垂直移动优先
                if (moveVector.y > 0)
                    OnMovePerform(Direction.Up, playerIndex);
                else if (moveVector.y < 0)
                    OnMovePerform(Direction.Down, playerIndex);
            }
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
            InputSystemActions?.Disable();
            Controller2Actions?.Disable();
            Controller3Actions?.Disable();
            Controller4Actions?.Disable();
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