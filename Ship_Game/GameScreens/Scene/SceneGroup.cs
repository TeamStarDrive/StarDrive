using System;
using Microsoft.Xna.Framework.Graphics;
using SDUtils;
using Ship_Game.AI.ShipMovement;
using Ship_Game.Data;
using Ship_Game.Data.Serialization;
using Ship_Game.Data.Serialization.Types;
using Ship_Game.ExtensionMethods;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;

namespace Ship_Game.GameScreens.Scene
{
    [StarDataType]
    public class SceneGroup
    {
        #pragma warning disable 649
        [StarData] readonly string Name;
        [StarData] readonly string Empire;
        [StarData] readonly bool DiverseShipEmpires;
        [StarData] readonly object[][] AI;
        [StarData] readonly bool DisableJumpSfx;
        [StarData] readonly bool EngineTrails;
        [StarData] readonly bool DustTrails;
        [StarData] readonly bool DebugTrail;
        [StarData] readonly ObjectSpawnInfo[] Objects = Empty<ObjectSpawnInfo>.Array;
        [StarData] readonly ObjectGroupInfo[] ObjectGroups = Empty<ObjectGroupInfo>.Array;
        #pragma warning restore 649

        public Array<SceneObj> AllObjects = new Array<SceneObj>();
        public SceneInstance Scene;

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
            Vector3 RandVec3(int arg) => Scene.Random.Vector3D(Vector3Serializer.ToVector(GetArgument(arg)));
            float Range(int arg) => RangeSerializer.ToRange(GetArgument(arg)).Generate();
            float Float(int arg) => FloatSerializer.ToFloat(GetArgument(arg));
            int Int(int arg) => (int)Math.Round(FloatSerializer.ToFloat(GetArgument(arg)));
            string Str(int arg) => GetArgument(arg) as string ?? "";

            switch (name)
            {
                case "IdlingInDeepSpace": return new IdlingInDeepSpace(Range(1));
                case "WarpingIn":         return new WarpingIn(Range(1));
                case "WarpingOut":        return new WarpingOut(Range(1));
                case "ForwardCoast":      return new ForwardCoast(Range(1));
                case "CoastWithRotate":   return new CoastWithRotate(Range(1), Vec3(2));
                case "Orbit":             return new Orbit(Range(1), Vec3(2), (Str(3).ToLower() == "right" ? OrbitPlan.OrbitDirection.Right : OrbitPlan.OrbitDirection.Left), RandVec3(4));
                case "GoToState":         return new GoToState(Float(1), Int(2));
                case "SetSpawnPos":       return new SetSpawnPos();
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

        EmpireData GetEmpire()
        {
            if (DiverseShipEmpires)
                return Scene.GetEmpire("Random");
            return Scene.GetEmpire(Empire);
        }

        public void CreateShips(SceneInstance scene, GameScreen screen)
        {
            Scene = scene;
            foreach (var obj in AllObjects)
                obj.Destroy();
            AllObjects.Clear();

            if (Objects.Length == 0 && ObjectGroups.Length == 0)
            {
                Log.Warning($"No ships in Main Menu fleet: {Name}");
                return;
            }

            EmpireData empire = GetEmpire();
            ISceneShipAI ai = CreateAI();

            var objects = new Array<ObjectSpawnInfo>(Objects);
            foreach (ObjectGroupInfo group in ObjectGroups)
            {
                for (int i = 0; i < group.Count; ++i)
                {
                    var spawn = new ObjectSpawnInfo
                    {
                        Type = group.Type,
                        Speed = group.Speed.Generate(),
                        Scale = group.Scale.Generate()
                    };
                    if (group.MinPos != null && group.MaxPos != null)
                        spawn.Position = Scene.Random.Vector3D(group.MinPos.Value, group.MaxPos.Value);
                    if (group.Orbit != null)
                        spawn.Position = GenerateOrbitPos(ai, group.Orbit.Value, group.Offset);
                    if (group.Rotation != null)
                        spawn.Rotation = group.Rotation.Value;
                    if (group.RandRot != null)
                        spawn.Rotation = Scene.Random.Vector3D(group.RandRot.Value);
                    objects.Add(spawn);
                }
            }

            foreach (ObjectSpawnInfo spawn in objects)
            {
                spawn.AI = ai;
                spawn.Empire = empire;
                spawn.DisableJumpSfx = DisableJumpSfx;

                if (DiverseShipEmpires)
                    empire = GetEmpire();

                var obj = new SceneObj(scene, spawn);
                obj.LoadContent(screen);
                obj.EngineTrails = EngineTrails;
                obj.DustTrails = DustTrails;
                obj.DebugTrail = DebugTrail;
                AllObjects.Add(obj);
            }
        }

        Orbit OrbitOrder;
        Orbit FindOrbitOrder(ISceneShipAI ai)
        {
            if (OrbitOrder != null)
                return OrbitOrder;
            for (int i = 0; i < ai.States.Length; ++i)
                if (ai.States[i]() is Orbit o)
                    return o;
            Log.Error($"Failed to find required Orbit order! Group: {Name}");
            return null;
        }

        Vector3 GenerateOrbitPos(ISceneShipAI ai, Vector3 orbit, Vector3 offset)
        {
            OrbitOrder = FindOrbitOrder(ai);
            if (OrbitOrder == null)
                return Vector3.Zero;

            float radius = orbit.X;
            float angle = Scene.Random.Float(orbit.Y, orbit.Z);
            Vector3 randDispersion = Scene.Random.Vector3D(offset);

            Vector3 orbitCenter = OrbitOrder.OrbitCenter;
            Vector2 pos = orbitCenter.ToVec2().PointFromAngle(angle, radius);
            return orbitCenter + new Vector3(pos.X, orbitCenter.Y, pos.Y) + randDispersion;
        }

        public void Update(GameScreen screen, FixedSimTime timeStep)
        {
            foreach (var ship in AllObjects)
                ship.Update(timeStep);

            // if all ship AI's have finished, create a new one
            AllObjects.RemoveAll(ship => ship.AI.Finished);
            if (AllObjects.IsEmpty)
                CreateShips(Scene, screen);
        }

        public void HandleInput(InputState input, GameScreen screen)
        {
            foreach (var ship in AllObjects)
                ship.HandleInput(input, screen);
        }

        public void Draw(SpriteBatch batch, GameScreen screen)
        {
            foreach (var ship in AllObjects)
                ship.Draw(batch, screen);
        }
    }
}
