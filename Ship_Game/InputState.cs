using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Ship_Game
{
    public sealed class InputState
    {
        public KeyboardState KeysCurr;
        public KeyboardState KeysPrev;
        public GamePadState GamepadCurr;
        public GamePadState GamepadPrev;
        public MouseState MouseCurr;
        public MouseState MousePrev;
        public int ScrollWheelPrev;

        public bool Repeat;
        //MouseDrag variables
        public Vector2 StartDrag { get; private set; }
        public Vector2 StartDragWorld { get; private set; }
        public Vector2 ProjectedPosition { get; private set; }
        public bool MouseDragging { get; private set; }
        public Vector2 EndDrag { get; private set; }
        public Vector2 EndDragWorld { get; private set; }
        public Vector2 SelectionPoint { get; private set; }
        public float SelectionRadiansToTarget { get; private set; }
        public Vector2 SelectionFacing { get; private set; }
        


        //Mouse Timers
        private float RightMouseDownTime;
        private float LeftMouseDownTime;

        //Mouse Clicks
        public bool RightMouseClick    => MouseButtonClicked(MouseCurr.RightButton, MousePrev.RightButton);
        public bool LeftMouseClick     => MouseButtonClicked(MouseCurr.LeftButton, MousePrev.LeftButton);
        public bool BackMouseClick     => MouseButtonClicked(MouseCurr.XButton1, MousePrev.XButton1);
        public bool ForwardMouseClick  => MouseButtonClicked(MouseCurr.XButton2, MousePrev.XButton2);
        public bool MiddleMouseClick   => MouseButtonClicked(MouseCurr.MiddleButton, MousePrev.MiddleButton);
        public bool LeftMouseReleased  => MouseButtonReleased(MouseCurr.LeftButton, MousePrev.LeftButton);
        public bool RightMouseReleased => MouseButtonReleased(MouseCurr.RightButton, MousePrev.RightButton);
        public bool LeftMouseDown      => MouseCurr.LeftButton  == ButtonState.Pressed;
        public bool RightMouseDown     => MouseCurr.RightButton == ButtonState.Pressed;
        public bool RightMouseHeldUp => MouseCurr.RightButton != ButtonState.Pressed || MousePrev.RightButton != ButtonState.Pressed;
        public Vector2 MouseScreenPos  => new Vector2(MouseCurr.X, MouseCurr.Y);

        public bool LeftMouseHeld(float seconds = 0.25f)
        {
            return MouseButtonHeld(MouseCurr.LeftButton, MousePrev.LeftButton, seconds, LeftMouseDownTime);
        }
        public bool RightMouseHeld(float seconds = 0.25f)
        {
            return MouseButtonHeld(MouseCurr.RightButton, MousePrev.RightButton, seconds, RightMouseDownTime);
        }

        private static bool MouseButtonHeld(ButtonState current, ButtonState prev, float seconds, float timer)
        {
            return current == ButtonState.Pressed && prev == ButtonState.Pressed && timer >= seconds;
        }
        private static bool MouseButtonClicked(ButtonState current, ButtonState prev)
        {
            return current == ButtonState.Pressed && prev == ButtonState.Released;
        }
        private static bool MouseButtonReleased(ButtonState current, ButtonState prev)
        {
            return current == ButtonState.Released && prev == ButtonState.Pressed;
        }

        public bool RightMouseDrag(Matrix view, Matrix projection, Viewport viewport)
        {

            if (!RightMouseHeld(.05f))
            {
                if (!MouseDragging) return false;

                Vector3 position = viewport.Unproject(
                    CursorPosition.ToVec3(), projection, view,
                    Matrix.Identity);

                Vector3 direction = viewport.Unproject(
                                        CursorPosition.ToVec3(1f),
                                        projection, view, Matrix.Identity) - position;
                direction.Normalize();
                Ray ray = new Ray(position, direction);
                float unknownRayModifer = -ray.Position.Z / ray.Direction.Z;
                Vector3 vector3 = new Vector3(ray.Position.X + unknownRayModifer * ray.Direction.X,
                    ray.Position.Y + unknownRayModifer * ray.Direction.Y, 0.0f);
                SelectionPoint = vector3.ToVec2();
                Vector2 target = CursorPosition;
                SelectionRadiansToTarget = StartDrag.RadiansToTarget(target);
                SelectionFacing = Vector2.Normalize(target - StartDrag);

                MouseDragging = false;
                return true;

            }
            if (MouseDragging)
            {
                EndDrag = CursorPosition;
                EndDragWorld = Empire.Universe.UnprojectToWorldPosition(StartDrag);
                return true; 
            }
            StartDrag = CursorPosition;
            StartDragWorld = Empire.Universe.UnprojectToWorldPosition(StartDrag);
            ProjectedPosition = StartDragWorld;
            MouseDragging = true;
            return true;

        }

        public bool IsKeyDown(Keys key) => KeysCurr.IsKeyDown(key);

        private bool KeyPressed(Keys key)
        {
            return Repeat ? KeysCurr.IsKeyDown(key) : WasKeyPressed(key);
        }
        public bool WasKeyPressed(Keys key)
        {
            return KeysCurr.IsKeyDown(key) && KeysPrev.IsKeyUp(key);
        }

        public bool GamepadClicked(Buttons button)
        {
            return GamepadCurr.IsButtonDown(button) && GamepadPrev.IsButtonUp(button);
        }
        public bool GamepadHeld(Buttons button) => GamepadCurr.IsButtonDown(button);


        public bool LeftStickFlickDown => GamepadCurr.ThumbSticks.Left.Y < 0f && GamepadPrev.ThumbSticks.Left.Y >= 0f;
        public bool LeftStickFlickUp   => GamepadCurr.ThumbSticks.Left.Y > 0f && GamepadPrev.ThumbSticks.Left.Y <= 0f;




        //Ingame 
        //UniverseScreen
        public bool PauseGame            => KeyPressed(Keys.Space);
        public bool UseRealLights        => KeyPressed(Keys.F5);
        public bool ShowExceptionTracker => KeyPressed(Keys.F6);
        public bool SendKudos            => KeyPressed(Keys.F7);
        public bool SpeedUp              => KeyPressed(Keys.OemPlus) || KeyPressed(Keys.Add);
        public bool SpeedDown            => KeyPressed(Keys.OemMinus) || KeyPressed(Keys.Subtract);
        public bool ScrapShip            => KeyPressed(Keys.Back) || KeyPressed(Keys.Delete);
        public bool ZoomToShip           => KeyPressed(Keys.PageUp);
        public bool ZoomOut              => KeyPressed(Keys.PageDown);
        public bool DeepSpaceBuildWindow => KeyPressed(Keys.B);
        public bool PlanetListScreen     => KeyPressed(Keys.L);
        public bool FTLOverlay           => KeyPressed(Keys.F1);
        public bool RangeOverlay         => KeyPressed(Keys.F2);
        public bool ShipListScreen       => KeyPressed(Keys.K);
        public bool FleetDesignScreen    => KeyPressed(Keys.J);
        public bool AutomationWindow     => KeyPressed(Keys.H);
        public bool Fleet1               => KeyPressed(Keys.D1);
        public bool Fleet2               => KeyPressed(Keys.D2);
        public bool Fleet3               => KeyPressed(Keys.D3);
        public bool Fleet4               => KeyPressed(Keys.D4);
        public bool Fleet5               => KeyPressed(Keys.D5);
        public bool Fleet6               => KeyPressed(Keys.D6);
        public bool Fleet7               => KeyPressed(Keys.D7);
        public bool Fleet8               => KeyPressed(Keys.D8);
        public bool Fleet9               => KeyPressed(Keys.D9);
        public bool AddToFleet           => IsKeyDown(Keys.LeftControl) && !IsKeyDown(Keys.LeftShift);
        public bool ReplaceFleet         => IsKeyDown(Keys.LeftControl) && IsKeyDown(Keys.LeftShift);

        //IngameWiki
        public bool ExitWiki => KeyPressed(Keys.P) && !GlobalStats.TakingInput;

        //debug
        public bool DebugMode            => LeftCtrlShift && (KeyPressed(Keys.OemTilde) || KeyPressed(Keys.Tab));
        public bool GetMemory            => KeyPressed(Keys.G);
        public bool ShowDebugWindow      => KeyPressed(Keys.H);  
        public bool EmpireToggle         => IsKeyDown(Keys.LeftShift);
        public bool SpawnShip            => KeyPressed(Keys.C);
        public bool SpawnFleet1          => KeyPressed(Keys.Z);
        public bool SpawnFleet2          => IsKeyDown(Keys.LeftControl) && KeyPressed(Keys.Z);
        public bool KillThis             => KeyPressed(Keys.X);
        public bool SpawnRemnantShip     => KeyPressed(Keys.V);
        //Ingame controls
        public bool PreviousTarget       => BackMouseClick;
        public bool ChaseCam             => MiddleMouseClick;
        public bool TacticalIcons        => IsKeyDown(Keys.LeftAlt);

        public bool IsAltKeyDown    => IsKeyDown(Keys.LeftAlt)     || IsKeyDown(Keys.RightAlt);
        public bool IsCtrlKeyDown   => IsKeyDown(Keys.LeftControl) || IsKeyDown(Keys.RightControl);
        public bool IsShiftKeyDown  => IsKeyDown(Keys.LeftShift)   || IsKeyDown(Keys.RightShift);


        public bool ShipDesignExit => KeyPressed(Keys.Y);
        public bool ShipYardArcMove()
        {
            if (GlobalStats.AltArcControl)
            {
                return LeftMouseDown && IsAltKeyDown;
            }
            return LeftMouseHeld();
        }

        public Vector2 CursorPosition { get; private set; }

        public bool Undo              => KeyPressed(Keys.Z) && IsKeyDown(Keys.LeftControl);
        public bool LeftCtrlShift     => IsKeyDown(Keys.LeftControl) && IsKeyDown(Keys.LeftShift);

        public bool AButtonDown       => GamepadClicked(Buttons.A);
        public bool BButtonDown       => GamepadClicked(Buttons.B);
        public bool BButtonHeld       => GamepadHeld(Buttons.B);
        public bool C                 => KeyPressed(Keys.C);

        public bool OpenInventory     => KeyPressed(Keys.I) || GamepadClicked(Buttons.DPadDown);
        public bool Escaped           => KeyPressed(Keys.Escape);

        public bool ExitScreen        => GamepadClicked(Buttons.Back);

        public bool InGameSelect      => LeftMouseClick || GamepadClicked(Buttons.A);
        public bool Land              => KeyPressed(Keys.L);

        public bool LeftShoulderDown  => GamepadClicked(Buttons.LeftShoulder);

        public bool MenuCancel => KeyPressed(Keys.Escape) || GamepadClicked(Buttons.B) || GamepadClicked(Buttons.Back);
        public bool MenuSelect => KeyPressed(Keys.Space)  || KeyPressed(Keys.Enter)    || GamepadClicked(Buttons.A) || GamepadClicked(Buttons.Start);
        public bool MenuUp     => KeyPressed(Keys.Up)     || GamepadClicked(Buttons.DPadUp)   || LeftStickFlickUp;
        public bool MenuDown   => KeyPressed(Keys.Down)   || GamepadClicked(Buttons.DPadDown) || LeftStickFlickDown;

        public Vector2 NormalizedCursorPosition { get; set; }

        public bool OpenMap => KeyPressed(Keys.M);

        public bool Up    => KeyPressed(Keys.Up)    || KeyPressed(Keys.W) || GamepadClicked(Buttons.DPadUp);
        public bool Down  => KeyPressed(Keys.Down)  || KeyPressed(Keys.S) || GamepadClicked(Buttons.DPadDown);
        public bool Left  => KeyPressed(Keys.Left)  || KeyPressed(Keys.A) || GamepadClicked(Buttons.DPadLeft);
        public bool Right => KeyPressed(Keys.Right) || KeyPressed(Keys.D) || GamepadClicked(Buttons.DPadRight);

        public bool ScrollIn  => MouseCurr.ScrollWheelValue > ScrollWheelPrev;
        public bool ScrollOut => MouseCurr.ScrollWheelValue < ScrollWheelPrev;

        public bool RightShoulderDown => GamepadClicked(Buttons.RightShoulder);
        public bool StartButtonDown   => GamepadClicked(Buttons.Start);

        public bool Tab => KeyPressed(Keys.Tab);
        public bool XButtonDown => GamepadClicked(Buttons.X);
        public bool YButtonDown => GamepadClicked(Buttons.Y);
        public bool XButtonHeld => GamepadHeld(Buttons.X);
        public bool YButtonHeld => GamepadHeld(Buttons.Y);


        private void UpdateTimers(float time)
        {
            TimerUpdate(time, LeftMouseHeld(0), ref LeftMouseDownTime);
            TimerUpdate(time, RightMouseHeld(0), ref RightMouseDownTime);

        }
        private static void TimerUpdate(float time, bool update, ref float timer)
        {
            if (update)
                timer += time;
            else
                timer = 0;
        }
        public void Update(GameTime gameTime)
        {
            float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            UpdateTimers(elapsedTime);

            KeysPrev        = KeysCurr;
            GamepadPrev     = GamepadCurr;
            MousePrev       = MouseCurr;
            ScrollWheelPrev = MouseCurr.ScrollWheelValue;
            MouseCurr       = Mouse.GetState();
            CursorPosition  = new Vector2(MouseCurr.X, MouseCurr.Y);
            KeysCurr        = Keyboard.GetState();
        }
    }
}