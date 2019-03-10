using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SgMotion;
using SgMotion.Controllers;
using Ship_Game.Audio;
using Ship_Game.Ships;
using SynapseGaming.LightingSystem.Rendering;

namespace Ship_Game.GameScreens.MainMenu
{
    /**
     * Cool ship with warp-in animation for the MainMenuScreen
     */
    class MainMenuShip
    {
        public readonly Vector3 InitialPosition;
        public readonly Vector3 InitialRotation;
        public Vector3 Position = new Vector3(-50000, 0, 50000f); // out of the screen
        public Vector3 Rotation;
        public Vector3 Scale = Vector3.One;
        float BaseScale;

        // StarDrive Meshes are oriented towards -Y, which is Vector3.Down 
        public Vector3 Forward => Rotation.DegsToRad().RotateVector(Vector3.Down);
        // And the Up is oriented to -Z, which is Vector3.Forward
        public Vector3 Up => Rotation.DegsToRad().RotateVector(Vector3.Forward);
        public Vector3 Right => Rotation.DegsToRad().RotateVector(Vector3.Right);

        readonly MainMenuScreen Screen;
        public readonly MainMenuShipAI AI;
        SceneObject ShipObj;
        AnimationController ShipAnim;

        bool DebugMeshRotate = false;
        bool DebugMeshInspect = false;

        public MainMenuShip(
            MainMenuScreen screen,
            in Vector3 initialPos,
            in Vector3 initialRot,
            IMainMenuShipAI ai)
        {
            Screen = screen;
            InitialPosition = initialPos;
            Rotation = InitialRotation = initialRot;

            AI = ai.GetClone();
            InitRandomShip();
        }

        public void HandleInput(InputState input)
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

                if (input.ScrollIn)  BaseScale += 0.1f;
                if (input.ScrollOut) BaseScale -= 0.1f;

                // if new keypress, spawn random ship
                if (input.KeyPressed(Keys.Space))
                    InitRandomShip();

                if (input.WasAnyKeyPressed)
                {
                    Log.Info($"Position: {Position.String()}  Rotation: {Rotation.String()}");
                }
            }
        }

        public void InitRandomShip()
        {
            if (ShipObj != null) // Allow multiple init
            {
                Screen.RemoveObject(ShipObj);
                ShipObj.Clear();
                ShipObj = null;
                ShipAnim = null;
            }

            // FrostHand: do we actually need to show Model/Ships/speeder/ship07 in base version? Or could show random ship for base and modded version?
            if (GlobalStats.HasMod && ResourceManager.MainMenuShipList.ModelPaths.Count > 0)
            {
                int shipIndex = RandomMath.InRange(ResourceManager.MainMenuShipList.ModelPaths.Count);
                string modelPath = ResourceManager.MainMenuShipList.ModelPaths[shipIndex];
                ShipObj = ResourceManager.GetSceneMesh(Screen.TransientContent, modelPath);
            }
            else if (DebugMeshInspect)
            {
                ShipObj = ResourceManager.GetSceneMesh(Screen.TransientContent, "Model/TestShips/Soyo/Soyo.obj");
                //ShipObj = ResourceManager.GetSceneMesh("Model/TestShips/SciFi-MK6/MK6_OBJ.obj");
            }
            else
            {
                ShipData[] hulls = ResourceManager.Hulls.Filter(s
                    => s.Role == ShipData.RoleName.frigate
                        //|| s.Role == ShipData.RoleName.cruiser
                        //|| s.Role == ShipData.RoleName.capital
                        //&& s.ShipStyle != "Remnant"
                        && s.ShipStyle != "Ralyeh"); // Ralyeh ships look disgusting in the menu
                ShipData hull = hulls[RandomMath.InRange(hulls.Length)];

                ShipObj = ResourceManager.GetSceneMesh(Screen.TransientContent, hull.ModelPath, hull.Animated);
                if (hull.Animated) // Support animated meshes if we use them at all
                {
                    SkinnedModel model = Screen.TransientContent.LoadSkinnedModel(hull.ModelPath);
                    ShipAnim = new AnimationController(model.SkeletonBones);
                    ShipAnim.StartClip(model.AnimationClips["Take 001"]);
                }
            }

            // we want main menu ships to have a certain acceptable size:
            BaseScale = 266f / ShipObj.ObjectBoundingSphere.Radius;
            if (DebugMeshInspect) BaseScale *= 4.0f;

            Screen.AddObject(ShipObj);
            UpdateTransform();
        }

        readonly AudioEmitter SoundEmitter = new AudioEmitter();

        public void PlaySfx(string sfx)
        {
            GameAudio.PlaySfxAsync(sfx, SoundEmitter);
        }

        void UpdateTransform()
        {
            ShipObj.AffineTransform(Position, Rotation.DegsToRad(), Scale*BaseScale);
        }

        public void Update(GameTime gameTime)
        {
            if (DebugMeshInspect)
            {
                Position = Vector3.Zero;
            }
            else
            {
                AI.Update(this, Screen.DeltaTime);
            }

            SoundEmitter.Position = Position;

            // shipObj can be modified while mod is loading
            if (ShipObj != null)
            {
                UpdateTransform();

                // Added by RedFox: support animated ships
                if (ShipAnim != null)
                {
                    ShipObj.SkinBones = ShipAnim.SkinnedBoneTransforms;
                    ShipAnim.Speed = 0.45f;
                    ShipAnim.Update(gameTime.ElapsedGameTime, Matrix.Identity);
                }
            }
        }

        public void Draw(SpriteBatch batch)
        {
            if (DebugMeshRotate || DebugMeshInspect)
            {
                void DrawLine(Vector3 a, Vector3 b, Color color)
                {
                    Vector2 sa = Screen.ProjectTo2D(a);
                    Vector2 sb = Screen.ProjectTo2D(b);
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
