namespace Fusion.Collywobbles.Futsal
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public static class IBall
    {
        public static float GROUND = 0.125f;

        public static float STOP_DISTANCE = 0.3f;
        public static float MAX_DISTANCE = 0.5f;
        public static float MIN_DISTANCE = 0.3f;
        public static float LERP_SPEED = 0.3f;
        public static float LERP_SPEED2 = 0.3f;

        public enum TARGET_TYPE
        {
            None,
            BlueGoal,
            RedGoal,
            BlueKeeper,
            RedKeeper
        }

        public enum STATE
        {
            Open,
            Possessed,
            DribbleLeft,
            DribbleRight,
            Goal,
            Keeper
        }

        //STATE _ballState { get; set; }

    }
}
