namespace Upstream.Data
{
    /// <summary>River predators. All of them are "sharks" for collision and eating rules; they differ in looks and moves.</summary>
    public enum PredatorKind
    {
        Shark,    // chases you steadily
        Pike,     // drifts, winds up, then lunges straight down
        Piranha,  // small, comes in schools of three, zigzags
        Catfish   // big and slow; only a very big fish can eat it
    }
}
