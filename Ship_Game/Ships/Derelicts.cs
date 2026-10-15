using SDGraphics;
using SDUtils;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game.Ships;

// the universe's derelicts: removals add them on the sim, save or UI thread, the sim thread updates them, a reload clears them
public sealed class Derelicts
{
    readonly Array<Derelict> Items = new();
    readonly object Sync = new();

    public int Count { get { lock (Sync) return Items.Count; } }

    public int NumSalvageable { get { lock (Sync) return CountSalvageable(); } }

    public Derelict[] ToArray() { lock (Sync) return Items.ToArray(); }

    int CountSalvageable()
    {
        int n = 0;
        for (int i = 0; i < Items.Count; ++i)
            if (Items[i].CanBeSalvaged)
                ++n;
        return n;
    }

    // the percent chance of a derelict death, lowered by the salvageable derelicts' share of their limit, to none at it
    public float Chance
    {
        get
        {
            int max = GlobalStats.Defaults.MaxSalvageableDerelicts;
            float share = max > 0 ? NumSalvageable / (float)max : 0f;
            return GlobalStats.Defaults.DerelictChance * (1f - share).Clamped(0f, 1f);
        }
    }

    // over its limit a salvageable one drifts; only a seen death hands its hull over, so a wreck the player does not know never shows
    public bool TryAdd(Ship ship, bool salvageable, bool seen)
    {
        lock (Sync)
        {
            SceneObject so = seen ? ship.GetSO() : null;
            if (salvageable && CountSalvageable() >= GlobalStats.Defaults.MaxSalvageableDerelicts)
                salvageable = false;
            if (!salvageable && (!seen || so == null || Items.Count - CountSalvageable() >= GlobalStats.Defaults.MaxDriftingDerelicts))
                return false;
            Items.Add(new Derelict(ship, so, salvageable, seen));
            if (so != null)
                ship.ReleaseSceneObject();
            return true;
        }
    }

    public void Update(FixedSimTime timeStep)
    {
        lock (Sync)
        {
            for (int i = Items.Count - 1; i >= 0; --i)
                if (!Items[i].Update(timeStep))
                    Items.RemoveAt(i);
        }
    }

    public void UpdateTurn()
    {
        lock (Sync)
        {
            for (int i = 0; i < Items.Count; ++i)
                Items[i].UpdateTurn();
        }
    }

    public void Clear()
    {
        lock (Sync)
        {
            try
            {
                for (int i = 0; i < Items.Count; ++i)
                    Items[i].RemoveSceneObject();
            }
            finally
            {
                Items.Clear();
            }
        }
    }
}
