using SDUtils;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game.Ships;

// the universe's derelicts: removals add them on the sim, save or UI thread, the sim thread updates them, a reload clears them
public sealed class Derelicts
{
    readonly Array<Derelict> Items = new();
    readonly object Sync = new();

    public int Count { get { lock (Sync) return Items.Count; } }

    public Derelict[] ToArray() { lock (Sync) return Items.ToArray(); }

    // false when the ship has no scene object or the drifting cap is full: it then shows the normal explosion
    public bool TryAdd(Ship ship)
    {
        lock (Sync)
        {
            SceneObject so = ship.GetSO();
            if (so == null || Items.Count >= GlobalStats.Defaults.MaxDriftingDerelicts)
                return false;
            Items.Add(new Derelict(ship, so));
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
