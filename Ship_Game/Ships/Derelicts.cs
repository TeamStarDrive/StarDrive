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

    // from any thread, so it reads the published list and not the empire's, which the sim thread owns
    public bool AnyKnownIn(Empire empire, SolarSystem system)
    {
        Derelict[] current = Current;
        for (int i = 0; i < current.Length; ++i)
            if (current[i].System == system && current[i].CanBeSalvaged && current[i].IsKnownBy(empire))
                return true;
        return false;
    }

    // from any thread: how many salvageable derelicts the empire knows of in the system, the first of them, and the production left in them
    public int CountKnownIn(Empire empire, SolarSystem system, out Derelict first, out float productionLeft)
    {
        Derelict[] current = Current;
        int count = 0;
        first = null;
        productionLeft = 0f;
        for (int i = 0; i < current.Length; ++i)
        {
            if (current[i].System == system && current[i].CanBeSalvaged && current[i].IsKnownBy(empire))
            {
                ++count;
                first ??= current[i];
                productionLeft += current[i].ProductionLeft;
            }
        }
        return count;
    }

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
    public bool TryAdd(Ship ship, bool salvageable, bool seen, Empire killer = null)
    {
        Derelict added;
        lock (Sync)
        {
            SceneObject so = seen ? ship.GetSO() : null;
            if (salvageable && CountSalvageable() >= GlobalStats.Defaults.MaxSalvageableDerelicts)
                salvageable = false;
            if (!salvageable && (!seen || so == null || CountDriftingInScene() >= GlobalStats.Defaults.MaxDriftingDerelicts))
                return false;
            added = new Derelict(ship, so, salvageable, seen);
            Items.Add(added);
            Publish();
            if (so != null)
                ship.ReleaseSceneObject();
        }
        if (added.CanBeSalvaged && added.System != null)
            ship.Universe.Screen?.RunOnSimThread(() => Join(added, killer));
        return true;
    }

    // on the sim thread, which owns the empires' lists: it joins its system, and each empire that learns of it lists the system
    void Join(Derelict d, Empire killer)
    {
        SolarSystem system = d.System;
        bool playerLearned = false;
        lock (Sync)
        {
            if (!d.CanBeSalvaged || !Items.ContainsRef(d))
                return;
            system.Derelicts.Add(d);
            var empires = system.Universe.Empires;
            for (int i = 0; i < empires.Count; ++i)
            {
                Empire e = empires[i];
                if (e.DerelictSystems.Contains(system))
                {
                    d.SetKnownBy(e);
                }
                else if (LearnsOf(e, d, killer))
                {
                    ListSystem(e, system);
                    playerLearned |= e.isPlayer;
                }
            }
        }
        if (playerLearned)
            system.Universe.Notifications?.AddNewDerelictNotification(d);
    }

    // on the sim thread: a ship entering a system with derelicts its empire does not list shows them to its empire
    public void Enter(Ship ship)
    {
        SolarSystem system = ship.System;
        Empire e = ship.Loyalty;
        Derelict first;
        lock (Sync)
        {
            if (system == null || system.Derelicts.Count == 0 || e.DerelictSystems.Contains(system))
                return;
            ListSystem(e, system);
            first = system.Derelicts[0];
        }
        if (e.isPlayer)
            system.Universe.Notifications?.AddNewDerelictNotification(first);
    }

    // under the lock: the empire lists the system, so it knows every derelict in it
    static void ListSystem(Empire e, SolarSystem system)
    {
        e.DerelictSystems.Add(system);
        for (int i = 0; i < system.Derelicts.Count; ++i)
            system.Derelicts[i].SetKnownBy(e);
    }

    // who learns of it as it forms: its owner, its killer, sensors at its death, a colony or a ship there, allies and full infiltrators of either, a mole there
    static bool LearnsOf(Empire e, Derelict d, Empire killer)
    {
        return e == d.Owner || e == killer || d.IsKnownBy(e)
            || d.System.HasPlanetsOwnedBy(e) || HasShipIn(e, d.System)
            || e.IsAlliedWith(d.Owner) || e.IsAlliedWith(killer)
            || FullyInfiltrates(e, d.Owner) || FullyInfiltrates(e, killer)
            || HasMoleIn(e, d.System);
    }

    static bool HasShipIn(Empire e, SolarSystem system)
    {
        var ships = system.ShipList;
        for (int i = 0; i < ships.Count; ++i)
            if (ships[i].Loyalty == e)
                return true;
        return false;
    }

    static bool FullyInfiltrates(Empire e, Empire target)
    {
        return e.GetRelationsOrNull(target)?.Espionage?.Level >= Espionage.MaxLevel;
    }

    static bool HasMoleIn(Empire e, SolarSystem system)
    {
        var moles = e.data.MoleList;
        for (int i = 0; i < moles.Count; ++i)
            for (int j = 0; j < system.PlanetList.Count; ++j)
                if (moles[i].PlanetId == system.PlanetList[j].Id)
                    return true;
        return false;
    }

    // under the lock: one that can no longer be salvaged leaves its system, and a system with none left leaves every empire's list
    static void Leave(Derelict d)
    {
        SolarSystem system = d.System;
        if (system == null || !system.Derelicts.RemoveRef(d) || system.Derelicts.Count > 0)
            return;
        var empires = system.Universe.Empires;
        for (int i = 0; i < empires.Count; ++i)
            empires[i].DerelictSystems.Remove(system);
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
                RejoinSystems(universe);
            }
            finally
            {
                Publish();
            }
        }
    }

    // the lists are not saved: an empire lists every system with a derelict it knows of, and so knows every derelict there
    void RejoinSystems(UniverseState universe)
    {
        for (int i = 0; i < Items.Count; ++i)
            if (Items[i].CanBeSalvaged && Items[i].System != null)
                Items[i].System.Derelicts.Add(Items[i]);

        var empires = universe.Empires;
        for (int i = 0; i < Items.Count; ++i)
        {
            Derelict d = Items[i];
            if (!d.CanBeSalvaged || d.System == null)
                continue;
            for (int j = 0; j < empires.Count; ++j)
                if (d.IsKnownBy(empires[j]) && !empires[j].DerelictSystems.Contains(d.System))
                    ListSystem(empires[j], d.System);
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
            {
                Items[i].UpdateTurn();
                if (!Items[i].CanBeSalvaged)
                    Leave(Items[i]);
            }
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
                {
                    Items[i].RemoveSceneObject();
                    Leave(Items[i]);
                }
            }
            finally
            {
                Items.Clear();
                Publish();
            }
        }
    }
}
