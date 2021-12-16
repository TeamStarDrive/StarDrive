using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Ship_Game.GameScreens.MainMenu;

namespace Ship_Game.GameScreens.Scene
{
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
        public override void Initialize(SceneObj ship)
        {
            ship.Position = new Vector3(-50000, 0, 50000); // out of screen, out of mind
            base.Initialize(ship);
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
        Vector3 OrbitCenter;
        Vector3 MoveDirection;
        Vector3 ObjRotPerSec;
        float DistFromCenter;

        /// <param name="orbitCenter">Required center of the orbit</param>
        /// <param name="moveDir">Direction of movement along the orbit</param>
        /// <param name="objRotPerSec">How much the object rotates around its own axis. Can be ZERO.</param>
        public Orbit(float duration, Vector3 orbitCenter, Vector3 moveDir, Vector3 objRotPerSec) : base(duration)
        {
            OrbitCenter = orbitCenter;
            MoveDirection = moveDir;
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
            // slow moves the ship across the screen
            Obj.Rotation += timeStep.FixedTime * ObjRotPerSec;
            Obj.Position += timeStep.FixedTime * MoveDirection * Obj.Speed;

            // keep a fixed distance from belt center after the ship has move forward
            Vector3 towardsShip = OrbitCenter.DirectionToTarget(Obj.Position);
            Obj.Position = OrbitCenter + towardsShip * DistFromCenter;

            return base.Update(timeStep);
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
        public override void Initialize(SceneObj ship)
        {
            ship.Rotation = ship.Spawn.Rotation; // reset direction
            ship.Scale = WarpScale;
            End = ship.Spawn.Position;
            Start = End - ship.Forward*100000f;
            ship.Position = Start;
            base.Initialize(ship);
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
