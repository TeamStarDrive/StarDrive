using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Ship_Game.Audio;
using Ship_Game.Data.Mesh;
using Ship_Game.GameScreens.MainMenu;
using Ship_Game.Ships;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game.GameScreens.Scene
{
    /**
     * A sort of generic scene object for Scenes
     */
    public class SceneObj
    {
        public Vector3 Position;
        public Vector3 Rotation;
        public Vector3 Scale = Vector3.One;
        public float Radius { get; private set; }
        public float HalfLength { get; private set; }
        public float Speed;
        float BaseScale;

        // StarDrive Meshes are oriented towards -Y, which is Vector3.Down 
        public Vector3 Forward => Rotation.DegsToRad().RotateVector(Vector3.Down);
        // And the Up is oriented to -Z, which is Vector3.Forward
        public Vector3 Up => Rotation.DegsToRad().RotateVector(Vector3.Forward);
        public Vector3 Right => Rotation.DegsToRad().RotateVector(Vector3.Right);

        public readonly SceneShipAI AI;
        SceneObject SO;

        bool DebugMeshRotate = false;
        bool DebugMeshInspect = false; // for debugging mesh loader
        static int LastDebugFrameId;

        public readonly ObjectSpawnInfo Spawn;
        public int HullSize { get; private set; } = 100;

        public SceneObj(ObjectSpawnInfo spawn)
        {
            Spawn = spawn;
            Position = spawn.Position;
            Rotation = spawn.Rotation;
            Speed = spawn.Speed;
            BaseScale = spawn.Scale;
            AI = spawn.AI.GetClone();
        }

        public void AxisRotate(float ax, float ay, float az)
        {
            AxisRotate(new Vector3(ax, ay, az));
        }

        public void AxisRotate(in Vector3 localRotation)
        {
            Vector3 dir = localRotation.DegsToRad().RotateVector(Forward);
            Rotation = dir.ToEulerAngles2();
        }

        public void HandleInput(InputState input, GameScreen screen)
        {
            // Use these controls to reorient the ship and planet in the menu. The new rotation
            // is logged into debug console and can be set as default values later
            if (DebugMeshInspect || DebugMeshRotate)
            {
                if (input.IsKeyDown(Keys.W)) Position += Forward * 10;
                if (input.IsKeyDown(Keys.S)) Position -= Forward * 10;
                if (input.IsKeyDown(Keys.A)) Position -= Forward.RightVector(Up) * 10;
                if (input.IsKeyDown(Keys.D)) Position += Forward.RightVector(Up) * 10;
                if (input.IsKeyDown(Keys.Z)) Position += Forward.UpVector(Up) * 10;
                if (input.IsKeyDown(Keys.X)) Position -= Forward.UpVector(Up) * 10;

                if (input.IsKeyDown(Keys.Up))    Rotation.X += 1; // Pitch craft Up/Down
                if (input.IsKeyDown(Keys.Down))  Rotation.X -= 1;
                if (input.IsKeyDown(Keys.Left))  Rotation.Y -= 1; // Yaw craft Left/Right 
                if (input.IsKeyDown(Keys.Right)) Rotation.Y += 1;
                if (input.IsKeyDown(Keys.Q))     Rotation.Z -= 1; // Roll craft
                if (input.IsKeyDown(Keys.E))     Rotation.Z += 1;

                
                if (input.IsKeyDown(Keys.NumPad8)) AxisRotate(+1,0,0); // Pitch craft Up/Down
                if (input.IsKeyDown(Keys.NumPad2)) AxisRotate(-1,0,0);
                if (input.IsKeyDown(Keys.NumPad4)) AxisRotate(0,+1,0); // Yaw craft Left/Right 
                if (input.IsKeyDown(Keys.NumPad6)) AxisRotate(0,-1,0);
                if (input.IsKeyDown(Keys.NumPad7)) AxisRotate(0,0,+1); // Roll craft
                if (input.IsKeyDown(Keys.NumPad9)) AxisRotate(0,0,-1);


                if (input.ScrollIn)  BaseScale += 0.1f;
                if (input.ScrollOut) BaseScale -= 0.1f;

                // if new keypress, spawn random ship
                if (input.KeyPressed(Keys.Space))
                    LoadContent(screen);

                if (input.WasAnyKeyPressed)
                {
                    if (LastDebugFrameId != StarDriveGame.Instance.FrameId)
                    {
                        Log.Info($"Position: {Position.String()}  Rotation: {Rotation.String()}");
                    }
                    LastDebugFrameId = StarDriveGame.Instance.FrameId;
                }
            }
        }

        public void DestroyShip()
        {
            if (SO != null)
            {
                ScreenManager.Instance.RemoveObject(SO);
                SO = null;
            }
        }

        public void LoadContent(GameScreen screen)
        {
            DestroyShip(); // Allow multiple init

            ShipHull hull = null;
            if (GlobalStats.HasMod && ResourceManager.MainMenuShipList.ModelPaths.Count > 0)
            {
                int shipIndex = RandomMath.InRange(ResourceManager.MainMenuShipList.ModelPaths.Count);
                string modelPath = ResourceManager.MainMenuShipList.ModelPaths[shipIndex];
                SO = StaticMesh.GetSceneMesh(screen.ContentManager, modelPath);
            }
            else if (DebugMeshInspect)
            {
                SO = StaticMesh.GetSceneMesh(screen.ContentManager, "Model/TestShips/Soyo/Soyo.obj");
                //ShipObj = StaticMesh.GetSceneMesh("Model/TestShips/SciFi-MK6/MK6_OBJ.obj");
            }
            else
            {
                SO = ChooseObject(Spawn.Type);
                if (SO == null)
                {
                    hull = ChooseShip(Spawn.Empire, Spawn.Type);
                    hull.LoadModel(out SO, screen.ContentManager);
                }
                if (SO.Animation != null)
                {
                    SO.Animation.Speed = 0.25f;
                }
            }

            var bounds = SO.GetMeshBoundingBox();
            Radius = bounds.Radius();
            HalfLength = (bounds.Max.Y - bounds.Min.Y) * 0.5f;
            HullSize = hull?.HullSlots.Length ?? (int)(Radius * 4);

            if (DebugMeshInspect)
            {
                Radius *= 4;
                HalfLength *= 4;
            }

            ScreenManager.Instance.AddObject(SO);
            // Do a first dummy update with deltaTime 0
            // to make sure we have correct position at first frame
            AI.Update(this, FixedSimTime.Zero/*paused during load*/);
            UpdateTransform();
        }

        static SceneObject ChooseObject(string type)
        {
            if (type == "asteroid")
            {
                int id = RandomMath2.InRange(ResourceManager.NumAsteroidModels);
                return new SceneObject(ResourceManager.GetAsteroidModel(id).Meshes[0]) { ObjectType = ObjectType.Dynamic };
            }
            if (type == "spacejunk")
            {
                int id = RandomMath2.InRange(ResourceManager.NumJunkModels);
                return new SceneObject(ResourceManager.GetJunkModel(id).Meshes[0]) { ObjectType = ObjectType.Dynamic };
            }
            return null;
        }

        static ShipHull ChooseShip(IEmpireData empire, string type)
        {
            string shipType = empire.ShipType;

            ShipHull[] empireShips = ResourceManager.Hulls.Filter(s => s.Style == shipType);
            if (empireShips.Length == 0)
            {
                Log.Error($"Failed to select '{type}' or 'fighter' Hull for '{shipType}'. Choosing a random ship.");
                return ResourceManager.Hulls.Filter(s => s.Role.ToString() == type).RandItem();
            }

            ShipHull[] roleHulls = empireShips.Filter(s => s.Role.ToString() == type);
            if (roleHulls.Length != 0)
            {
                return roleHulls.RandItem();
            }

            ShipHull[] fighters = empireShips.Filter(s => s.Role == RoleName.fighter);
            if (fighters.Length != 0)
            {
                return fighters.RandItem();
            }

            return empireShips.RandItem(); // whatever!
        }

        readonly AudioEmitter SoundEmitter = new AudioEmitter();

        public void PlaySfx(string sfx)
        {
            GameAudio.PlaySfxAsync(sfx, SoundEmitter);
        }

        void UpdateTransform()
        {
            SO.AffineTransform(Position, Rotation.DegsToRad(), Scale*BaseScale);
        }

        public void Update(FixedSimTime timeStep)
        {
            if (DebugMeshInspect)
            {
                Position = Vector3.Zero;
            }
            else
            {
                AI.Update(this, timeStep);
            }

            SoundEmitter.Position = Position;

            // shipObj can be modified while mod is loading
            if (SO != null)
            {
                UpdateTransform();
                SO.UpdateAnimation(timeStep.FixedTime);
            }
        }

        public void Draw(SpriteBatch batch, GameScreen screen)
        {
            if (DebugMeshRotate || DebugMeshInspect)
            {
                void DrawLine(Vector3 a, Vector3 b, Color color)
                {
                    Vector2d sa = screen.ProjectToScreenPosition(a);
                    Vector2d sb = screen.ProjectToScreenPosition(b);
                    batch.DrawLine(sa, sb, color, 2f);
                }

                batch.Begin();
                DrawLine(Position, Position + Right*100, Color.Red);
                DrawLine(Position, Position + Forward*100, Color.Green);
                DrawLine(Position, Position + Up*100, Color.Blue);
                batch.End();
            }
        }
    }
}
