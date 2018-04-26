using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Ship_Game
{
    public sealed class InputState
    {
        //public bool CancelInput = ExitScreenTimer >0;
        public KeyboardState KeysCurr;
        public KeyboardState KeysPrev;
        public GamePadState GamepadCurr;
        public GamePadState GamepadPrev;
        public MouseState MouseCurr;
        public MouseState MousePrev;
        public int ScrollWheelPrev;
        public float DoubleClickTime = .5f;
        public bool Repeat;
        public float ExitScreenTimer;
        // MouseDrag variables
        public Vector2 StartRighthold { get; private set; }
        public Vector2 EndRightHold { get; private set; }
        public Vector2 StartLeftHold { get; private set; }
        public Vector2 EndLeftHold { get; private set; }
       
        public bool WasAnyKeyPressed => KeysCurr.GetPressedKeys().Length > 0;

        // Mouse Timers
        private float RightMouseDownTime;
        private float LeftMouseDownTime;
        private bool RightMouseWasHeldInteral = false;
        private bool LeftMouseWasHeldInteral  = false;
        public bool RightMouseWasHeld         => RightMouseWasHeldInteral;
        public bool LeftMouseWasHeld          => LeftMouseWasHeldInteral;
        public float ReadRightMouseDownTime   => RightMouseDownTime;
        private bool RightHeld                = false;
        private bool LeftHeld                 = false;
        private float LeftDblClickTimer  = 0f;
        private float RightDblClickTimer = 0f;
        public bool RightMouseDoubleClick  { get; private set; }
        public bool LeftMouseDoubleClick { get; private set; }


        // Mouse Clicks
        public bool RightMouseClick    => MouseButtonClicked(MouseCurr.RightButton, MousePrev.RightButton);
        public bool LeftMouseClick     => MouseButtonClicked(MouseCurr.LeftButton, MousePrev.LeftButton);
        public bool BackMouseClick     => MouseButtonClicked(MouseCurr.XButton1, MousePrev.XButton1);
        public bool ForwardMouseClick  => MouseButtonClicked(MouseCurr.XButton2, MousePrev.XButton2);
        public bool MiddleMouseClick   => MouseButtonClicked(MouseCurr.MiddleButton, MousePrev.MiddleButton);
        public bool LeftMouseReleased  => MouseButtonReleased(MouseCurr.LeftButton, MousePrev.LeftButton);
        public bool RightMouseReleased => MouseButtonReleased(MouseCurr.RightButton, MousePrev.RightButton);
        public bool LeftMouseDown      => MouseCurr.LeftButton  == ButtonState.Pressed;
        public bool RightMouseDown     => MouseCurr.RightButton == ButtonState.Pressed;
        public bool LeftMouseUp        => MouseCurr.LeftButton  != ButtonState.Pressed;
        public bool RightMouseUp       => MouseCurr.RightButton != ButtonState.Pressed;
        public bool LeftMouseHeldDown  => MouseCurr.LeftButton   == ButtonState.Pressed && MousePrev.LeftButton  == ButtonState.Pressed;
        public bool RightMouseHeldDown => MouseCurr.RightButton == ButtonState.Pressed && MousePrev.RightButton == ButtonState.Pressed;
        public bool RightMouseHeldUp   => MouseCurr.RightButton != ButtonState.Pressed && MousePrev.RightButton != ButtonState.Pressed && !LeftMouseWasHeld;
        public bool LeftMouseHeldUp    => MouseCurr.LeftButton != ButtonState.Pressed && MousePrev.LeftButton != ButtonState.Pressed;
        public Vector2 MouseScreenPos  => new Vector2(MouseCurr.X, MouseCurr.Y);

        //mouse position
        private Vector2 MouseRightClickPos = Vector2.Zero;
        private Vector2 MouseLeftClickPos = Vector2.Zero;
        public bool MouseDrag => StartLeftHold != Vector2.Zero || StartRighthold != Vector2.Zero;
        private bool MouseLeftDrag = false;
        private bool MouseRightDrag = false;
        private void SetMouseDrag()
        {
            MouseLeftDrag =  MouseCursorDragCheck(MouseLeftDrag, ref MouseLeftClickPos, MouseCurr.LeftButton);
            MouseRightDrag = MouseCursorDragCheck(MouseRightDrag, ref MouseRightClickPos, MouseCurr.RightButton);
        }

        private bool MouseCursorDragCheck(bool set, ref Vector2 cursorPos, ButtonState pressed)
        {
            if (pressed != ButtonState.Pressed)
            {
                cursorPos = Vector2.Zero;
                return false;
            }
            if (cursorPos == Vector2.Zero)
            {
                cursorPos = CursorPosition;
                return false;
            }

            if (set || cursorPos.SqDist(CursorPosition) > 10)
                return true;
            return false;

        }

        public bool LeftMouseHeld(float seconds = 0.15f)
        {
            LeftHeld = MouseLeftDrag && MouseButtonHeld(MouseCurr.LeftButton, MousePrev.LeftButton, seconds, LeftMouseDownTime);            
            return LeftHeld;
        }
        public bool RightMouseHeld(float seconds = 0.15f)
        {
            RightHeld = MouseRightDrag && MouseButtonHeld(MouseCurr.RightButton, MousePrev.RightButton, seconds, RightMouseDownTime);            
            return RightHeld;
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
        public bool PauseGame            => KeyPressed(Keys.Space) && !IsShiftKeyDown;
        public bool UseRealLights        => KeyPressed(Keys.F5);
        public bool ShowExceptionTracker => KeyPressed(Keys.F6);
        public bool SendKudos            => KeyPressed(Keys.F7);
        public bool SpeedReset           => KeyPressed(Keys.Space) && IsShiftKeyDown;
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
        public bool AddToFleet           => IsCtrlKeyDown && !IsShiftKeyDown;
        public bool ReplaceFleet         => IsCtrlKeyDown && IsShiftKeyDown;
        public bool QueueAction          => IsShiftKeyDown;
        public bool OrderOption          => IsAltKeyDown;
        public bool ShipPieMenu          => WasKeyPressed(Keys.Q);
        
        //input.KeysCurr.IsKeyDown(Keys.LeftAlt)
        //IngameWiki
        public bool ExitWiki => KeyPressed(Keys.P) && !GlobalStats.TakingInput;

        //FleetDesignScreen
        public bool FleetRemoveSquad => KeyPressed(Keys.Back) || KeyPressed(Keys.Delete);
        public bool FleetExitScreen => KeyPressed(Keys.J) || KeyPressed(Keys.Escape);

        //debug
        public bool DebugMode            => LeftCtrlShift && (KeyPressed(Keys.OemTilde) || KeyPressed(Keys.Tab));
        public bool GetMemory            => KeyPressed(Keys.G);
        public bool ShowDebugWindow      => KeyPressed(Keys.H);  
        public bool EmpireToggle         => IsKeyDown(Keys.LeftShift);
        public bool SpawnShip            => KeyPressed(Keys.C);
        public bool SpawnFleet1          => KeyPressed(Keys.Z) && !IsKeyDown(Keys.LeftControl);
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
        public bool IsEnterOrEscape => IsKeyDown(Keys.Enter)       || IsKeyDown(Keys.Escape);

        //researchScreen
        public bool ResearchExitScreen => KeyPressed(Keys.R);

        public bool ShipDesignExit => KeyPressed(Keys.Y);
        public bool ShipYardArcMove()
        {
            if (GlobalStats.AltArcControl)
            {
                return LeftMouseDown && IsAltKeyDown;
            }
            return LeftMouseHeld();
        }

        public Vector2 CursorPosition { get ; private set; }

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

        public bool WASDUp => KeyPressed(Keys.W);
        public bool WASDDown => KeyPressed(Keys.S);
        public bool WASDLeft => KeyPressed(Keys.A);
        public bool WASDRight => KeyPressed(Keys.D);

        public bool ArrowRight => KeyPressed(Keys.Right) || KeyPressed(Keys.NumPad6);
        public bool ArrowUp => KeyPressed(Keys.Up) || KeyPressed(Keys.NumPad8);
        public bool ArrowDown => KeyPressed(Keys.Down) || KeyPressed(Keys.NumPad2);
        public bool ArrowLeft => KeyPressed(Keys.Left) || KeyPressed(Keys.NumPad4);

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
            Vector2 endHoldPoint =  Vector2.Zero;
            TimerUpdate(time, LeftMouseDown, ref LeftMouseDownTime, ref LeftMouseWasHeldInteral, ref LeftHeld);
            EndLeftHold = endHoldPoint;
            TimerUpdate(time, RightMouseDown, ref RightMouseDownTime, ref RightMouseWasHeldInteral, ref RightHeld);
            EndRightHold       = endHoldPoint;
            RightDblClickTimer =  RightDblClickTimer <= 0 && RightMouseClick ? DoubleClickTime : RightDblClickTimer - time;
            LeftDblClickTimer  = LeftDblClickTimer <=0 && LeftMouseClick ? DoubleClickTime : LeftDblClickTimer - time;

        }
        private void TimerUpdate(float time, bool update, ref float timer, ref bool wasHeld, ref bool held)
        {
            if (update)
            {
                timer += time;
            }
            else
            {
                wasHeld = held && timer > 0;
                held = false;
                timer = 0;
            }
        }
        private Vector2 UpdateHoldStartPosistion(bool held, bool wasHeld, Vector2 holdPosistion, bool drag)
        {
            if (!held && !wasHeld) return Vector2.Zero;
            return held && drag && holdPosistion == Vector2.Zero ? CursorPosition : holdPosistion;            
        }
        private Vector2 UpdateHoldEndPosistion(bool held, bool wasHeld, Vector2 holdPosistion, bool drag)
        {
            if (!(held && drag) && !wasHeld) return Vector2.Zero;
            return CursorPosition;
        }
        public void Update(GameTime gameTime)
        {
            float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            KeysPrev        = KeysCurr;
            GamepadPrev     = GamepadCurr;
            MousePrev       = MouseCurr;
            ScrollWheelPrev = MouseCurr.ScrollWheelValue;
            MouseCurr       = Mouse.GetState();
            CursorPosition  = new Vector2(MouseCurr.X, MouseCurr.Y);
            KeysCurr        = Keyboard.GetState();
            if (ExitScreenTimer >= 0)
            {
                ExitScreenTimer -= elapsedTime;
                return;
            }
            SetMouseDrag();

            RightMouseDoubleClick = RightDblClickTimer > 0 && RightMouseClick;
            RightDblClickTimer = RightMouseDoubleClick ? 0 : RightDblClickTimer;

            LeftMouseDoubleClick = LeftDblClickTimer > 0 && LeftMouseClick;
            LeftDblClickTimer = LeftMouseDoubleClick ? 0 : LeftDblClickTimer;
                
            UpdateTimers(elapsedTime);

            StartRighthold = UpdateHoldStartPosistion(RightHeld, RightMouseWasHeld, StartRighthold, MouseRightDrag);
            EndRightHold   = UpdateHoldEndPosistion(RightHeld, RightMouseWasHeld, StartRighthold, MouseRightDrag);
            StartLeftHold  = UpdateHoldStartPosistion(LeftHeld, LeftMouseWasHeld, StartLeftHold, MouseLeftDrag);
            EndLeftHold    = UpdateHoldEndPosistion(LeftHeld, LeftMouseWasHeld, StartLeftHold, MouseLeftDrag);

        }
    }
}