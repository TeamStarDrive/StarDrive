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
// a salvageable one stops in its system and sinks only when its turns run out
public sealed class Derelict
{
    // a salvageable derelict stops over this time and stays put; a drifting one keeps going and sinks after its breakup
    public const float StopSeconds = 10f;
    public const float MinBreakupSeconds = 3f;
    public const float MaxBreakupSeconds = 5f;
    public const float SinkSpeed = 50f;
    public const float RemoveDepth = 5000f;
    // it shrinks as it sinks, to this share of its size at the removal depth
    public const float ScaleAtRemoval = 0.1f;
    // smoke from 20% of its modules at death, thinning out to the last six over 60 s
    public const float SmokeSourcesPerModule = 0.2f;
    public const int MinSmokeSources = 6;
    public const float SmokeThinSeconds = 60f;
    // the breakup's small blasts, with their volume and limit in AudioConfig.yaml
    public const string BreakupBlastSound = "sd_derelict_breakup";
    // a salvageable derelict lasts this many turns, times the production pace, then sinks away
    public const int SalvageableTurns = 100;

    [StarDataType]
    public struct SmokeSource
    {
        [StarData] public Vector3 Local;
        [StarData] public float Area;
        [StarData] public ShipModuleType Type;
        internal ShipModuleDamageVisualization Visual;
    }

    // what a save keeps of a salvageable derelict: no model, and only the smoke left after the thin-out
    [StarDataType]
    public sealed class SaveData
    {
        [StarData] public IShipDesign Design;
        [StarData] public Empire Owner;
        [StarData] public Vector2 Position;
        [StarData] public Vector2 DriftVelocity;
        [StarData] public Vector3 Rotation;
        [StarData] public Vector3 Spin;
        [StarData] public float Radius;
        [StarData] public float Age;
        [StarData] public int TurnsLeft;
        [StarData] public bool[] KnownBy;
        [StarData] public SmokeSource[] Smoke;
    }

    readonly UniverseState Universe;
    SceneObject So;
    readonly Vector2 MeshOffset;
    readonly Vector2 DriftVelocity;
    readonly Vector3 Spin; // radians per second around the X, Y and Z axes
    readonly SmokeSource[] Smoke;
    readonly float LightsAtDeath;
    readonly AudioEmitter BlastEmitter = new(maxDistance: GameAudio.ShipSfxDistance);
    float NextBlast;

    public readonly IShipDesign Design;
    public readonly Empire Owner;
    public readonly float Radius;
    public readonly float BreakupSeconds;
    public readonly bool Salvageable;
    // by empire id - 1; written on the sim thread, read on the UI thread
    bool[] KnownBy;
    public int TurnsLeft { get; private set; }
    public Vector2 Position { get; private set; }
    public float Z { get; private set; }
    public float Age { get; private set; }
    public float RotationX { get; private set; }
    public float RotationY { get; private set; }
    public float RotationZ { get; private set; }
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
            TurnsLeft = (int)(SalvageableTurns * Universe.ProductionPace);

        if (!seen)
        {
            Position = StopPoint(ship);
            Age = StopSeconds;
            return;
        }
        GameAudio.PlaySfxAsync("sd_explosion_ship_det_small", ship.SoundEmitter);
        SpaceJunk.SpawnJunk(Universe, (int)(Radius * 0.03f), Position, DriftVelocity, ship,
                            maxSize: Radius * 0.1f, ignite: true);
    }

    // from a save: a salvageable derelict with its lights out and no model until the player sees it
    public Derelict(UniverseState universe, SaveData save)
    {
        Universe = universe;
        Design = save.Design.IsAnExistingSavedDesign ? ResourceManager.Ships.GetDesign(save.Design.Name) : save.Design;
        Owner = save.Owner;
        Salvageable = true;
        MeshOffset = Design.BaseHull.MeshOffset;
        Radius = save.Radius;
        Position = save.Position;
        DriftVelocity = save.DriftVelocity;
        RotationX = save.Rotation.X;
        RotationY = save.Rotation.Y;
        RotationZ = save.Rotation.Z;
        Spin = save.Spin;
        Age = save.Age;
        TurnsLeft = save.TurnsLeft;
        KnownBy = save.KnownBy ?? new bool[universe.NumEmpires];
        Smoke = save.Smoke ?? Empty<SmokeSource>.Array;
    }

    public SaveData ToSave()
    {
        var smoke = new SmokeSource[Math.Min(MinSmokeSources, Smoke.Length)];
        for (int i = 0; i < smoke.Length; ++i)
            smoke[i] = new() { Local = Smoke[i].Local, Area = Smoke[i].Area, Type = Smoke[i].Type };
        return new()
        {
            Design = Design,
            Owner = Owner,
            Position = Position,
            DriftVelocity = DriftVelocity,
            Rotation = new(RotationX, RotationY, RotationZ),
            Spin = Spin,
            Radius = Radius,
            Age = Age,
            TurnsLeft = TurnsLeft,
            KnownBy = (bool[])KnownBy.Clone(),
            Smoke = smoke,
        };
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

    public bool IsKnownBy(Empire empire)
    {
        bool[] known = KnownBy;
        int i = empire.Id - 1;
        return i < known.Length && known[i];
    }

    public bool KnownByPlayer => IsKnownBy(Universe.Player);

    void SetKnownBy(Empire empire)
    {
        int i = empire.Id - 1;
        if (i >= KnownBy.Length)
        {
            var grown = new bool[Universe.NumEmpires.LowerBound(i + 1)];
            Array.Copy(KnownBy, grown, KnownBy.Length);
            grown[i] = true;
            KnownBy = grown;
        }
        else
        {
            KnownBy[i] = true;
        }
    }

    // a salvageable one sinks once its turns run out
    public bool Sinking => Salvageable ? TurnsLeft <= 0 : Age > BreakupSeconds;

    public bool CanBeSalvaged => Salvageable && TurnsLeft > 0;

    // once a turn, on the sim thread: its timer runs down, and an empire with a sensor over it learns of it for good
    public void UpdateTurn()
    {
        if (!CanBeSalvaged)
            return;
        --TurnsLeft;
        var empires = Universe.Empires;
        for (int i = 0; i < empires.Count; ++i)
        {
            Empire e = empires[i];
            if (!IsKnownBy(e) && InSensorRangeOf(e))
                SetKnownBy(e);
        }
    }

    bool InSensorRangeOf(Empire empire)
    {
        Empire.InfluenceNode[] nodes = empire.SensorNodes;
        for (int i = 0; i < nodes.Length; ++i)
            if (nodes[i].Position.InRadius(Position, nodes[i].Radius))
                return true;
        return false;
    }

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

    public float Scale => 1f - (1f - ScaleAtRemoval) * (Z / RemoveDepth).Clamped(0f, 1f);

    Matrix Rotation => Matrix.CreateRotationY(RotationY) * Matrix.CreateRotationX(RotationX) * Matrix.CreateRotationZ(RotationZ);

    Matrix World(in Matrix rotation, float scale)
    {
        return Matrix.CreateTranslation(new Vector3(MeshOffset, 0f)) * Matrix.CreateScale(scale)
             * rotation * Matrix.CreateTranslation(new Vector3(Position, Z));
    }

    // on the UI thread: a salvageable one with no model wants it once the player knows of it and it is on screen
    public bool WantsModel => So == null && CanBeSalvaged && KnownByPlayer && Universe.IsSystemViewOrCloser
                              && Universe.Screen.IsInFrustum(new Vector3(Position, Z), Radius);

    // under the list's lock; it keeps the model until it sinks away or the content reloads, so it needs no cleanup like a ship's
    public bool AttachModel(SceneObject so)
    {
        if (So != null || !CanBeSalvaged)
            return false;
        so.World = World(Rotation, Scale);
        so.EmissiveScale = 0f;
        so.Visibility = GlobalStats.ShipVisibility;
        So = so;
        Universe.Screen.AddObject(so);
        return true;
    }

    // false once it has sunk out of sight and left the scene; one with no model is gone once it starts to sink
    public bool Update(FixedSimTime timeStep)
    {
        SceneObject so = So;
        if (so == null && !CanBeSalvaged)
            return false;

        float dt = timeStep.FixedTime;
        Position += VelocityNow * dt;
        Age += dt;
        if (Sinking)
            Z += SinkSpeed * dt;

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
