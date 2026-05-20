using System;
using SDGraphics;
using SDUtils;
using Ship_Game.Ships;
using Vector2 = SDGraphics.Vector2;
using Vector3d = SDGraphics.Vector3d;

namespace Ship_Game
{
    // Drives the camera when Auto-Cinematic Mode (Shift+F11) is active.
    // Owns a list of shot types; each tick, asks the current shot to Update,
    // and when the shot reports it's done, picks a random valid shot from
    // the list and starts it.
    public class CinematicDirector
    {
        readonly UniverseScreen Universe;
        readonly CinematicShot[] Shots;
        CinematicShot Current;

        public CinematicDirector(UniverseScreen universe)
        {
            Universe = universe;
            Shots = new CinematicShot[]
            {
                new HoldOnTargetShot(universe),
                new FlybyShot(universe),
            };
        }

        public void Reset()
        {
            Current = null;
        }

        public void Update(float elapsedTime)
        {
            if (Current != null && Current.Update(elapsedTime))
                return;

            Current = PickNextShot();
            Current?.Begin();
        }

        CinematicShot PickNextShot()
        {
            var valid = new Array<CinematicShot>();
            for (int i = 0; i < Shots.Length; i++)
                if (Shots[i].IsValid())
                    valid.Add(Shots[i]);
            if (valid.Count == 0)
                return null;
            return valid[Universe.UState.Random.InRange(valid.Count)];
        }
    }

    public abstract class CinematicShot
    {
        protected readonly UniverseScreen Universe;
        protected CinematicShot(UniverseScreen universe) { Universe = universe; }

        // Can this shot start right now? (e.g. FocusCombatShot requires combat ships)
        public abstract bool IsValid();

        // Pick targets / initialize per-shot state. Called once.
        public abstract void Begin();

        // Tick. Return true to stay active, false when the shot is done
        // and the director should pick the next one.
        public abstract bool Update(float elapsedTime);

        // Pick a target from ships within MaxPickRange of the current camera
        // position. Keeps the cinematic focused around what the player was
        // already looking at; the camera doesn't snap across the universe to
        // distant action. Combat ships preferred, falls back to any live ship.
        const float MaxPickRange = 50000f;

        protected static Ship PickCinematicTarget(UniverseScreen us)
        {
            var center = new Vector2((float)us.CamPos.X, (float)us.CamPos.Y);
            float maxSq = MaxPickRange * MaxPickRange;

            var combat = new Array<Ship>();
            foreach (Empire e in us.UState.Empires)
            {
                var ships = e.OwnedShips;
                for (int i = 0; i < ships.Count; i++)
                {
                    Ship s = ships[i];
                    if (s != null && s.Active && s.InCombat
                        && s.Position.SqDist(center) < maxSq)
                        combat.Add(s);
                }
            }
            if (combat.Count > 0)
                return combat[us.UState.Random.InRange(combat.Count)];

            var alive = new Array<Ship>();
            foreach (Empire e in us.UState.Empires)
            {
                var ships = e.OwnedShips;
                for (int i = 0; i < ships.Count; i++)
                {
                    Ship s = ships[i];
                    if (s != null && s.Active
                        && s.Position.SqDist(center) < maxSq)
                        alive.Add(s);
                }
            }
            if (alive.Count > 0)
                return alive[us.UState.Random.InRange(alive.Count)];

            return null;
        }
    }

    // Hover above target at a slight angle. With the 3D LookAt camera the
    // offset makes pitch/yaw visible as the target moves; pure overhead would
    // degenerate the LookAt (forward parallel to world up).
    public class HoldOnTargetShot : CinematicShot
    {
        const float SideOffset    = 1200f;
        const float BackOffset    = 1200f;
        const double Altitude     = 2500.0;
        const double TrackingXY   = 0.08;
        const double TrackingZ    = 0.10;

        Ship Target;
        Vector2 SideDir;
        Vector2 BackDir;
        float Duration;
        float Elapsed;

        public HoldOnTargetShot(UniverseScreen universe) : base(universe) { }

        public override bool IsValid() => PickCinematicTarget(Universe) != null;

        public override void Begin()
        {
            Target = PickCinematicTarget(Universe);
            Duration = Universe.UState.Random.Float(5f, 8f);
            Elapsed = 0f;
            if (Target == null) return;

            Vector2 forward = Target.Direction;
            if (forward.AlmostZero())
                forward = new Vector2(1f, 0f);
            float side = Universe.UState.Random.RollDice(50) ? 1f : -1f;
            SideDir = new Vector2(-forward.Y * side, forward.X * side);
            BackDir = -forward;
        }

        public override bool Update(float elapsedTime)
        {
            Elapsed += elapsedTime;
            if (Target == null || !Target.Active || Elapsed >= Duration)
                return false;

            Vector2 eyeXY = Target.Position + SideDir * SideOffset + BackDir * BackOffset;
            var goal = new Vector3d(eyeXY.X, eyeXY.Y, Altitude);
            var cur = Universe.UState.CamPos;
            Universe.UState.CamPos = new Vector3d(
                cur.X.SmoothStep(goal.X, TrackingXY),
                cur.Y.SmoothStep(goal.Y, TrackingXY),
                cur.Z.SmoothStep(goal.Z, TrackingZ));
            Universe.CinematicLookAt = new Vector3d(Target.Position.X, Target.Position.Y, 0);
            return true;
        }
    }

    // 3D strafing flyby. Camera flies in a straight line parallel to the
    // target's facing, offset laterally so the target is off-center. Always
    // looks at the target -- as the camera passes, the lookAt vector swings
    // around (yaw) and tilts (pitch), giving the cockpit-overtake feel. World
    // up stays world up (no roll) so the horizon doesn't tumble.
    //
    // Layout (top-down view, target facing +F, camera on +S side):
    //
    //                                          PathExit
    //                                              |
    //                                              |   (camera ahead, looks back)
    //                                              |
    //                          [Target]----F-->----+--Alongside (closest pass)
    //                                              |
    //                                              |   (camera behind, looks forward)
    //                                              |
    //                                          PathStart
    public class FlybyShot : CinematicShot
    {
        const float LateralOffset    = 900f;
        const float ApproachDistance = 4500f;
        const float ExitDistance     = 4500f;

        // Eye altitude profile (units above the ship plane). Lower at pass
        // makes the ship dominate the frame; higher at entry/exit gives air.
        const double AltApproach = 1800.0;
        const double AltPass     = 600.0;
        const double AltExit     = 1500.0;

        const float DurApproach = 1.8f;
        const float DurPass     = 2.2f;
        const float DurExit     = 1.8f;
        const float Duration    = DurApproach + DurPass + DurExit;

        Ship Target;
        Vector2 PathStart;
        Vector2 PathAlongside;
        Vector2 PathExit;
        float Elapsed;

        public FlybyShot(UniverseScreen universe) : base(universe) { }

        public override bool IsValid() => PickCinematicTarget(Universe) != null;

        public override void Begin()
        {
            Target = PickCinematicTarget(Universe);
            Elapsed = 0f;
            if (Target == null) return;

            Vector2 forward = Target.Direction;
            if (forward.AlmostZero()) // stationary or unset
                forward = new Vector2(1f, 0f);

            float side = Universe.UState.Random.RollDice(50) ? 1f : -1f;
            var lateral = new Vector2(-forward.Y * side, forward.X * side);

            Vector2 alongside = Target.Position + lateral * LateralOffset;
            PathStart     = alongside - forward * ApproachDistance;
            PathAlongside = alongside;
            PathExit      = alongside + forward * ExitDistance;
        }

        public override bool Update(float elapsedTime)
        {
            Elapsed += elapsedTime;
            if (Target == null || !Target.Active || Elapsed >= Duration)
                return false;

            Vector2 xy;
            double z;
            if (Elapsed < DurApproach)
            {
                float t = Elapsed / DurApproach;
                float eased = Smoothstep(t);
                xy = Lerp(PathStart, PathAlongside, eased);
                z = AltApproach + (AltPass - AltApproach) * eased;
            }
            else if (Elapsed < DurApproach + DurPass)
            {
                float t = (Elapsed - DurApproach) / DurPass;
                Vector2 passMid = PathAlongside + (PathExit - PathAlongside) * 0.5f;
                xy = Lerp(PathAlongside, passMid, t);
                z = AltPass;
            }
            else
            {
                float t = (Elapsed - DurApproach - DurPass) / DurExit;
                float eased = Smoothstep(t);
                Vector2 fromMid = PathAlongside + (PathExit - PathAlongside) * 0.5f;
                xy = Lerp(fromMid, PathExit, eased);
                z = AltPass + (AltExit - AltPass) * eased;
            }

            Universe.UState.CamPos = new Vector3d(xy.X, xy.Y, z);
            Universe.CinematicLookAt = new Vector3d(Target.Position.X, Target.Position.Y, 0);
            return true;
        }

        static float Smoothstep(float t) => t * t * (3f - 2f * t);

        static Vector2 Lerp(in Vector2 a, in Vector2 b, float t)
            => new(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t);
    }
}
