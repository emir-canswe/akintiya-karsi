namespace Upstream.Data
{
    /// <summary>
    /// How much of the health bar (GameConfig.maxHP = 100) each hazard takes. Bigger hazards hurt more,
    /// but every one of them is at least 60, so a second hit always ends the run (small heals included).
    /// </summary>
    public static class HitDamage
    {
        public const int Piranha = 60;
        public const int SmallRock = 60;
        public const int Hook = 65;
        public const int BigRock = 70;
        public const int Log = 70;
        public const int Shark = 75;
        public const int Pike = 80;
        public const int BigShark = 90;
        public const int Catfish = 95;
        public const int Barracuda = 80;
        public const int Swordfish = 90;
        public const int Anglerfish = 95;

        /// <summary>The smallest hit; two of these must empty even a legendary fish's bar.</summary>
        public const int Min = Piranha;

        public static int ForPredator(PredatorKind kind, bool isLarge)
        {
            switch (kind)
            {
                case PredatorKind.Piranha: return Piranha;
                case PredatorKind.Pike: return Pike;
                case PredatorKind.Catfish: return Catfish;
                case PredatorKind.Barracuda: return Barracuda;
                case PredatorKind.Swordfish: return Swordfish;
                case PredatorKind.Anglerfish: return Anglerfish;
                default: return isLarge ? BigShark : Shark;
            }
        }

        public static int ForRock(bool isSmall) => isSmall ? SmallRock : BigRock;
    }
}
