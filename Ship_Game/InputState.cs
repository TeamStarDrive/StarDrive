using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;

namespace Ship_Game
{
    public sealed class InputState
    {
        public KeyboardState CurrentKeyboardState;

        public GamePadState CurrentGamePadState;

        public KeyboardState LastKeyboardState;

        public GamePadState LastGamePadState;

        public MouseState CurrentMouseState;

        public MouseState LastMouseState;

        public int PreviousScrollWheelValue;

        public bool Repeat;
        private float Timer;

        public float RightMouseTimer = 0.35f;
        public bool RightMouseClick => (CurrentMouseState.RightButton == ButtonState.Pressed && LastMouseState.RightButton == ButtonState.Released);
        public bool LeftMouseClick => (CurrentMouseState.LeftButton == ButtonState.Pressed && LastMouseState.LeftButton == ButtonState.Released);
        public bool BackMouseClick => (CurrentMouseState.XButton1 == ButtonState.Pressed && LastMouseState.XButton1 == ButtonState.Released);
        public bool MiddleMouseClick => (CurrentMouseState.MiddleButton == ButtonState.Pressed && LastMouseState.MiddleButton == ButtonState.Released);
        public bool LeftMouseRelease => CurrentMouseState.LeftButton == ButtonState.Released;
        public Vector2 MouseScreenPos => new Vector2(CurrentMouseState.X, CurrentMouseState.Y);
        public bool LeftMouseHeld(float seconds = .25f)
        {
            if (CurrentMouseState.LeftButton == ButtonState.Pressed && LastMouseState.LeftButton == ButtonState.Pressed)
            {
                Timer += .01666f;
                return Timer >= seconds;
            }
            Timer = 0;
            return false;
        }
        
        //Ingame 
        //UniverseScreen
        public bool PauseGame            => IsNewKeyPress(Keys.Space);
        public bool UseRealLights        => IsNewKeyPress(Keys.F5);
        public bool ShowExceptionTracker => IsNewKeyPress(Keys.F6);
        public bool SendKudos            => IsNewKeyPress(Keys.F7);
        public bool SpeedUp              => IsNewKeyPress(Keys.OemPlus) || IsNewKeyPress(Keys.Add);
        public bool SpeedDown            => IsNewKeyPress(Keys.OemMinus) || IsNewKeyPress(Keys.Subtract);
        public bool ScrapShip            => IsNewKeyPress(Keys.Back) || IsNewKeyPress(Keys.Delete);
        public bool ZoomToShip           => IsNewKeyPress(Keys.PageUp);
        public bool ZoomOut              => IsNewKeyPress(Keys.PageDown);
        public bool DeepSpaceBuildWindow => IsNewKeyPress(Keys.B);
        public bool PlanetListScreen     => IsNewKeyPress(Keys.L);
        public bool FTLOverlay           => IsNewKeyPress(Keys.F1);
        public bool RangeOverlay         => IsNewKeyPress(Keys.F2);
        public bool ShipListScreen       => IsNewKeyPress(Keys.K);
        public bool FleetDesignScreen    => IsNewKeyPress(Keys.J);
        public bool AutomationWindow     => IsNewKeyPress(Keys.H);
        public bool Fleet1               => IsNewKeyPress(Keys.D1);
        public bool Fleet2               => IsNewKeyPress(Keys.D2);
        public bool Fleet3               => IsNewKeyPress(Keys.D3);
        public bool Fleet4               => IsNewKeyPress(Keys.D4);
        public bool Fleet5               => IsNewKeyPress(Keys.D5);
        public bool Fleet6               => IsNewKeyPress(Keys.D6);
        public bool Fleet7               => IsNewKeyPress(Keys.D7);
        public bool Fleet8               => IsNewKeyPress(Keys.D8);
        public bool Fleet9               => IsNewKeyPress(Keys.D9);
        public bool AddToFleet           => RepeatingKeyCheck(Keys.LeftControl) && !RepeatingKeyCheck(Keys.LeftShift);
        public bool ReplaceFleet         => RepeatingKeyCheck(Keys.LeftControl) && RepeatingKeyCheck(Keys.LeftShift);

        //IngameWiki
        public bool ExitWiki => IsNewKeyPress(Keys.P) && !GlobalStats.TakingInput;

        //debug
        public bool DebugMode            => LeftCtrlShift && (IsNewKeyPress(Keys.OemTilde) || IsNewKeyPress(Keys.Tab));
        public bool GetMemory            => IsNewKeyPress(Keys.G);
        public bool ShowDebugWindow      => IsNewKeyPress(Keys.H);  
        public bool EmpireToggle         => RepeatingKeyCheck(Keys.LeftShift);
        public bool SpawnShip            => IsNewKeyPress(Keys.C);
        public bool SpawnFleet1          => IsNewKeyPress(Keys.Z);
        public bool SpawnFleet2          => RepeatingKeyCheck(Keys.LeftControl) && IsNewKeyPress(Keys.Z);
        public bool KillThis             => IsNewKeyPress(Keys.X);
        public bool SpawnRemnantShip     => IsNewKeyPress(Keys.V);
        //Ingame controls
        public bool PreviousTarget       => BackMouseClick;
        public bool ChaseCam             => MiddleMouseClick;
        public bool TacticalIcons        => RepeatingKeyCheck(Keys.LeftAlt);
        //Ingame debug
        // public bool 

        //ShipDesign Screen
        public bool ShipDesignExit => CurrentKeyboardState.IsKeyDown(Keys.Y) && !LastKeyboardState.IsKeyDown(Keys.Y);
        public bool ShipYardArcMove()
        {
            if (GlobalStats.AltArcControl)
            {
                //The Doctor: ALT (either) + LEFT CLICK to pick and move arcs. This way, it's impossible to accidentally pick the wrong arc, while it's just as responsive and smooth as the original method when you are trying to.                    
                return CurrentMouseState.LeftButton == ButtonState.Pressed &&
                       CurrentMouseState.LeftButton == ButtonState.Pressed &&
                       (CurrentKeyboardState.IsKeyDown(Keys.LeftAlt) ||
                        LastKeyboardState.IsKeyDown(Keys.LeftAlt)
                        || CurrentKeyboardState.IsKeyDown(Keys.RightAlt)
                        || LastKeyboardState.IsKeyDown(Keys.RightAlt));
            }
            return LeftMouseHeld();
        }
        /// <summary>
        /// below are the defaults set previously. i bleieve the idea is to set the button wanted here with a name to indicate its use.
        /// </summary>

        //keyCombinations
        private bool LeftCtrlShift => CurrentKeyboardState.IsKeyDown(Keys.LeftControl) && CurrentKeyboardState.IsKeyDown(Keys.LeftShift);

        public bool AButtonDown
        {
            get
            {
                if (CurrentGamePadState.Buttons.A != ButtonState.Pressed)
                {
                    return false;
                }
                return LastGamePadState.Buttons.A == ButtonState.Released;
            }
        }

        public bool BButtonDown
        {
            get
            {
                if (CurrentGamePadState.Buttons.B != ButtonState.Pressed)
                {
                    return false;
                }
                return LastGamePadState.Buttons.B == ButtonState.Released;
            }
        }

        public bool BButtonHeld => CurrentGamePadState.Buttons.B == ButtonState.Pressed;

        public bool C => IsNewKeyPress(Keys.C);

        public bool CommandOpenInventory
        {
            get
            {
                if (IsNewKeyPress(Keys.I))
                {
                    return true;
                }
                if (CurrentGamePadState.DPad.Down != ButtonState.Pressed)
                {
                    return false;
                }
                return LastGamePadState.DPad.Down == ButtonState.Released;
            }
        }

        public Vector2 CursorPosition { get; set; }

       
        public bool Escaped => IsNewKeyPress(Keys.Escape);

        public bool ExitScreen
        {
            get
            {
                if (CurrentGamePadState.Buttons.Back != ButtonState.Pressed)
                {
                    return false;
                }
                return LastGamePadState.Buttons.Back == ButtonState.Released;
            }
        }

        public bool InGameSelect
        {
            get
            {
                if (CurrentMouseState.LeftButton == ButtonState.Pressed && LastMouseState.LeftButton == ButtonState.Released)
                {
                    return true;
                }
                if (CurrentGamePadState.Buttons.A != ButtonState.Pressed)
                {
                    return false;
                }
                return LastGamePadState.Buttons.A == ButtonState.Released;
            }
        }

        public bool Land => IsNewKeyPress(Keys.L);

        public bool RepeatingKeyCheck(Keys key) => CurrentKeyboardState.IsKeyDown(key);
        public bool IsKeyDown(Keys key)         => CurrentKeyboardState.IsKeyDown(key);
        
        public bool LeftShoulderDown
        {
            get
            {
                if (CurrentGamePadState.Buttons.LeftShoulder != ButtonState.Pressed)
                {
                    return false;
                }
                return LastGamePadState.Buttons.LeftShoulder == ButtonState.Released;
            }
        }

        public bool MenuCancel
        {
            get
            {
                if (IsNewKeyPress(Keys.Escape) || CurrentGamePadState.Buttons.B == ButtonState.Pressed && LastGamePadState.Buttons.B == ButtonState.Released)
                {
                    return true;
                }
                if (CurrentGamePadState.Buttons.Back != ButtonState.Pressed)
                {
                    return false;
                }
                return LastGamePadState.Buttons.Back == ButtonState.Released;
            }
        }

        public bool MenuDown
        {
            get
            {
                if (IsNewKeyPress(Keys.Down) || CurrentGamePadState.DPad.Down == ButtonState.Pressed && LastGamePadState.DPad.Down == ButtonState.Released)
                {
                    return true;
                }
                if (CurrentGamePadState.ThumbSticks.Left.Y >= 0f)
                {
                    return false;
                }
                return LastGamePadState.ThumbSticks.Left.Y >= 0f;
            }
        }

        public bool MenuSelect
        {
            get
            {
                if (IsNewKeyPress(Keys.Space) || IsNewKeyPress(Keys.Enter) || CurrentGamePadState.Buttons.A == ButtonState.Pressed && LastGamePadState.Buttons.A == ButtonState.Released)
                {
                    return true;
                }
                if (CurrentGamePadState.Buttons.Start != ButtonState.Pressed)
                {
                    return false;
                }
                return LastGamePadState.Buttons.Start == ButtonState.Released;
            }
        }

        public bool MenuUp
        {
            get
            {
                if (IsNewKeyPress(Keys.Up) || CurrentGamePadState.DPad.Up == ButtonState.Pressed && LastGamePadState.DPad.Up == ButtonState.Released)
                {
                    return true;
                }
                if (CurrentGamePadState.ThumbSticks.Left.Y <= 0f)
                {
                    return false;
                }
                return LastGamePadState.ThumbSticks.Left.Y <= 0f;
            }
        }
        public Vector2 NormalizedCursorPosition { get; set; }

        public bool OpenMap => IsNewKeyPress(Keys.M);

        


        public bool Right
        {
            get
            {
                if (IsNewKeyPress(Keys.Right) || IsNewKeyPress(Keys.D))
                {
                    return true;
                }
                if (CurrentGamePadState.DPad.Right != ButtonState.Pressed)
                {
                    return false;
                }
                return LastGamePadState.DPad.Right == ButtonState.Released;
            }
        }

        public bool Up
        {
            get
            {
                if (IsNewKeyPress(Keys.Up) || IsNewKeyPress(Keys.W))
                {
                    return true;
                }
                if (CurrentGamePadState.DPad.Up != ButtonState.Pressed)
                {
                    return false;
                }
                return LastGamePadState.DPad.Up == ButtonState.Released;
            }
        }

        public bool Down
        {
            get
            {
                if (IsNewKeyPress(Keys.Down) || IsNewKeyPress(Keys.S))
                {
                    return true;
                }
                if (CurrentGamePadState.DPad.Down != ButtonState.Pressed)
                {
                    return false;
                }
                return LastGamePadState.DPad.Down == ButtonState.Released;
            }
        }

        public bool Left
        {
            get
            {
                if (IsNewKeyPress(Keys.Left) || IsNewKeyPress(Keys.A))
                {
                    return true;
                }
                if (CurrentGamePadState.DPad.Left != ButtonState.Pressed)
                {
                    return false;
                }
                return LastGamePadState.DPad.Left == ButtonState.Released;
            }
        }



        public bool RightShoulderDown
        {
            get
            {
                if (CurrentGamePadState.Buttons.RightShoulder != ButtonState.Pressed)
                {
                    return false;
                }
                return LastGamePadState.Buttons.RightShoulder == ButtonState.Released;
            }
        }

        public bool ScrollIn => CurrentMouseState.ScrollWheelValue > PreviousScrollWheelValue;

        public bool ScrollOut => CurrentMouseState.ScrollWheelValue < PreviousScrollWheelValue;

        public bool StartButtonDown
        {
            get
            {
                if (CurrentGamePadState.Buttons.Start != ButtonState.Pressed)
                {
                    return false;
                }
                return LastGamePadState.Buttons.Start == ButtonState.Released;
            }
        }

        public bool Tab => IsNewKeyPress(Keys.Tab);

        
        public bool XButtonDown
        {
            get
            {
                if (CurrentGamePadState.Buttons.X != ButtonState.Pressed)
                {
                    return false;
                }
                return LastGamePadState.Buttons.X == ButtonState.Released;
            }
        }

        public bool XButtonHeld => CurrentGamePadState.Buttons.X == ButtonState.Pressed;

        public bool YButtonDown
        {
            get
            {
                if (CurrentGamePadState.Buttons.Y != ButtonState.Pressed)
                {
                    return false;
                }
                return LastGamePadState.Buttons.Y == ButtonState.Released;
            }
        }

        public bool YButtonHeld => CurrentGamePadState.Buttons.Y == ButtonState.Pressed;

        private bool IsNewKeyPress(Keys key)
        {
            if (!Repeat)
                return CurrentKeyboardState.IsKeyDown(key) && LastKeyboardState.IsKeyUp(key);
            return CurrentKeyboardState.IsKeyDown(key);
        }

        public bool WasKeyPressed(Keys key)
        {
            return CurrentKeyboardState.IsKeyDown(key) && LastKeyboardState.IsKeyUp(key);
        }

        public void Update(GameTime gameTime)
        {
            float elapsedTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (CurrentMouseState.RightButton != ButtonState.Pressed)
            {
                RightMouseTimer = 0.35f;
            }
            else
            {
                InputState rightMouseTimer = this;
                rightMouseTimer.RightMouseTimer = rightMouseTimer.RightMouseTimer - elapsedTime;
            }
            LastKeyboardState = CurrentKeyboardState;
            LastGamePadState = CurrentGamePadState;
            LastMouseState = CurrentMouseState;
            PreviousScrollWheelValue = CurrentMouseState.ScrollWheelValue;
            CurrentMouseState = Mouse.GetState();
            CursorPosition = new Vector2(CurrentMouseState.X, CurrentMouseState.Y);
            CurrentKeyboardState = Keyboard.GetState();
        }
    }
}