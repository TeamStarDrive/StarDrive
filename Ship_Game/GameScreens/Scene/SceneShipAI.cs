using System;
using Microsoft.Xna.Framework;
using Ship_Game.GameScreens.MainMenu;

namespace Ship_Game.GameScreens.Scene
{
    public interface ISceneShipAI
    {
        SceneShipAI GetClone();
    }

    public class SceneShipAI : ISceneShipAI
    {
        readonly Func<SceneAction>[] States;
        SceneAction Current;
        int State;
        public bool Finished { get; private set; }

        public SceneShipAI(params Func<SceneAction>[] states)
        {
            States = states;
        }
        public SceneShipAI GetClone()
        {
            return new SceneShipAI(States);
        }
        public void Update(SceneShip ship, FixedSimTime timeStep)
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
}
