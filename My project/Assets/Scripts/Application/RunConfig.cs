using System;

namespace OneDayGame.Application
{
    [Serializable]
    public sealed class RunConfig
    {
        public int StartMaxHp = 100;

        public float UltimateStart = 100f;

        public float UltimateMax = 100f;

        public float UltimateRechargePerSecond = 18f;

        public int InitialStage = 1;

        public int PlayerStartX = 0;

        public int PlayerStartY = 0;

        public float PlayerDamageOnTouchInterval = 0.35f;

        public float PlayerClampSpeed = 11f;

        public float EnemyBoundaryPadding = 0.5f;
    }
}
