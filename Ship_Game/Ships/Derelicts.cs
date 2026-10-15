using SDUtils;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game.Ships;

// the universe's derelicts, added and updated on the sim thread; a reload clears them from the UI thread
public sealed class Derelicts
{
    Array<Derelict> Items = new();

    public int Count => Items.Count;

    public Derelict[] ToArray() => Items.ToArray();

    // false when the ship has no scene object or the drifting cap is full: it then shows the normal explosion
    public bool TryAdd(Ship ship)
    {
        SceneObject so = ship.GetSO();
        if (so == null || Items.Count >= GlobalStats.Defaults.MaxDriftingDerelicts)
            return false;
        Items.Add(new Derelict(ship, so));
        ship.ReleaseSceneObject();
        return true;
    }

    public void Update(FixedSimTime timeStep)
    {
        Array<Derelict> items = Items;
        for (int i = items.Count - 1; i >= 0; --i)
            if (!items[i].Update(timeStep))
                items.RemoveAt(i);
    }

    // swaps the list out, so an update running on the sim thread finishes on the old one
    public void Clear()
    {
        Array<Derelict> items = Items;
        Items = new();
        for (int i = 0; i < items.Count; ++i)
            items[i].RemoveSceneObject();
    }
}
