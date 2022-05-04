using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SDGraphics;
using Ship_Game.AI.ShipMovement;
using Ship_Game.GameScreens.MainMenu;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace Ship_Game.GameScreens.Scene
{
    class SetSpawnPos : SceneAction
    {
        public SetSpawnPos() : base(0f)
        {
        }
        public override void Initialize(SceneObj obj)
        {
            obj.Position = obj.Spawn.Position;
            obj.Rotation = obj.Spawn.Rotation;
            base.Initialize(obj);
        }
    }
    class GoToState : SceneAction
    {
        public readonly int State;
        public GoToState(float delay, int state) : base(delay)
        {
            State = state;
        }
        public override bool Update(FixedSimTime timeStep)
        {
            Obj.Position += timeStep.FixedTime * Obj.Forward * Obj.Speed;
            return base.Update(timeStep);
        }
    }

    class IdlingInDeepSpace : SceneAction
    {
        public IdlingInDeepSpace(float duration) : base(duration)
        {
        }
        public override void Initialize(SceneObj obj)
        {
            obj.Position = new Vector3(-50000, 0, 50000); // out of screen, out of mind
            base.Initialize(obj);
        }
    }

    class ForwardCoast : SceneAction
    {
        public ForwardCoast(float duration) : base(duration)
        {
        }
        public override bool Update(FixedSimTime timeStep)
        {
            Obj.Position += timeStep.FixedTime * Obj.Forward * Obj.Speed;
            return base.Update(timeStep);
        }
    }

    // slow moves the object across the screen
    class CoastWithRotate : SceneAction
    {
        Vector3 RadsPerSec;
        public CoastWithRotate(float duration, Vector3 rotRadiansPerSec) : base(duration)
        {
            RadsPerSec = rotRadiansPerSec;
        }
        public override bool Update(FixedSimTime timeStep)
        {
            Obj.Rotation += timeStep.FixedTime * RadsPerSec;
            Obj.Position += timeStep.FixedTime * Obj.Forward * Obj.Speed;
            return base.Update(timeStep);
        }
    }

    // orbits around orbit center towards fixed direction,
    // object can rotate around its own axis while still following the orbit
    class Orbit : SceneAction
    {
        public Vector3 OrbitCenter;
        OrbitPlan.OrbitDirection Direction;
        Vector3 ObjRotPerSec;
        float DistFromCenter;

        /// <param name="duration">Duration of the orbit action</param>
        /// <param name="orbitCenter">Required center of the orbit</param>
        /// <param name="direction">Left(CW) or Right(CCW)?</param>
        /// <param name="objRotPerSec">How much the object rotates around its own axis. Can be ZERO.</param>
        public Orbit(float duration, Vector3 orbitCenter, OrbitPlan.OrbitDirection direction, Vector3 objRotPerSec) : base(duration)
        {
            OrbitCenter = orbitCenter;
            Direction = direction;
            ObjRotPerSec = objRotPerSec;
        }
        public override void Initialize(SceneObj obj)
        {
            base.Initialize(obj);

            DistFromCenter = obj.Position.Distance(OrbitCenter);
            if (DistFromCenter <= 0.1f)
                Log.Warning($"Orbit Action: DistanceFromCenter too small: {DistFromCenter}");
        }
        public override bool Update(FixedSimTime timeStep)
        {
            Vector3 moveDir = (OrbitCenter - Obj.Position).Cross(Vector3.Up).Normalized();
            if (Direction == OrbitPlan.OrbitDirection.Right)
                moveDir = -moveDir;

            // slow moves the ship across the screen
            Obj.Rotation += timeStep.FixedTime * ObjRotPerSec;
            Obj.Position += timeStep.FixedTime * moveDir * Obj.Speed;

            // keep a fixed distance from belt center after the ship has move forward
            Vector3 towardsShip = OrbitCenter.DirectionToTarget(Obj.Position);
            Obj.Position = OrbitCenter + towardsShip * DistFromCenter;

            return base.Update(timeStep);
        }

        public override void Draw()
        {
            if (Obj.DebugTrail)
            {
                Vector2d a = Obj.Scene.Screen.ProjectToScreenPosition(Obj.Position);
                Vector2d b = Obj.Scene.Screen.ProjectToScreenPosition(OrbitCenter);
                Obj.Scene.Screen.DrawLine(a, b, Colors.Cream.Alpha(0.1f));
            }
        }
    }

    class WarpingIn : SceneAction
    {
        Vector3 Start, End;
        readonly Vector3 WarpScale = new Vector3(1, 4, 1);
        bool ExitingFTL;

        public WarpingIn(float duration) : base(duration)
        {
        }
        public override void Initialize(SceneObj obj)
        {
            obj.Rotation = obj.Spawn.Rotation; // reset direction
            obj.Scale = WarpScale;
            End = obj.Spawn.Position;
            Start = End - obj.Forward*100000f;
            obj.Position = Start;
            base.Initialize(obj);
        }
        public override bool Update(FixedSimTime timeStep)
        {
            Obj.Position = Start.LerpTo(End, RelativeTime);

            if (!ExitingFTL && Remaining < 0.15f)
            {
                FTLManager.ExitFTL(() => Obj.Position, Obj.Forward, Obj.HalfLength);
                ExitingFTL = true;
            }

            if (base.Update(timeStep))
            {
                if (!Obj.Spawn.DisableJumpSfx)
                {
                    string cue = Ships.Ship.GetEndWarpCue(Obj.Spawn.Empire, Obj.HullSize);
                    Obj.PlaySfx(cue);
                }
                Obj.Position = End;
                Obj.Scale = Vector3.One;
                return true;
            }
            return false;
        }
    }

    class WarpingOut : SceneAction
    {
        Vector3 Start; // far right foreground
        Vector3 End;
        readonly Vector3 WarpScale = new Vector3(1, 4, 1);
        float SpoolTimer;
        bool EnteringFTL;

        public WarpingOut(float duration) : base(duration)
        {
        }
        public override void Initialize(SceneObj ship)
        {
            Start = ship.Position;
            End   = ship.Position + ship.Forward * 50000f;
            if (!ship.Spawn.DisableJumpSfx)
            {
                string cue = Ships.Ship.GetStartWarpCue(ship.Spawn.Empire, ship.HullSize);
                ship.PlaySfx(cue);
            }
            base.Initialize(ship);
        }
        public override bool Update(FixedSimTime timeStep)
        {
            SpoolTimer += timeStep.FixedTime;
            if (SpoolTimer < 3.2f)
            {
                Obj.Position += timeStep.FixedTime * Obj.Forward * Obj.Speed;

                float remaining = 3.2f - SpoolTimer;
                if (!EnteringFTL && remaining < 0.5f)
                {
                    FTLManager.EnterFTL(Obj.Position, Obj.Forward, Obj.HalfLength);
                    EnteringFTL = true;
                }
                return false;
            }

            // spooling finished, begin warping and updating main timer:
            Obj.Position = Start.LerpTo(End, RelativeTime);
            Obj.Scale = WarpScale;
            if (base.Update(timeStep))
            {
                Obj.Scale = Vector3.One;
                return true;
            }
            return false;
        }
    }
}
