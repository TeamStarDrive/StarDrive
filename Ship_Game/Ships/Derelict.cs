using System;
using SDGraphics;
using SDUtils;
using Ship_Game.Audio;
using Ship_Game.Data.Serialization;
using Ship_Game.Graphics;
using Ship_Game.Graphics.Particles;
using Ship_Game.Universe;
using Ship_Game.Utils;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;
using Color = Microsoft.Xna.Framework.Color;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;
using Matrix = SDGraphics.Matrix;

namespace Ship_Game.Ships;

// a ship that died without exploding: it breaks up and drifts on with its lights out, tumbling and smoking, then sinks away;
// a salvageable one stops in its system and sinks only when its turns run out or a platform strips it empty
[StarDataType]
public sealed class Derelict
{
    // a salvageable derelict stops over this time and stays put; a drifting one keeps going and sinks after its breakup
    public const float StopSeconds = 10f;
    public const float MinBreakupSeconds = 3f;
    public const float MaxBreakupSeconds = 5f;
    public const float SinkSpeed = 50f;
    // one stripped empty sinks away twice as fast
    public const float StrippedSinkSpeed = 2f * SinkSpeed;
    public const float RemoveDepth = 5000f;
    // it shrinks as it sinks from its rest depth, to this share of its size at the removal depth
    public const float ScaleAtRemoval = 0.1f;
    // a salvageable one settles at 25 a second as deep as its radius, at most 500, keeping its size, so it looks a little distant
    public const float RestSinkSpeed = 25f;
    public const float MaxRestDepth = 500f;
    // smoke from 20% of its modules at death, thinning out to the last six over 60 s
    public const float SmokeSourcesPerModule = 0.2f;
    public const int MinSmokeSources = 6;
    public const float SmokeThinSeconds = 60f;
    // the breakup's small blasts, with their volume and limit in AudioConfig.yaml
    public const string BreakupBlastSound = "sd_derelict_breakup";
    // a salvageable derelict lasts this many turns, times the production pace, then sinks away
    public const int SalvageableTurns = 100;
    // the production to salvage: this share of the health the ship had left at its death, times its cost before any empire's bonus
    public const float SalvageShareOfHealth = 0.5f;
    // and never less than this, times the production pace
    public const float MinSalvageProduction = 50f;
    // while a platform strips it on screen, per production of the salvage speed: flashes a second, rounded up, and small pieces of junk a second
    public const float StripFlashesPerSalvage = 0.66f;
    public const float StripJunkPerSalvage = 0.32f;
    // each piece of junk flies off its hull in a random direction at this speed
    public const float MinStripJunkSpeed = 30f;
    public const float MaxStripJunkSpeed = 80f;
    public const float StripJunkShareOfRadius = 0.05f;

    [StarDataType]
    public struct SmokeSource
    {
        [StarData] public Vector3 Local;
        [StarData] public float Area;
        [StarData] public ShipModuleType Type;
        internal ShipModuleDamageVisualization Visual;
    }

    UniverseState Universe;
    SceneObject So;
    Vector2 MeshOffset;
    [StarData] readonly Vector2 DriftVelocity;
    [StarData] readonly Vector3 Spin; // radians per second around the X, Y and Z axes
    SmokeSource[] Smoke;
    // a save keeps all its smoke points while they thin out, then the six left
    [StarData] SmokeSource[] SavedSmoke;
    readonly float LightsAtDeath;
    readonly AudioEmitter BlastEmitter = new(maxDistance: GameAudio.ShipSfxDistance);
    float NextBlast;
    // its flashes and junk run on this long after a platform last stripped it, while the player sees that platform
    float StripEffectsLeft;
    Ship StrippedBy;
    float NextStripFlash;
    float NextStripJunk;

    // unique among the universe's objects, so a salvage order saved with its empire finds it again
    [StarData] public readonly int Id;
    [StarData] public IShipDesign Design { get; private set; }
    [StarData] public readonly Empire Owner;
    [StarData] public readonly float Radius;
    public readonly float BreakupSeconds;
    [StarData] public readonly bool Salvageable;
    // by empire id - 1; written on the sim thread, read on the UI thread
    [StarData] bool[] KnownBy;
    // by empire id - 1: the empires that had a salvage station built in its system while it lay there, which see its techs from then on
    [StarData] bool[] TechsShown = Empty<bool>.Array;
    [StarData] public int TurnsLeft { get; private set; }
    // a salvageable one's production to salvage, fixed at its death, and what is still left of it
    [StarData] public readonly float Production;
    // a salvageable one's techs, picked at its death, which its loads advance for the empire salvaging it
    [StarData] public readonly string[] Techs = Empty<string>.Array;
    [StarData] public float ProductionLeft { get; private set; }
    // a salvageable one's system, where it stops
    [StarData] public readonly SolarSystem System;
    // the empire salvaging it, set on the sim thread; its timer stands meanwhile
    [StarData] public Empire Claimant { get; private set; }
    [StarData] public Vector2 Position { get; private set; }
    [StarData] public float Z { get; private set; }
    [StarData] public float Age { get; private set; }
    [StarData] public float RotationX { get; private set; }
    [StarData] public float RotationY { get; private set; }
    [StarData] public float RotationZ { get; private set; }
    public int NumSmokeSources => Smoke.Length;
    public bool InScene => So != null;

    public int NumSmokingNow
    {
        get
        {
            int last = Math.Min(MinSmokeSources, Smoke.Length);
            float thinned = (Age / SmokeThinSeconds).Clamped(0f, 1f);
            return Smoke.Length - (int)((Smoke.Length - last) * thinned);
        }
    }

    // seen: it breaks up where the ship died; unseen, it is a salvageable one already at its stop point, with no effects
    public Derelict(Ship ship, SceneObject so, bool salvageable, bool seen)
    {
        Universe = ship.Universe;
        So = so;
        Id = Universe.CreateId();
        Design = ship.ShipData;
        Owner = ship.Loyalty;
        Salvageable = salvageable;
        MeshOffset = ship.ShipData.BaseHull.MeshOffset;
        Radius = ship.Radius;
        Position = ship.Position;
        RotationX = ship.XRotation;
        RotationY = ship.YRotation;
        RotationZ = ship.Rotation;
        LightsAtDeath = ship.LightsOut ? 0f : 1f;
        DriftVelocity = DriftVelocityOf(ship);

        var random = Universe.Random;
        float spin = 50f / ship.SurfaceArea;
        Spin = new(random.Float(-1f, 1f) * spin, random.Float(-1f, 1f) * spin, random.Float(-1f, 1f) * spin);
        BreakupSeconds = random.Float(MinBreakupSeconds, MaxBreakupSeconds);
        Smoke = PickSmokeSources(ship);

        KnownBy = new bool[Universe.NumEmpires];
        var empires = Universe.Empires;
        for (int i = 0; i < empires.Count; ++i)
            if (ship.KnownByEmpires.KnownBy(empires[i]))
                SetKnownBy(empires[i]);
        if (salvageable)
        {
            TurnsLeft = (int)(SalvageableTurns * Universe.ProductionPace);
            Production = (ship.HealthPercent.Clamped(0f, 1f) * SalvageShareOfHealth * Design.GetCostBeforeBonuses(Universe.ProductionPace))
                         .LowerBound(MinSalvageProduction * Universe.ProductionPace);
            ProductionLeft = Production;
            Techs = PickTechs(Design, Owner, random);
            System = SalvageSystemAt(Universe, StopPoint(ship));
        }

        if (!seen)
        {
            Position = StopPoint(ship);
            Z = RestDepth;
            Age = StopSeconds;
            return;
        }
        GameAudio.PlaySfxAsync("sd_explosion_ship_det_small", ship.SoundEmitter);
        SpaceJunk.SpawnJunk(Universe, (int)(Radius * 0.03f), Position, DriftVelocity, ship,
                            maxSize: Radius * 0.1f, ignite: true);
    }

    [StarDataConstructor]
    Derelict()
    {
    }

    [StarDataSerialize]
    StarDataDynamicField[] OnSerialize()
    {
        var smoke = new SmokeSource[Age < SmokeThinSeconds ? Smoke.Length : Math.Min(MinSmokeSources, Smoke.Length)];
        for (int i = 0; i < smoke.Length; ++i)
            smoke[i] = new() { Local = Smoke[i].Local, Area = Smoke[i].Area, Type = Smoke[i].Type };
        return new StarDataDynamicField[] { new(nameof(SavedSmoke), smoke) };
    }

    // from a save: its lights out, past its breakup, and no model until the player sees it; with salvage ops off it sinks away
    [StarDataDeserialized(typeof(ShipDesign), typeof(UniverseParams))]
    public void OnDeserialized(UniverseState us)
    {
        Universe = us;
        if (Design is { IsAnExistingSavedDesign: true })
            Design = ResourceManager.Ships.GetDesign(Design.Name);
        if (Design?.BaseHull != null)
            MeshOffset = Design.BaseHull.MeshOffset;
        if (!us.P.SalvageOps)
            TurnsLeft = 0;
        KnownBy ??= Empty<bool>.Array;
        Smoke = SavedSmoke ?? Empty<SmokeSource>.Array;
        SavedSmoke = null;
    }

    static Vector2 DriftVelocityOf(Ship ship)
    {
        Vector2 velocity = ship.Velocity;
        float speed = velocity.Length();
        return speed > ship.MaxSTLSpeed ? velocity * (ship.MaxSTLSpeed / speed) : velocity;
    }

    // a salvageable derelict slows linearly to a stop, so it covers half the distance it would drift in that time
    public static Vector2 StopPoint(Ship ship) => ship.Position + DriftVelocityOf(ship) * (StopSeconds * 0.5f);

    // the system a salvageable derelict would stop in: none outside every system, or inside a star's radiation or 2000 around it
    public static SolarSystem SalvageSystemAt(UniverseState universe, Vector2 stop)
    {
        var systems = universe.Systems;
        for (int i = 0; i < systems.Count; ++i)
        {
            SolarSystem system = systems[i];
            float distance = stop.Distance(system.Position);
            if (distance < system.Radius)
                return system.InSafeDistanceFromRadiation(distance) ? system : null;
        }
        return null;
    }

    // how many techs a salvageable one holds, by its hull role; an untethered station holds 3
    public static int TechsHeld(RoleName hullRole) => hullRole switch
    {
        RoleName.frigate    => 2,
        RoleName.destroyer  => 3,
        RoleName.cruiser    => 4,
        RoleName.battleship => 5,
        RoleName.capital    => 6,
        _                   => 3,
    };

    // the techs that unlock the design's modules and its hull, root techs left out
    public static Array<string> TechsOf(IShipDesign design)
    {
        var techs = new Array<string>();
        foreach (Technology tech in ResourceManager.TechsList)
            if (!tech.IsRootNode && (UnlocksAModule(tech, design.UniqueModuleUIDs) || UnlocksHull(tech, design.Hull)))
                techs.Add(tech.UID);
        return techs;
    }

    static bool UnlocksAModule(Technology tech, string[] modules)
    {
        for (int i = 0; i < tech.ModulesUnlocked.Count; ++i)
            if (modules.Contains(tech.ModulesUnlocked[i].ModuleUID))
                return true;
        return false;
    }

    static bool UnlocksHull(Technology tech, string hull)
    {
        for (int i = 0; i < tech.HullsUnlocked.Count; ++i)
            if (tech.HullsUnlocked[i].Name == hull)
                return true;
        return false;
    }

    // as many of its design's techs as its hull role holds, at random; a Remnant ship holds none
    static string[] PickTechs(IShipDesign design, Empire owner, RandomBase random)
    {
        if (owner.WeAreRemnants)
            return Empty<string>.Array;

        Array<string> techs = TechsOf(design);
        var held = new string[Math.Min(TechsHeld(design.HullRole), techs.Count)];
        for (int i = 0; i < held.Length; ++i)
        {
            int pick = random.InRange(i, techs.Count);
            held[i] = techs[pick];
            techs[pick] = techs[i];
        }
        return held;
    }

    public bool IsKnownBy(Empire empire) => IsSet(KnownBy, empire);

    public bool KnownByPlayer => IsKnownBy(Universe.Player);

    // under the list's lock
    internal void SetKnownBy(Empire empire) => Set(ref KnownBy, empire);

    public bool ShowsTechsTo(Empire empire) => IsSet(TechsShown, empire);

    // under the list's lock
    internal void ShowTechsTo(Empire empire) => Set(ref TechsShown, empire);

    static bool IsSet(bool[] byEmpire, Empire empire)
    {
        int i = empire.Id - 1;
        return i < byEmpire.Length && byEmpire[i];
    }

    // a list too short for the empire grows into a new array, so the UI thread reads the old one or the new one whole
    void Set(ref bool[] byEmpire, Empire empire)
    {
        int i = empire.Id - 1;
        if (i >= byEmpire.Length)
        {
            var grown = new bool[Universe.NumEmpires.LowerBound(i + 1)];
            Array.Copy(byEmpire, grown, byEmpire.Length);
            grown[i] = true;
            byEmpire = grown;
        }
        else
        {
            byEmpire[i] = true;
        }
    }

    // a salvageable one sinks once its turns run out or it is stripped empty
    public bool Sinking => Salvageable ? TurnsLeft <= 0 || Stripped : Age > BreakupSeconds;

    public bool Stripped => Salvageable && ProductionLeft <= 0f;

    public bool CanBeSalvaged => Salvageable && TurnsLeft > 0 && !Stripped;

    // the player finds on the map and selects only the salvageable ones it knows of
    public bool SelectableByPlayer => CanBeSalvaged && KnownByPlayer;

    // once a turn, on the sim thread: its timer runs down while no empire salvages it
    public void UpdateTurn()
    {
        if (CanBeSalvaged && Claimant == null)
            --TurnsLeft;
    }

    // on the sim thread: false when another empire holds it
    public bool TryClaim(Empire empire)
    {
        if (Claimant != null && Claimant != empire)
            return false;
        Claimant = empire;
        return true;
    }

    // on the sim thread: its timer runs on, and any empire can salvage it again
    public void ReleaseClaim()
    {
        Claimant = null;
    }

    // under the list's lock, on the sim thread: up to this much of what is left; returns what was stripped
    internal float Strip(Ship platform, float amount)
    {
        float stripped = amount.Clamped(0f, ProductionLeft);
        if (stripped <= 0f)
            return 0f;

        ProductionLeft -= stripped;
        // a little over a turn, so they run unbroken while a platform strips it every turn; one stripped empty sinks with none
        StripEffectsLeft = Stripped ? 0f : Universe.P.TurnTimer * 1.2f;
        StrippedBy = platform;
        return stripped;
    }

    // on the sim thread: its flashes and junk show while a platform strips it that the player sees, as a supply ship's shuttles do
    internal bool StripEffectsShown => StripEffectsLeft > 0f && StrippedBy is { Active: true, InPlayerSensorRange: true };

    // dead modules first, then damaged ones; conduits only spark, so they are left out
    static SmokeSource[] PickSmokeSources(Ship ship)
    {
        var dead = new Array<ShipModule>();
        var damaged = new Array<ShipModule>();
        ShipModule[] modules = ship.Modules;
        for (int i = 0; i < modules.Length; ++i)
        {
            ShipModule m = modules[i];
            if (!m.CanVisualizeDamage || m.ModuleType == ShipModuleType.PowerConduit)
                continue;
            if (!m.Active)
                dead.Add(m);
            else if (m.Health < m.ActualMaxHealth)
                damaged.Add(m);
        }

        int wanted = Math.Max(MinSmokeSources, (int)(modules.Length * SmokeSourcesPerModule + 0.5f));
        var sources = new Array<SmokeSource>();
        AddRandomSources(sources, wanted, dead, ship.Universe.Random);
        AddRandomSources(sources, wanted, damaged, ship.Universe.Random);
        return sources.ToArray();
    }

    static void AddRandomSources(Array<SmokeSource> sources, int wanted, Array<ShipModule> modules, RandomBase random)
    {
        while (sources.Count < wanted && modules.Count > 0)
        {
            int pick = random.InRange(modules.Count);
            ShipModule m = modules[pick];
            modules.RemoveAtSwapLast(pick);
            sources.Add(new()
            {
                Local = new(m.LocalCenter, m.HullSurfaceZ),
                Area = m.Area,
                Type = m.ModuleType,
            });
        }
    }

    public Vector2 VelocityNow => Salvageable ? DriftVelocity * (1f - Age / StopSeconds).Clamped(0f, 1f) : DriftVelocity;

    // where a salvageable one comes to rest: slowing linearly, it covers half its speed times the time left
    public Vector2 RestPosition => Salvageable ? Position + VelocityNow * ((StopSeconds - Age) * 0.5f) : Position;

    public float RestDepth => Salvageable ? Math.Min(Radius, MaxRestDepth) : 0f;

    public float Scale => 1f - (1f - ScaleAtRemoval) * ((Z - RestDepth) / (RemoveDepth - RestDepth)).Clamped(0f, 1f);

    Matrix Rotation => Matrix.CreateRotationY(RotationY) * Matrix.CreateRotationX(RotationX) * Matrix.CreateRotationZ(RotationZ);

    Matrix World(in Matrix rotation, float scale)
    {
        return Matrix.CreateTranslation(new Vector3(MeshOffset, 0f)) * Matrix.CreateScale(scale)
             * rotation * Matrix.CreateTranslation(new Vector3(Position, Z));
    }

    // on the UI thread: one with no model wants it once the player knows of it and it is on screen
    public bool WantsModel => So == null && KnownByPlayer && Universe.IsSystemViewOrCloser
                              && Universe.Screen.IsInFrustum(new Vector3(Position, Z), Radius);

    // under the list's lock; it keeps the model until it sinks away or the content reloads, so it needs no cleanup like a ship's
    public bool AttachModel(SceneObject so)
    {
        if (So != null)
            return false;
        so.World = World(Rotation, Scale);
        so.EmissiveScale = 0f;
        so.Visibility = GlobalStats.ShipVisibility;
        So = so;
        Universe.Screen.AddObject(so);
        return true;
    }

    // false once it has sunk out of sight and left the scene; with no model, one the player does not know of is gone once it starts to sink
    public bool Update(FixedSimTime timeStep)
    {
        SceneObject so = So;
        if (so == null && Sinking && !KnownByPlayer)
            return false;

        float dt = timeStep.FixedTime;
        Position += VelocityNow * dt;
        Age += dt;
        if ((StripEffectsLeft -= dt) <= 0f)
            StrippedBy = null;
        if (Sinking)
            Z += (Stripped ? StrippedSinkSpeed : SinkSpeed) * dt;
        else if (Z < RestDepth)
            Z = (Z + RestSinkSpeed * dt).UpperBound(RestDepth);

        if (Z >= RemoveDepth)
        {
            RemoveSceneObject();
            return false;
        }
        if (so == null)
            return true;

        float scale = Scale;
        bool visible = Universe.IsSystemViewOrCloser && Universe.Screen.IsInFrustum(new Vector3(Position, Z), Radius * scale);
        so.Visibility = visible ? GlobalStats.ShipVisibility : ObjectVisibility.None;
        if (!visible)
        {
            for (int i = 0; i < Smoke.Length; ++i)
                Smoke[i].Visual = null;
            return true;
        }

        RotationX += Spin.X * dt;
        RotationY += Spin.Y * dt;
        RotationZ = (RotationZ + Spin.Z * dt).AsNormalizedRadians();

        Matrix rotation = Rotation;
        var center = new Vector3(Position, Z);
        so.World = World(rotation, scale);
        so.EmissiveScale = Age < BreakupSeconds ? LightsAtDeath * FluorescentLight.Level(FluorescentLight.TurnOff, Age / BreakupSeconds) : 0f;

        ParticleManager particles = Universe.Screen.Particles;
        if (dt <= 0f || particles == null)
            return true;

        if (Age < BreakupSeconds)
            UpdateBreakup(dt, particles);
        if (StripEffectsShown)
            UpdateStripping(dt, particles, scale);

        int smoking = NumSmokingNow;
        for (int i = 0; i < Smoke.Length; ++i)
        {
            ref SmokeSource s = ref Smoke[i];
            if (i >= smoking)
            {
                s.Visual = null;
                continue;
            }
            Vector3 pos = (s.Local * scale).Transform(rotation) + center;
            s.Visual ??= new ShipModuleDamageVisualization(s.Area, pos, s.Type, particles);
            s.Visual.Update(timeStep, pos, scale: scale, isAlive: false);
        }
        return true;
    }

    void UpdateBreakup(float dt, ParticleManager particles)
    {
        var random = Universe.Random;
        Vector2 velocity = VelocityNow;
        if ((NextBlast -= dt) <= 0f)
        {
            NextBlast = random.Float(0.15f, 0.5f);
            var pos = new Vector3(Position + random.Vector2D(Radius * 0.5f), Z);
            ExplosionManager.AddExplosion(Universe.Screen, pos, velocity, Radius * 0.5f, 2.5f, ExplosionType.Projectile);
            particles.Flash.AddParticle(pos);
            BlastEmitter.Position = pos;
            GameAudio.PlaySfxAsync(BreakupBlastSound, BlastEmitter);
        }
        if (random.RollDice(50))
        {
            var pos = new Vector3(Position + random.Vector2D(Radius * 0.5f), Z);
            particles.Lightning.AddParticle(pos, new Vector3(velocity, 0f), 0.5f, Color.White);
        }
    }

    // flashes at random points on its hull, and small pieces of junk pulled off it in random directions, both at random intervals around their rates
    void UpdateStripping(float dt, ParticleManager particles, float scale)
    {
        var random = Universe.Random;
        float salvage = GlobalStats.Defaults.SalvageSpeed;
        float spread = Radius * 0.5f * scale;
        if ((NextStripFlash -= dt) <= 0f)
        {
            NextStripFlash = random.Float(0.5f, 1.5f) / (float)Math.Ceiling(salvage * StripFlashesPerSalvage);
            particles.Flash.AddParticle(new Vector3(Position + random.Vector2D(spread), Z));
        }
        if ((NextStripJunk -= dt) <= 0f)
        {
            NextStripJunk = random.Float(0.5f, 1.5f) / (salvage * StripJunkPerSalvage);
            Vector2 velocity = random.Direction2D() * random.Float(MinStripJunkSpeed, MaxStripJunkSpeed);
            SpaceJunk.SpawnJunk(Universe, new Vector3(Position + random.Vector2D(spread), Z), velocity, Radius * StripJunkShareOfRadius * scale);
        }
    }

    public void RemoveSceneObject()
    {
        if (So != null)
        {
            Universe.Screen.RemoveObject(So);
            So = null;
        }
        for (int i = 0; i < Smoke.Length; ++i)
            Smoke[i].Visual = null;
    }
}
