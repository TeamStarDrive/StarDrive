using System;
using Microsoft.Xna.Framework;
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
        abstract class ShipState
        {
            protected readonly MainMenuShip Ship;
            protected readonly float Duration;
            protected float Time;
            protected float RelativeTime => Time / Duration;
            protected float Remaining => Duration - Time;
            protected Func<ShipState> NextState;

            protected ShipState(MainMenuShip ship, float duration)
            {
                Ship = ship;
                Duration = duration;
            }

            // @return TRUE if lifetime transition is over
            public virtual bool Update(float deltaTime)
            {
                Time += deltaTime;
                if (Time >= Duration)
                {
                    Time = Duration;
                    Ship.State = NextState();
                    return true;
                }
                return false;
            }
        }

        class IdlingInDeepSpace : ShipState
        {
            public IdlingInDeepSpace(MainMenuShip ship, float duration) : base(ship, duration)
            {
                ship.Position = new Vector3(-50000, 0, 50000); // out of screen, out of mind
                NextState = () => new WarpingIn(ship);
            }
        }

        class CoastingAcrossScreen : ShipState
        {
            float Speed = 10f; // forward speed in units/s
            bool Spooling;
            bool EnteringFTL;
            public CoastingAcrossScreen(MainMenuShip ship) : base(ship, 4/*RandomMath.RandomBetween(30, 40)*/)
            {
                NextState = () => new WarpingOut(ship);
            }
            public override bool Update(float deltaTime)
            {
                // slow moves the ship across the screen
                Ship.Rotation.Y += deltaTime * 0.06f;
                Ship.Position += deltaTime * Ship.Forward * Speed;
                if (!Spooling && Remaining < 3.0f)
                {
                    GameAudio.PlaySfxAsync("sd_warp_start_large");
                    Spooling = true;
                }
                if (!EnteringFTL && Remaining < 0.2f)
                {
                    FTLManager.AddFTL(Ship.Position, Ship.Forward, 266);
                    EnteringFTL = true;
                }
                return base.Update(deltaTime);
            }
        }

        class WarpingIn : ShipState
        {
            readonly Vector3 Start, End;
            readonly Vector3 WarpScale = new Vector3(1, 4, 1);
            bool ExitingFTL;

            public WarpingIn(MainMenuShip ship) : base(ship, duration: 1f)
            {
                ship.Rotation = new Vector3(-110, 152, -10); // reset direction
                ship.Scale = WarpScale;
                End = new Vector3(-489, -68, 226);
                Start = End - ship.Forward*100000f;
                ship.Position = Start;
                NextState = () => new CoastingAcrossScreen(ship);
            }
            public override bool Update(float deltaTime)
            {
                Ship.Position = Start.LerpTo(End, RelativeTime);

                if (!ExitingFTL && Remaining < 0.1f)
                {
                    FTLManager.AddFTL(End, Ship.Forward, -266);
                    ExitingFTL = true;
                }

                if (base.Update(deltaTime))
                {
                    GameAudio.PlaySfxAsync("sd_warp_stop");
                    Ship.Position = End;
                    Ship.Scale = Vector3.One;
                    return true;
                }
                return false;
            }
        }

        class WarpingOut : ShipState
        {
            readonly Vector3 Start; // far right foreground
            readonly Vector3 End;
            readonly Vector3 WarpScale = new Vector3(1, 4, 1);

            public WarpingOut(MainMenuShip ship) : base(ship, 1f)
            {
                Start = ship.Position;
                End   = ship.Position + ship.Forward * 50000f;
                NextState = () => new IdlingInDeepSpace(ship, 2f);
            }
            public override bool Update(float deltaTime)
            {
                Ship.Position = Start.LerpTo(End, RelativeTime);
                Ship.Scale = WarpScale;
                if (base.Update(deltaTime))
                {
                    Ship.Scale = Vector3.One;
                    return true;
                }
                return false;
            }
        }

        Vector3 Position;
        Vector3 Rotation = new Vector3(-110, 152, -10);
        Vector3 Scale = new Vector3(1, 1, 1);

        // StarDrive Meshes are oriented towards -Y, which is Vector3.Down 
        Vector3 Forward => Rotation.DegsToRad().RotateVector(Vector3.Down);
        // And the Up is oriented to -Z, which is Vector3.Forward
        Vector3 Up => Rotation.DegsToRad().RotateVector(Vector3.Forward);
        Vector3 Right => Rotation.DegsToRad().RotateVector(Vector3.Right);

        float BaseScale = 1.225f;
        ShipState State;

        SceneObject ShipObj;
        AnimationController ShipAnim;

        bool DebugMeshRotate = false;
        bool DebugMeshInspect = false;

        readonly MainMenuScreen Screen;

        public MainMenuShip(MainMenuScreen screen)
        {
            Screen = screen;
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
            State = new IdlingInDeepSpace(this, 1);
            UpdateTransform();

            FTLManager.LoadContent(Screen);
        }

        void UpdateTransform()
        {
            ShipObj.AffineTransform(Position, Rotation.DegsToRad(), Scale*BaseScale);
        }

        public void Update(GameTime gameTime)
        {
            if (DebugMeshInspect)
            {
                Position = new Vector3(0f, 0f, 0f);
            }
            else
            {
                State?.Update(0.016f);
            }
            
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

            FTLManager.Update(Screen, 0.016f);
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

            FTLManager.DrawFTLModels(Screen, batch);
        }
    }
}
