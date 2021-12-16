using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Data;
using Ship_Game.Data.Serialization;
using Ship_Game.Data.Serialization.Types;
using Ship_Game.GameScreens.Scene;
using Ship_Game.Ships;

namespace Ship_Game.GameScreens.Scene
{
    [StarDataType]
    public class SceneGroup
    {
        #pragma warning disable 649
        [StarData] readonly string Name;
        [StarData] readonly string Empire;
        [StarData] readonly bool DiverseShipEmpires;
        [StarData] readonly Vector3 Rotation;
        [StarData] readonly object[][] AI;
        [StarData] readonly Vector3? MinPos;
        [StarData] readonly Vector3? MaxPos;
        [StarData] readonly Range? SpeedRange;
        [StarData] readonly bool DisableJumpSfx;
        [StarData] readonly ObjectSpawnInfo[] Objects = Empty<ObjectSpawnInfo>.Array;
        [StarData] readonly ObjectGroupInfo[] ObjectGroups = Empty<ObjectGroupInfo>.Array;
        #pragma warning restore 649

        SceneAction CreateAction(object[] descriptor)
        {
            string name = descriptor.Length > 0 ? descriptor[0] as string : null;
            if (name == null)
            {
                Log.Error("Expected AI Action ID as first parameter: - [id, arg1, arg2, ...]");
                return new IdlingInDeepSpace(1f);
            }

            object GetArgument(int arg)
            {
                if (arg >= descriptor.Length)
                {
                    Log.Error($"AI Action '{name}' missing argument at index: {arg}");
                    return 0;
                }
                return descriptor[arg];
            }

            Vector3 Vec3(int arg) => Vector3Serializer.ToVector(GetArgument(arg));
            Vector3 RandVec3(int arg) => Vector3Serializer.ToVector(GetArgument(arg));
            float Range(int arg) => RangeSerializer.ToRange(GetArgument(arg)).Generate();
            float Float(int arg) => FloatSerializer.ToFloat(GetArgument(arg));
            int Int(int arg) => (int)Math.Round(FloatSerializer.ToFloat(GetArgument(arg)));

            switch (name)
            {
                case "IdlingInDeepSpace": return new IdlingInDeepSpace(Range(1));
                case "WarpingIn":         return new WarpingIn(Range(1));
                case "WarpingOut":        return new WarpingOut(Range(1));
                case "ForwardCoast":      return new ForwardCoast(Range(1));
                case "CoastWithRotate":   return new CoastWithRotate(Range(1), Vec3(2));
                case "Orbit":             return new Orbit(Range(1), Vec3(2), Vec3(3), RandVec3(4));
                case "GoToState":         return new GoToState(Float(1), Int(2));
                default:
                    Log.Warning($"Unrecognized AI State: '{name}'");
                    return null;
            }
        }

        ISceneShipAI CreateAI()
        {
            var states = new Array<Func<SceneAction>>();

            foreach (object[] state in AI)
            {
                states.Add(() => CreateAction(state));
            }

            return new SceneShipAI(states.ToArray());
        }

        IEmpireData GetEmpire()
        {
            if (!DiverseShipEmpires && Empire.NotEmpty() && Empire != "Random")
            {
                IEmpireData e = ResourceManager.AllRaces.Filter(
                    p => p.Name.Contains(Empire)).FirstOrDefault();
                if (e != null) return e;
            }
            return ResourceManager.MajorRaces.RandItem();
        }

        public Array<SceneObj> FleetShips = new Array<SceneObj>();

        public void CreateShips(GameScreen screen)
        {
            foreach (var ship in FleetShips)
                ship.DestroyShip();
            FleetShips.Clear();

            if (Objects.Length == 0 && ObjectGroups.Length == 0)
            {
                Log.Warning($"No ships in Main Menu fleet: {Name}");
                return;
            }

            IEmpireData empire = GetEmpire();
            ISceneShipAI ai = CreateAI();

            var objects = new Array<ObjectSpawnInfo>(Objects);
            foreach (var group in ObjectGroups)
            {
                for (int i = 0; i < group.Count; ++i)
                {
                    objects.Add(new ObjectSpawnInfo{ Type = group.Type });
                }
            }

            foreach (ObjectSpawnInfo spawn in objects)
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

                var ship = new SceneObj(spawn);
                ship.LoadContent(screen);
                FleetShips.Add(ship);
            }
        }

        public void Update(GameScreen screen, FixedSimTime timeStep)
        {
            foreach (var ship in FleetShips)
                ship.Update(timeStep);
            
            // if all ship AI's have finished, create a new one
            FleetShips.RemoveAll(ship => ship.AI.Finished);
            if (FleetShips.IsEmpty)
                CreateShips(screen);
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
