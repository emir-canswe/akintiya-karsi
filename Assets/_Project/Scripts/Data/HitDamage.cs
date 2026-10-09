namespace Upstream.Data
{
    /// <summary>
    /// How much of the health bar (GameConfig.maxHP = 100) each hazard takes. Bigger hazards hurt more.
    /// </summary>
    public static class HitDamage
    {
        public const int Piranha = 15;
        public const int SmallRock = 20;
        public const int Hook = 25;
        public const int BigRock = 30;
        public const int Log = 30;
        public const int Shark = 35;
        public const int Pike = 40;
        public const int BigShark = 45;
        public const int Catfish = 50;

        public static int ForPredator(PredatorKind kind, bool isLarge)
        {
            switch (kind)
            {
                case PredatorKind.Piranha: return Piranha;
                case PredatorKind.Pike: return Pike;
                case PredatorKind.Catfish: return Catfish;
                default: return isLarge ? BigShark : Shark;
            }
        }

        public static int ForRock(bool isSmall) => isSmall ? SmallRock : BigRock;
    }
}
