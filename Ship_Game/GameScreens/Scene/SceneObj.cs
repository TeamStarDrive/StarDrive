using System;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using SDGraphics;
using SDGraphics.Input;
using SDUtils;
using Ship_Game.Audio;
using Ship_Game.Data.Mesh;
using Ship_Game.Ships;
using SynapseGaming.LightingSystem.Rendering;
using Vector3 = SDGraphics.Vector3;
using Vector2 = SDGraphics.Vector2;
using Matrix = SDGraphics.Matrix;
using Ship_Game.Utils;

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
        ShipHull Hull; // if this is a ship, for asteroids etc it will be null

        bool DebugMeshRotate = false;
        bool DebugMeshInspect = false; // for debugging mesh loader
        static int LastDebugFrameId;

        public readonly ObjectSpawnInfo Spawn;
        public int HullSize { get; private set; } = 100;

        public SceneInstance Scene;
        public bool EngineTrails;
        public bool DustTrails;
        ParticleEmitter DustEmitter;
        public bool DebugTrail;
        Color ThrustColor1;
        Color EmpireColor;
        Thruster[] Thrusters = Empty<Thruster>.Array;

        public SceneObj(SceneInstance scene, ObjectSpawnInfo spawn)
        {
            Scene = scene;
            Spawn = spawn;
            Position = spawn.Position;
            Rotation = spawn.Rotation;
            Speed = spawn.Speed;
            BaseScale = spawn.Scale;
            AI = spawn.AI.GetClone();

            ThrustColor1 = new Color(spawn.Empire.ThrustColor1R, spawn.Empire.ThrustColor1G, spawn.Empire.ThrustColor1B);
            EmpireColor = spawn.Empire.Traits.Color;
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

        public void Destroy()
        {
            if (SO != null)
            {
                ScreenManager.Instance.RemoveObject(SO);
                SO = null;
            }
        }

        public void LoadContent(GameScreen screen)
        {
            Destroy(); // Allow multiple init

            Hull = null;
            if (DebugMeshInspect)
            {
                StaticMesh mesh = StaticMesh.LoadMesh(screen.ContentManager, "Model/TestShips/Soyo/Soyo.obj");
                SO = mesh!.CreateSceneObject();
                //ShipObj = StaticMesh.GetSceneMesh("Model/TestShips/SciFi-MK6/MK6_OBJ.obj");
            }
            else
            {
                SO = ChooseObject(Spawn.Type, Scene.Random);
                if (SO == null)
                {
                    Hull = ChooseShip(Spawn.Empire, Spawn.Type, Scene.Random);
                    Hull.LoadModel(out SO, screen.ContentManager);
                }
                if (SO.Animation != null)
                {
                    SO.Animation.Speed = 0.25f;
                }
            }

            var bounds = SO.WorldBoundingBox;
            Radius = bounds.Radius();
            HalfLength = (bounds.Max.Y - bounds.Min.Y) * 0.5f;
            HullSize = Hull?.HullSlots.Length ?? (int)(Radius * 4);

            if (DebugMeshInspect)
            {
                Radius *= 4;
                HalfLength *= 4;
            }

            // Phase 3.7: ObjectVisibility defaults to None (enum 0), and the forward
            // renderer skips any SO with Visibility==None (SunBurnStubs.RenderScene).
            // Universe ships/asteroids set this from GlobalStats.AsteroidVisibility/
            // ShipVisibility; MainMenu SceneObjs were missing the same step, so all
            // 1400+ MainMenu asteroids and freighters submitted to ObjectManager
            // were silently culled out of the render pass.
            SO.Visibility = (Hull == null) ? GlobalStats.AsteroidVisibility
                                           : GlobalStats.ShipVisibility;

            ScreenManager.Instance.AddObject(SO);
            Thrusters = Spawn.ThrusterFlashes && Hull != null ? CreateThrusters(Hull) : Empty<Thruster>.Array;
            // Do a first dummy update with deltaTime 0
            // to make sure we have correct position at first frame
            AI.Update(this, FixedSimTime.Zero/*paused during load*/);
            UpdateTransform();
            UpdateThrusters();
        }

        static Thruster[] CreateThrusters(ShipHull hull)
        {
            var thrusters = new Thruster[hull.Thrusters.Length];
            for (int i = 0; i < thrusters.Length; ++i)
            {
                thrusters[i] = new Thruster();
                thrusters[i].LoadAndAssignDefaultEffects(ResourceManager.RootContent);
            }
            return thrusters;
        }

        static SceneObject ChooseObject(string type, RandomBase random)
        {
            if (type == "asteroid")
            {
                int id = random.InRange(ResourceManager.NumAsteroidModels);
                var model = ResourceManager.GetAsteroidModel(id);
                return model.CreateSceneObject();
            }
            if (type == "spacejunk")
            {
                int id = random.InRange(ResourceManager.NumJunkModels);
                var model = ResourceManager.GetJunkModel(id);
                return model.CreateSceneObject();
            }
            return null;
        }

        static ShipHull ChooseShip(IEmpireData empire, string type, RandomBase random)
        {
            string shipType = empire.ShipType;

            ShipHull[] empireShips = ResourceManager.Hulls.Filter(s => s.Style == shipType);
            if (empireShips.Length == 0)
            {
                Log.Error($"Failed to select '{type}' or 'fighter' Hull for '{shipType}'. Choosing a random ship.");
                return random.ItemFilter(ResourceManager.Hulls, s => s.Role.ToString() == type);
            }

            ShipHull[] roleHulls = empireShips.Filter(s => s.Role.ToString() == type);
            if (roleHulls.Length != 0)
            {
                return random.Item(roleHulls);
            }

            ShipHull[] fighters = empireShips.Filter(s => s.Role == RoleName.fighter);
            if (fighters.Length != 0)
            {
                return random.Item(fighters);
            }

            return random.Item(empireShips); // whatever!
        }

        public const float MenuSfxVolume = 0.5f;
        internal readonly AudioEmitter SoundEmitter = new(maxDistance: GameAudio.ShipSfxDistance, volumeScale: MenuSfxVolume);

        public void PlaySfx(string sfx)
        {
            GameAudio.PlaySfxAsync(sfx, SoundEmitter);
        }

        void UpdateTransform()
        {
            SO.AffineTransform(Position, Rotation.DegsToRad(), Scale*BaseScale);
        }

        // hull thrusters are placed around the ship's center, which is MeshOffset away from the mesh origin
        Vector3 ThrusterWorldPos(in ShipHull.ThrusterZone zone)
        {
            var meshPos = new Vector3(zone.Position.X - Hull.MeshOffset.X, zone.Position.Y - Hull.MeshOffset.Y, zone.Position.Z);
            return meshPos.Transform(SO.World);
        }

        void UpdateThrusters()
        {
            if (Thrusters.Length == 0)
                return;

            float heat = AI.ThrusterHeat;
            float tickSpeed = heat > SceneAction.SublightThrusterHeat ? 0.004f : 0.002f;
            Vector3 forward = Forward;
            Vector3 up = -Up; // in-game thrusters take the mesh's +Z as up
            for (int i = 0; i < Thrusters.Length; ++i)
            {
                Thruster thruster = Thrusters[i];
                thruster.WorldPos = ThrusterWorldPos(Hull.Thrusters[i]);
                thruster.Scale = Hull.Thrusters[i].Scale * BaseScale;
                thruster.Update(forward, up, heat, tickSpeed, EmpireColor, EmpireColor);
            }
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
                UpdateThrusters();
            }

            if (DebugTrail)
            {
                Scene.Particles.EngineTrail.AddParticle(Position, Vector3.Zero, BaseScale * 2, Color.WhiteSmoke);
            }
            if (EngineTrails)
            {
                if (Hull != null && SO != null)
                {
                    Vector3 forward = Forward;
                    for (int i = 0; i < Hull.Thrusters.Length; ++i)
                    {
                        Vector3 pos = ThrusterWorldPos(Hull.Thrusters[i]);
                        EngineTrail.Update(Scene.Particles, pos, forward, BaseScale, 1f, ThrustColor1, EmpireColor);
                    }
                }
                else
                {
                    EngineTrail.Update(Scene.Particles, Position, Forward, BaseScale, 1f, ThrustColor1, EmpireColor);
                }
            }
            if (DustTrails)
            {
                if (DustEmitter == null)
                {
                    DustEmitter = Scene.Particles.AsteroidParticles.NewEmitter(1.0f, Position);
                }
                DustEmitter.Update(timeStep.FixedTime, Position, BaseScale, Color.White);
            }
        }

        public void DrawThrusters(ref Matrix view, ref Matrix projection)
        {
            if (AI.ThrusterHeat <= 0f)
                return;
            // drawn twice, as in-game ships draw theirs
            for (int i = 0; i < Thrusters.Length; ++i)
            {
                Thrusters[i].Draw(ref view, ref projection);
                Thrusters[i].Draw(ref view, ref projection);
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

                DrawLine(Position, Position + Right*100, Color.Red);
                DrawLine(Position, Position + Forward*100, Color.Green);
                DrawLine(Position, Position + Up*100, Color.Blue);
            }
            AI.Draw();
        }
    }
}
