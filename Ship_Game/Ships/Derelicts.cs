using System;
using System.Collections.Generic;
using System.Threading;
using SDGraphics;
using SDUtils;
using Ship_Game.Commands.Goals;
using Ship_Game.Universe;
using Ship_Game.Utils;
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

    public int NumDrifting { get { lock (Sync) return CountDrifting(); } }

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

    // a test's fixed outcome for the hull's roll; null in the game
    internal bool? TestDriftRoll;

    // the hull's roll for a derelict to drift rather than lie to be salvaged
    public bool RollsDrifting(RoleName hullRole, RandomBase random) => TestDriftRoll ?? random.RollDice(Derelict.DriftingChance(hullRole));

    // the drifting cap counts every derelict past salvage, with a model or not, but none rising for its ship
    int CountDrifting() => Items.Count(d => !d.CanBeSalvaged && !d.RisingForItsShip);

    static bool PlayerKnowsAtDeath(Ship ship, Empire killer)
    {
        Empire player = ship.Universe.Player;
        return player != null && (ship.Loyalty == player || killer == player || InOwnSensors(player, ship.Position));
    }

    // from any thread: within the sensors of the empire's own ships and planets, its allies' and its moles' left out
    static bool InOwnSensors(Empire empire, Vector2 point)
    {
        Empire.InfluenceNode[] nodes = empire.SensorNodes;
        for (int i = 0; i < nodes.Length; ++i)
        {
            GameObject source = nodes[i].Source;
            bool own = source is Ship s ? s.Active && s.Loyalty == empire : source is Planet p && p.Owner == empire;
            if (own && point.InRadius(source.Position, nodes[i].Radius))
                return true;
        }
        return false;
    }

    // under the lock: outside every system only a wreck the player owned, destroyed or had in its own sensors drifts, and at the cap one with a model takes the place of the oldest drifting one without
    bool HasPlaceToDrift(Ship ship, bool withModel, Empire killer)
    {
        if (Derelict.SystemAt(ship.Universe, Derelict.StopPoint(ship)) == null && !PlayerKnowsAtDeath(ship, killer))
            return false;
        if (CountDrifting() < DriftingCap(ship.Universe))
            return true;
        if (!withModel)
            return false;
        for (int i = 0; i < Items.Count; ++i)
        {
            if (!Items[i].Salvageable && !Items[i].InScene)
            {
                Items.RemoveAt(i);
                return true;
            }
        }
        return false;
    }

    // from any thread: a derelict of the design that may yet come back as a ship, salvageable or rising for its ship and not a station, which keeps the design from being overwritten or deleted
    public bool AnyMayComeBackAs(string designName)
    {
        Derelict[] current = Current;
        for (int i = 0; i < current.Length; ++i)
        {
            Derelict d = current[i];
            if ((d.CanBeSalvaged || d.RisingForItsShip) && DerelictRecovery.CanRecover(d) && d.Design.Name == designName)
                return true;
        }
        return false;
    }

    const float MinCapMultiplier = 0.1f;
    const float MaxCapMultiplier = 10f;

    // the galaxy's star systems times the multiplier, used from 0.1 to 10; the cap is rounded up, at least 1, and a float's stray last digit never rounds it up
    static int Cap(UniverseState universe, float multiplier)
        => Math.Max(1, (int)Math.Ceiling(universe.Systems.Count * multiplier.Clamped(MinCapMultiplier, MaxCapMultiplier) - 0.001f));

    // as the game or a mod loads: a cap multiplier out of 0.1 to 10 is used as the nearer of the two
    public static void WarnOfCapMultipliers(GamePlayGlobals settings, string file)
    {
        WarnIfOutOfRange(nameof(GamePlayGlobals.DriftingDerelictCapMultiplier), settings.DriftingDerelictCapMultiplier, file);
        WarnIfOutOfRange(nameof(GamePlayGlobals.SalvageableDerelictCapMultiplier), settings.SalvageableDerelictCapMultiplier, file);
    }

    static void WarnIfOutOfRange(string knob, float multiplier, string file)
    {
        if (multiplier > MaxCapMultiplier)
            Log.Warning($"{file}: {knob}={multiplier} is over {MaxCapMultiplier}, so {MaxCapMultiplier} is used");
        else if (multiplier < MinCapMultiplier)
            Log.Warning($"{file}: {knob}={multiplier} is under {MinCapMultiplier}, so {MinCapMultiplier} is used");
    }

    public static int SalvageableCap(UniverseState universe) => Cap(universe, GlobalStats.Defaults.SalvageableDerelictCapMultiplier);

    public static int DriftingCap(UniverseState universe) => Cap(universe, GlobalStats.Defaults.DriftingDerelictCapMultiplier);

    // the percent chance of a derelict death, lowered by the share of their limit taken, to none at it: by the salvageable derelicts with salvage ops on, the drifting ones with them off
    public float Chance(UniverseState universe)
    {
        float share = universe.P.SalvageOps ? NumSalvageable / (float)SalvageableCap(universe) : NumDrifting / (float)DriftingCap(universe);
        return GlobalStats.Defaults.DerelictChance * (1f - share).Clamped(0f, 1f);
    }

    // over its limit a salvageable one drifts; a drifting one is left seen or not where it has a place, but only a seen death hands its hull over, and an unseen one gets its model once the player knows of it and sees it
    public bool TryAdd(Ship ship, bool salvageable, bool seen, Empire killer = null)
    {
        Derelict added;
        lock (Sync)
        {
            SceneObject so = seen ? ship.GetSO() : null;
            if (salvageable && CountSalvageable() >= SalvageableCap(ship.Universe))
                salvageable = false;
            if (!salvageable && !HasPlaceToDrift(ship, so != null, killer))
                return false;
            added = new Derelict(ship, so, salvageable, seen: salvageable ? seen : so != null);
            if (!salvageable && PlayerKnowsAtDeath(ship, killer))
                added.SetKnownBy(ship.Universe.Player);
            Items.Add(added);
            Publish();
            if (so != null)
                ship.ReleaseSceneObject();
        }
        if (added.CanBeSalvaged && added.System != null)
            ship.Universe.Screen?.RunOnSimThread(() => Join(added, killer));
        else if (!added.Salvageable && added.System != null && !added.KnownByPlayer)
            ship.Universe.Screen?.RunOnSimThread(() => LearnOfDrifting(added, killer));
        return true;
    }

    // on the sim thread, which owns the empires' lists: the player learns of a drifting one in a system as of a salvageable one there
    void LearnOfDrifting(Derelict d, Empire killer)
    {
        Empire player = d.System.Universe.Player;
        lock (Sync)
        {
            if (Items.ContainsRef(d) && (LearnsOf(player, d, killer) || player.DerelictSystems.Contains(d.System)))
                d.SetKnownBy(player);
        }
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

    // on the sim thread: a ship entering a system with derelicts its empire does not list shows them to its empire, and a player's ship shows the player the drifting ones there
    public void Enter(Ship ship)
    {
        SolarSystem system = ship.System;
        Empire e = ship.Loyalty;
        Derelict first;
        lock (Sync)
        {
            if (system == null)
                return;
            if (e.isPlayer)
                ShowDriftingIn(e, system);
            if (system.Derelicts.Count == 0 || e.DerelictSystems.Contains(system))
                return;
            ListSystem(e, system);
            first = system.Derelicts[0];
        }
        if (e.isPlayer)
            system.Universe.Notifications?.AddNewDerelictNotification(first);
    }

    // under the lock
    void ShowDriftingIn(Empire player, SolarSystem system)
    {
        for (int i = 0; i < Items.Count; ++i)
            if (!Items[i].Salvageable && Items[i].System == system)
                Items[i].SetKnownBy(player);
    }

    // a ship that changes hands where it is counts as entering its system for its new owner; a save or the UI can apply the change of hands
    public void ChangedHands(Ship ship)
    {
        if (ship.Universe.Screen is { IsOnSimThread: false } screen)
            screen.RunOnSimThread(() => Enter(ship));
        else
            Enter(ship);
    }

    // under the lock: the empire lists the system, so it knows every derelict in it, a player the drifting ones too
    void ListSystem(Empire e, SolarSystem system)
    {
        e.DerelictSystems.Add(system);
        for (int i = 0; i < system.Derelicts.Count; ++i)
            system.Derelicts[i].SetKnownBy(e);
        if (e.isPlayer)
            ShowDriftingIn(e, system);
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

    // on the sim thread: a failed recovery breaks it up, heard where the player sees it, and it leaves its system at once
    public void BreakApart(Derelict d)
    {
        lock (Sync)
        {
            if (d.OnScreen)
                d.Sound(Derelict.GiveWaySound);
            d.BreakApart();
            Leave(d);
        }
    }

    // on the sim thread: a successful recovery raises it for the empire, heard where the player sees it, and it leaves its system at once
    public void Rise(Derelict d, Empire empire)
    {
        lock (Sync)
        {
            d.Rise(empire);
            Leave(d);
            if (d.OnScreen)
                d.Sound(Derelict.RecoveredSound);
        }
    }

    // on the sim thread: a recovery's break-in turn, heard where its effects show on screen
    public void SoundBreakIn(Derelict d)
    {
        lock (Sync)
        {
            if (d.OnScreen && d.RecoveryEffectsShown)
                d.Sound(Derelict.BreakInSound);
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

    // on the sim thread; one risen for its ship leaves the list with its model, and its ship is made after the lock, as a new ship joins its empire's lists, even when another derelict's update or hand-off throws
    public void Update(FixedSimTime timeStep)
    {
        Array<(Derelict Derelict, SceneObject Model)> risen = null;
        try
        {
            lock (Sync)
            {
                bool removed = false;
                try
                {
                    for (int i = Items.Count - 1; i >= 0; --i)
                    {
                        Derelict d = Items[i];
                        bool stays = d.Update(timeStep);
                        if (stays && d.Risen)
                        {
                            (risen ??= new()).Add((d, d.TakeModel()));
                            stays = false;
                        }
                        if (!stays)
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
        finally
        {
            for (int i = 0; risen != null && i < risen.Count; ++i)
            {
                try
                {
                    risen[i].Derelict.BringBackShip(risen[i].Model);
                }
                catch (Exception e)
                {
                    Log.Error(e, $"Recovered ship hand-off failed for design {risen[i].Derelict.Design.Name}");
                }
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
