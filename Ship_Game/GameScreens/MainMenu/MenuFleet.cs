using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data;
using Ship_Game.Ships;

namespace Ship_Game.GameScreens.MainMenu
{
    [StarDataType]
    class ShipSpawnInfo
    {
        #pragma warning disable 649
        [StarDataKey] public readonly ShipData.RoleName Role = ShipData.RoleName.fighter;
        [StarData] public readonly Vector3 Position;
        [StarData] public readonly float Speed = 10f;
        [StarData] public Vector3 AxisRotation;
        public IEmpireData Empire;
        public IMainMenuShipAI AI;
        public Vector3 Rotation;
        #pragma warning restore 649
    }

    class MenuFleet
    {
        #pragma warning disable 649
        [StarDataKey] readonly string Name;
        [StarData] readonly string Empire;
        [StarData] readonly Vector3 Rotation;
        [StarData] readonly object[][] AI;
        [StarData] readonly ShipSpawnInfo[] Ships;
        #pragma warning restore 649

        readonly FloatConverter Floater = new FloatConverter();

        ShipState CreateState(object[] descriptor)
        {
            string name = descriptor[0] as string;
            float a = 0f, b = 0f;
            if (descriptor.Length > 0) a = (float)Floater.Convert(descriptor[1]);
            if (descriptor.Length > 1) b = (float)Floater.Convert(descriptor[2]);

            float Duration() => RandomMath.RandomBetween(a, b);

            switch (name)
            {
                case "IdlingInDeepSpace": return new IdlingInDeepSpace(Duration());
                case "WarpingIn":       return new WarpingIn();
                case "WarpingOut":      return new WarpingOut();
                case "CoastingForward": return new CoastingForward(Duration());
                case "GoToState":       return new GoToState(a, (int)Math.Round(b));
                default:
                    Log.Warning($"Unrecognized AI State: '{name}'");
                    return null;
            }
        }

        IMainMenuShipAI CreateAI()
        {
            var states = new Array<Func<ShipState>>();

            foreach (object[] state in AI)
            {
                states.Add(() => CreateState(state));
            }

            return new MainMenuShipAI(states.ToArray());
        }

        IEmpireData GetEmpire()
        {
            if (Empire.NotEmpty() && Empire != "Random")
            {
                IEmpireData e = ResourceManager.AllRaces.Filter(p => p.Name.Contains(Empire)).FirstOrDefault();
                if (e != null) return e;
            }
            return ResourceManager.MajorRaces.RandItem();
        }

        public Array<MainMenuShip> FleetShips = new Array<MainMenuShip>();

        public void CreateShips()
        {
            FleetShips.Clear();
            if (Ships == null || Ships.Length == 0)
            {
                Log.Warning($"No ships in Main Menu fleet: {Name}");
                return;
            }

            IEmpireData empire = GetEmpire();
            IMainMenuShipAI ai = CreateAI();

            foreach (ShipSpawnInfo spawn in Ships)
            {
                spawn.AI = ai;
                spawn.Empire = empire;
                spawn.Rotation = Rotation;

                var ship = new MainMenuShip(spawn);
                FleetShips.Add(ship);

                if (spawn.AxisRotation.NotZero())
                {
                    ship.AxisRotate(spawn.AxisRotation);
                }
            }
        }

        public void Update(GameTime gameTime, GameScreen screen)
        {
            foreach (var ship in FleetShips)
                ship.Update(gameTime, screen);
            
            // if all ship AI's have finished, create a new one
            FleetShips.RemoveAllIf(ship => ship.AI.Finished);
            if (FleetShips.IsEmpty)
                CreateShips();
        }

        public void HandleInput(InputState input, GameScreen screen)
        {
            foreach (var ship in FleetShips)
                ship.HandleInput(input, screen);
        }

        public void Draw(SpriteBatch batch, GameScreen screen)
        {
            foreach (var ship in FleetShips)
                ship.Draw(batch, screen);
        }
    }
}
