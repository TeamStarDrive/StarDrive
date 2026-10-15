using System.Threading;
using SDGraphics;
using SDUtils;
using Ship_Game.Universe;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game.Ships;

// the universe's derelicts under one lock: added on the sim, save, load or UI thread, updated on the sim thread, given models on the UI thread
public sealed class Derelicts
{
    readonly Array<Derelict> Items = new();
    readonly object Sync = new();
    // rebuilt under the lock whenever the list changes, so the UI thread reads it every frame without waiting
    Derelict[] Shared = Empty<Derelict>.Array;

    public int Count { get { lock (Sync) return Items.Count; } }

    public Derelict[] Current => Volatile.Read(ref Shared);

    void Publish() => Volatile.Write(ref Shared, Items.ToArray());

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

    // the drifting cap counts only the ones with a model: a wreck nobody can see costs nothing
    int CountDriftingInScene()
    {
        int n = 0;
        for (int i = 0; i < Items.Count; ++i)
            if (!Items[i].CanBeSalvaged && Items[i].InScene)
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
            if (!salvageable && (!seen || so == null || CountDriftingInScene() >= GlobalStats.Defaults.MaxDriftingDerelicts))
                return false;
            Items.Add(new Derelict(ship, so, salvageable, seen));
            Publish();
            if (so != null)
                ship.ReleaseSceneObject();
            return true;
        }
    }

    public void Update(FixedSimTime timeStep)
    {
        lock (Sync)
        {
            bool removed = false;
            try
            {
                for (int i = Items.Count - 1; i >= 0; --i)
                {
                    if (!Items[i].Update(timeStep))
                    {
                        Items.RemoveAt(i);
                        removed = true;
                    }
                }
            }
            finally
            {
                if (removed)
                    Publish();
            }
        }
    }

    public Derelict.SaveData[] GetSaves()
    {
        lock (Sync)
        {
            var saves = new Derelict.SaveData[Items.Count];
            for (int i = 0; i < saves.Length; ++i)
                saves[i] = Items[i].ToSave();
            return saves;
        }
    }

    public void Load(UniverseState universe, Derelict.SaveData[] saves)
    {
        if (saves == null)
            return;
        lock (Sync)
        {
            try
            {
                for (int i = 0; i < saves.Length; ++i)
                    if (saves[i].Design?.BaseHull != null)
                        Items.Add(new Derelict(universe, saves[i]));
            }
            finally
            {
                Publish();
            }
        }
    }

    // asked every frame on the UI thread: when the sim thread holds the list, a model can wait a frame
    public Derelict[] WantingModels()
    {
        if (!Monitor.TryEnter(Sync))
            return Empty<Derelict>.Array;
        try
        {
            Array<Derelict> wanting = null;
            for (int i = 0; i < Items.Count; ++i)
                if (Items[i].WantsModel)
                    (wanting ??= new()).Add(Items[i]);
            return wanting?.ToArray() ?? Empty<Derelict>.Array;
        }
        finally
        {
            Monitor.Exit(Sync);
        }
    }

    // false when it left the list or already has a model
    public bool TryAttachModel(Derelict derelict, SceneObject so)
    {
        lock (Sync)
            return Items.ContainsRef(derelict) && derelict.AttachModel(so);
    }

    // on the UI thread, with the models loaded outside the lock so the sim thread does not wait on them
    public void LoadModels(UniverseScreen screen)
    {
        if (StarDriveGame.Instance == null || !screen.UState.IsSystemViewOrCloser)
            return; // no models in unit tests, and none wanted zoomed out
        Derelict[] wanting = WantingModels();
        for (int i = 0; i < wanting.Length; ++i)
            if (wanting[i].Design.LoadModel(out SceneObject so, screen.ContentManager))
                TryAttachModel(wanting[i], so);
    }

    public void UpdateTurn()
    {
        lock (Sync)
        {
            for (int i = 0; i < Items.Count; ++i)
                Items[i].UpdateTurn();
        }
    }

    // a content reload takes every model out of the scene: the derelicts stay and get a model again when seen
    public void RemoveSceneObjects()
    {
        lock (Sync)
        {
            for (int i = 0; i < Items.Count; ++i)
                Items[i].RemoveSceneObject();
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
                Publish();
            }
        }
    }
}
