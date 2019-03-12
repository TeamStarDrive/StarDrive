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
        [StarDataKey] public ShipData.RoleName Role = ShipData.RoleName.fighter;
        [StarData] public Vector3 Position;
        [StarData] public float Speed = 10f;
        public IEmpireData Empire;
        public IMainMenuShipAI AI;
        public Vector3 Rotation;
        public bool DisableJumpSfx;
        #pragma warning restore 649
    }

    [StarDataType]
    class ShipGroupInfo
    {
        #pragma warning disable 649
        [StarDataKey] public ShipData.RoleName Role = ShipData.RoleName.fighter;
        [StarData] public int Count = 1;
        #pragma warning restore 649
    }

    class MenuFleet
    {
        #pragma warning disable 649
        [StarDataKey] readonly string Name;
        [StarData] readonly string Empire;
        [StarData] readonly bool DiverseShipEmpires;
        [StarData] readonly Vector3 Rotation;
        [StarData] readonly object[][] AI;
        [StarData] readonly ShipSpawnInfo[] Ships = Empty<ShipSpawnInfo>.Array;
        [StarData] readonly Vector3? MinPos;
        [StarData] readonly Vector3? MaxPos;
        [StarData] readonly Range? SpeedRange;
        [StarData] readonly bool DisableJumpSfx;
        [StarData] readonly ShipGroupInfo[] ShipGroups = Empty<ShipGroupInfo>.Array;
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
                case "CoastWithRotate": return new CoastWithRotate(Duration());
                case "FreighterCoast":  return new FreighterCoast(Duration());
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
            if (!DiverseShipEmpires && Empire.NotEmpty() && Empire != "Random")
            {
                IEmpireData e = ResourceManager.AllRaces.Filter(p => p.Name.Contains(Empire)).FirstOrDefault();
                if (e != null) return e;
            }
            return ResourceManager.MajorRaces.RandItem();
        }

        public Array<MainMenuShip> FleetShips = new Array<MainMenuShip>();

        public void CreateShips()
        {
            foreach (var ship in FleetShips)
                ship.DestroyShip();
            FleetShips.Clear();

            if (Ships.Length == 0 && ShipGroups.Length == 0)
            {
                Log.Warning($"No ships in Main Menu fleet: {Name}");
                return;
            }

            IEmpireData empire = GetEmpire();
            IMainMenuShipAI ai = CreateAI();

            var ships = new Array<ShipSpawnInfo>(Ships);
            foreach (var group in ShipGroups)
            {
                for (int i = 0; i < group.Count; ++i)
                {
                    ships.Add(new ShipSpawnInfo{ Role = group.Role });
                }
            }

            foreach (ShipSpawnInfo spawn in ships)
            {
                spawn.AI = ai;
                spawn.Empire = empire;
                spawn.Rotation = Rotation;
                spawn.DisableJumpSfx = DisableJumpSfx;

                if (MinPos != null && MaxPos != null)
                {
                    spawn.Position = new Vector3(
                        RandomMath.RandomBetween(MinPos.Value.X, MaxPos.Value.X),
                        RandomMath.RandomBetween(MinPos.Value.Y, MaxPos.Value.Y),
                        RandomMath.RandomBetween(MinPos.Value.Z, MaxPos.Value.Z)
                    );
                }
                if (SpeedRange != null)
                {
                    spawn.Speed = SpeedRange.Value.Generate();
                }

                if (DiverseShipEmpires)
                    empire = GetEmpire();

                var ship = new MainMenuShip(spawn);
                FleetShips.Add(ship);
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
