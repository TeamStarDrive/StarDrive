using System.Collections.Generic;
using System.Threading;
using SDGraphics;
using SDUtils;
using Ship_Game.Commands.Goals;
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

    // from any thread: the derelict with this id, or null once it has left the list
    public Derelict Find(int id) => Current.Find(d => d.Id == id);

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

    int CountSalvageable() => Items.Count(d => d.CanBeSalvaged);

    // the drifting cap counts only the ones with a model: a wreck nobody can see costs nothing
    int CountDriftingInScene() => Items.Count(d => !d.CanBeSalvaged && d.InScene);

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

    // a ship that changes hands where it is counts as entering its system for its new owner; a save or the UI can apply the change of hands
    public void ChangedHands(Ship ship)
    {
        if (ship.Universe.Screen is { IsOnSimThread: false } screen)
            screen.RunOnSimThread(() => Enter(ship));
        else
            Enter(ship);
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

    static bool HasShipIn(Empire e, SolarSystem system) => system.ShipList.Any(s => s.Loyalty == e);

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

    // on the sim thread: a platform strips up to this much from the derelict, and one stripped empty leaves its system at once
    public float Strip(Derelict d, Ship platform, float amount)
    {
        lock (Sync)
        {
            float stripped = d.Strip(platform, amount);
            if (d.Stripped)
                Leave(d);
            return stripped;
        }
    }

    // on the sim thread: a failed recovery breaks it up, and it leaves its system at once
    public void BreakApart(Derelict d)
    {
        lock (Sync)
        {
            d.BreakApart();
            Leave(d);
        }
    }

    // on the sim thread: a failed recovery's catastrophe takes it out of the universe at once; its blast comes after the lock, as the ships it kills roll for derelicts of their own as they die
    public void Explode(Derelict d)
    {
        bool seen;
        lock (Sync)
        {
            seen = d.OnScreen;
            d.BreakApart(); // so a panel or order still holding it no longer finds it salvageable
            Leave(d);
            d.RemoveSceneObject();
            if (Items.RemoveRef(d))
                Publish();
        }
        d.Explode(seen);
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

    // a derelict whose design is gone from the game is left out
    public void Load(UniverseState universe, Derelict[] loaded)
    {
        if (loaded == null)
            return;
        lock (Sync)
        {
            try
            {
                for (int i = 0; i < loaded.Length; ++i)
                    if (loaded[i].Design?.BaseHull != null)
                        Items.Add(loaded[i]);
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
            ShowTechsToStations();
        }
    }

    // under the lock, on the sim thread: each empire with a salvage station built in a derelict's system, which so knows of it, sees its techs from then on; an empire's stations are looked up once a turn
    void ShowTechsToStations()
    {
        HashSet<SolarSystem>[] stations = null;
        for (int i = 0; i < Items.Count; ++i)
        {
            Derelict d = Items[i];
            if (!d.CanBeSalvaged || d.System == null)
                continue;
            var empires = d.System.Universe.Empires;
            stations ??= new HashSet<SolarSystem>[empires.Count];
            for (int j = 0; j < empires.Count; ++j)
            {
                Empire e = empires[j];
                if (d.IsKnownBy(e) && !d.ShowsTechsTo(e) && (stations[j] ??= SalvageOps.StationSystems(e)).Contains(d.System))
                    d.ShowTechsTo(e);
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
