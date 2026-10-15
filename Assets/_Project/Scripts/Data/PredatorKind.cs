namespace Upstream.Data
{
    /// <summary>River predators. All of them are "sharks" for collision and eating rules; they differ in looks and moves.</summary>
    public enum PredatorKind
    {
        Shark,    // chases you steadily
        Pike,     // drifts, winds up, then lunges straight down
        Piranha,  // small, comes in schools of three, zigzags
        Catfish,  // big and slow; only a very big fish can eat it
        // Hunters of the late river: they show up at 3000, 4000 and 5000 m. Only a very big fish can eat them.
        Barracuda,  // 3000 m: fastest swimmer, homes in hard
        Swordfish,  // 4000 m: long wind-up, then a near-straight charge with its bill
        Anglerfish  // 5000 m: huge and dark, lures with a glowing orb, wide hit area
    }
}
