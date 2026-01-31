using System;
using UnityEngine;
using UnityEngine.InputSystem;


namespace GGJ
{
    public class InputManager : MonoSingleton<InputManager>
    {
        public InputSystem_Actions InputSystemActions;

        private void Start()
        {
            InitInput();
        }

        //TODO 其他输入or更多玩家？
        void InitInput()
        {
            InputSystemActions = new InputSystem_Actions();
            InputSystemActions.Enable();
            
            InputSystemActions.Player_1.Up.started += ctx => OnMovePerform(Direction.Up, 0);
            InputSystemActions.Player_1.Down.started += ctx => OnMovePerform(Direction.Down, 0);
            InputSystemActions.Player_1.Left.started += ctx => OnMovePerform(Direction.Left, 0);
            InputSystemActions.Player_1.Right.started += ctx => OnMovePerform(Direction.Right, 0);
            InputSystemActions.Player_1.Attack.started += ctx => OnAttackStart(ctx, 0);
            
            InputSystemActions.Player_2.Up.started += ctx => OnMovePerform(Direction.Up, 1);
            InputSystemActions.Player_2.Down.started += ctx => OnMovePerform(Direction.Down, 1);
            InputSystemActions.Player_2.Left.started += ctx => OnMovePerform(Direction.Left, 1);
            InputSystemActions.Player_2.Right.started += ctx => OnMovePerform(Direction.Right, 1);
            InputSystemActions.Player_2.Attack.started += ctx => OnAttackStart(ctx, 1);
        }

        private void OnDestroy()
        {
            InputSystemActions.Disable();
        }

        private void OnAttackStart(InputAction.CallbackContext ctx, int playerIdx)
        {
            GameManager.Instance.GetPlayer(playerIdx).FireMask();
        }

        private void OnMovePerform(Direction dir, int playerIdx)
        {
            GameManager.Instance.GetPlayer(playerIdx).SetInput(dir);
        }
    }
}
