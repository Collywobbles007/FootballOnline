namespace Fusion.Collywobbles.Futsal
{
    using UnityEngine;
    using Fusion;


    public enum EGameplayInputAction
    {
        LMB = 0,
        RMB = 1,
        MMB = 2,
        Space = 3,
        Dash = 4,
        Sprint = 5,
        LeftTrigger = 6,
        RightTrigger = 7,
        TAB = 8,
        Enter = 9,
        F1 = 10,
        F2 = 11,
        F3 = 12,
        F4 = 13,
        F5 = 14,
        F6 = 15,
        F7 = 16,
        F8 = 17,
        F9 = 18,
        F10 = 19,
    }

    public struct GameplayInput : INetworkInput
    {
        public Vector2 MoveDirection;
        public Vector2 LookRotationDelta;
        public NetworkButtons Actions;

        public bool LMB { get { return Actions.IsSet(EGameplayInputAction.LMB); } set { Actions.Set(EGameplayInputAction.LMB, value); } }
        public bool RMB { get { return Actions.IsSet(EGameplayInputAction.RMB); } set { Actions.Set(EGameplayInputAction.RMB, value); } }
        public bool MMB { get { return Actions.IsSet(EGameplayInputAction.MMB); } set { Actions.Set(EGameplayInputAction.MMB, value); } }
        public bool Space { get { return Actions.IsSet(EGameplayInputAction.Space); } set { Actions.Set(EGameplayInputAction.Space, value); } }
        public bool Sprint { get { return Actions.IsSet(EGameplayInputAction.Sprint); } set { Actions.Set(EGameplayInputAction.Sprint, value); } }
        public bool TAB { get { return Actions.IsSet(EGameplayInputAction.TAB); } set { Actions.Set(EGameplayInputAction.TAB, value); } }
        public bool Enter { get { return Actions.IsSet(EGameplayInputAction.Enter); } set { Actions.Set(EGameplayInputAction.Enter, value); } }
        public bool F1 { get { return Actions.IsSet(EGameplayInputAction.F1); } set { Actions.Set(EGameplayInputAction.F1, value); } }
        public bool F2 { get { return Actions.IsSet(EGameplayInputAction.F2); } set { Actions.Set(EGameplayInputAction.F2, value); } }
        public bool F3 { get { return Actions.IsSet(EGameplayInputAction.F3); } set { Actions.Set(EGameplayInputAction.F3, value); } }
        public bool F4 { get { return Actions.IsSet(EGameplayInputAction.F4); } set { Actions.Set(EGameplayInputAction.F4, value); } }
        public bool F5 { get { return Actions.IsSet(EGameplayInputAction.F5); } set { Actions.Set(EGameplayInputAction.F5, value); } }
        public bool F6 { get { return Actions.IsSet(EGameplayInputAction.F6); } set { Actions.Set(EGameplayInputAction.F6, value); } }
        public bool F7 { get { return Actions.IsSet(EGameplayInputAction.F7); } set { Actions.Set(EGameplayInputAction.F7, value); } }
        public bool F8 { get { return Actions.IsSet(EGameplayInputAction.F8); } set { Actions.Set(EGameplayInputAction.F8, value); } }
        public bool F9 { get { return Actions.IsSet(EGameplayInputAction.F9); } set { Actions.Set(EGameplayInputAction.F9, value); } }
        public bool F10 { get { return Actions.IsSet(EGameplayInputAction.F10); } set { Actions.Set(EGameplayInputAction.F10, value); } }

    }

    public static class GameplayInputActionExtensions
    {
        public static bool IsActive(this EGameplayInputAction action, GameplayInput input)
        {
            return input.Actions.IsSet(action) == true;
        }

        public static bool WasActivated(this EGameplayInputAction action, GameplayInput currentInput, GameplayInput previousInput)
        {
            return currentInput.Actions.IsSet(action) == true && previousInput.Actions.IsSet(action) == false;
        }

        public static bool WasDeactivated(this EGameplayInputAction action, GameplayInput currentInput, GameplayInput previousInput)
        {
            return currentInput.Actions.IsSet(action) == false && previousInput.Actions.IsSet(action) == true;
        }
    }
}
