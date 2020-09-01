﻿using System;
using Microsoft.Xna.Framework;

namespace Ship_Game.GameScreens.MainMenu
{
    interface IMainMenuShipAI
    {
        MainMenuShipAI GetClone();
    }

    class MainMenuShipAI : IMainMenuShipAI
    {
        readonly Func<ShipState>[] States;
        ShipState Current;
        int State;
        public bool Finished { get; private set; }

        public MainMenuShipAI(params Func<ShipState>[] states)
        {
            States = states;
        }
        public MainMenuShipAI GetClone()
        {
            return new MainMenuShipAI(States);
        }
        public void Update(MainMenuShip ship, FixedSimTime timeStep)
        {
            if (Finished)
                return;

            if (Current == null)
            {
                Current = States[State]();
                Current.Initialize(ship);
            }

            if (Current.Update(timeStep))
            {
                if (Current is GoToState goTo)
                {
                    State = goTo.State;
                }
                else
                {
                    ++State;
                }
                Current = null;

                // if out of bounds, then we're done
                if (State >= States.Length)
                {
                    Finished = true;
                }
            }
        }
    }

    abstract class ShipState
    {
        protected MainMenuShip Ship;
        protected readonly float Duration;
        protected float Time;
        protected float RelativeTime => Time / Duration;
        protected float Remaining => Duration - Time;

        protected ShipState(float duration)
        {
            Duration = duration;
        }

        public virtual void Initialize(MainMenuShip ship)
        {
            Ship = ship;
            Time = 0f;
        }

        // @return TRUE if lifetime transition is over
        public virtual bool Update(FixedSimTime timeStep)
        {
            Time += timeStep.FixedTime;
            if (Time >= Duration)
            {
                Time = Duration;
                return true;
            }
            return false;
        }
    }

    class GoToState : ShipState
    {
        public readonly int State;
        public GoToState(float delay, int state) : base(delay)
        {
            State = state;
        }
        public override bool Update(FixedSimTime timeStep)
        {
            Ship.Position += timeStep.FixedTime * Ship.Forward * Ship.Speed;
            return base.Update(timeStep);
        }
    }

    class IdlingInDeepSpace : ShipState
    {
        public IdlingInDeepSpace(float duration) : base(duration)
        {
        }
        public override void Initialize(MainMenuShip ship)
        {
            ship.Position = new Vector3(-50000, 0, 50000); // out of screen, out of mind
            base.Initialize(ship);
        }
    }

    class FreighterCoast : ShipState
    {
        public FreighterCoast(float duration) : base(duration)
        {
        }
        public override bool Update(FixedSimTime timeStep)
        {
            Ship.Position += timeStep.FixedTime * Ship.Forward * Ship.Speed;
            return base.Update(timeStep);
        }
    }

    class CoastWithRotate : ShipState
    {
        public CoastWithRotate(float duration) : base(duration)
        {
        }
        public override bool Update(FixedSimTime timeStep)
        {
            // slow moves the ship across the screen
            Ship.Rotation.Y += timeStep.FixedTime * 0.24f;
            Ship.Position += timeStep.FixedTime * Ship.Forward * Ship.Speed;
            return base.Update(timeStep);
        }
    }

    class WarpingIn : ShipState
    {
        Vector3 Start, End;
        readonly Vector3 WarpScale = new Vector3(1, 4, 1);
        bool ExitingFTL;

        public WarpingIn() : base(1f)
        {
        }
        public override void Initialize(MainMenuShip ship)
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
            Ship.Position = Start.LerpTo(End, RelativeTime);

            if (!ExitingFTL && Remaining < 0.15f)
            {
                FTLManager.ExitFTL(() => Ship.Position, Ship.Forward, Ship.HalfLength);
                ExitingFTL = true;
            }

            if (base.Update(timeStep))
            {
                if (!Ship.Spawn.DisableJumpSfx)
                {
                    string cue = Ships.Ship.GetEndWarpCue(Ship.Spawn.Empire, Ship.HullSize);
                    Ship.PlaySfx(cue);
                }
                Ship.Position = End;
                Ship.Scale = Vector3.One;
                return true;
            }
            return false;
        }
    }

    class WarpingOut : ShipState
    {
        Vector3 Start; // far right foreground
        Vector3 End;
        readonly Vector3 WarpScale = new Vector3(1, 4, 1);
        float SpoolTimer;
        bool EnteringFTL;

        public WarpingOut() : base(1f)
        {
        }
        public override void Initialize(MainMenuShip ship)
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
                Ship.Position += timeStep.FixedTime * Ship.Forward * Ship.Speed;

                float remaining = 3.2f - SpoolTimer;
                if (!EnteringFTL && remaining < 0.5f)
                {
                    FTLManager.EnterFTL(Ship.Position, Ship.Forward, Ship.HalfLength);
                    EnteringFTL = true;
                }
                return false;
            }

            // spooling finished, begin warping and updating main timer:
            Ship.Position = Start.LerpTo(End, RelativeTime);
            Ship.Scale = WarpScale;
            if (base.Update(timeStep))
            {
                Ship.Scale = Vector3.One;
                return true;
            }
            return false;
        }
    }

}
