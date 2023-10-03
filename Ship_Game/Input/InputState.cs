using System;
using Vector2 = SDGraphics.Vector2;
using XnaInput = Microsoft.Xna.Framework.Input;
using SDGraphics.Input;

namespace Ship_Game
{
    // @note This abstraction is used for unit tests
    public interface IInputProvider
    {
        XnaInput.MouseState GetMouse();
        XnaInput.KeyboardState GetKeyboard();
        XnaInput.GamePadState GetGamePad();
        void SetMouse(int x, int y);
    }

    public class DefaultInputProvider : IInputProvider
    {
        public XnaInput.MouseState GetMouse() => XnaInput.Mouse.GetState();
        public XnaInput.KeyboardState GetKeyboard() => XnaInput.Keyboard.GetState();
        public XnaInput.GamePadState GetGamePad() => XnaInput.GamePad.GetState(Microsoft.Xna.Framework.PlayerIndex.One);
        public void SetMouse(int x, int y) => XnaInput.Mouse.SetPosition(x, y);
    }

    public sealed partial class InputState
    {
        public IInputProvider Provider;
        public XnaInput.KeyboardState KeysCurr;
        public XnaInput.KeyboardState KeysPrev;
        public XnaInput.GamePadState GamepadCurr;
        public XnaInput.GamePadState GamepadPrev;
        public XnaInput.MouseState MouseCurr; // Use CursorPosition for mouse position
        public XnaInput.MouseState MousePrev; // Use PrevCursorPos for previous frame's mouse pos

        public int ScrollWheelPrev;
        public float ExitScreenTimer;

        public Keys[] GetKeysDown()
        {
            return KeysCurr.GetPressedKeys().Select(xnaKey => (Keys)xnaKey);
        }

        // Mouse position
        public Vector2 CursorPosition { get; private set; }
        public Vector2 PrevCursorPos { get; private set; }
        public Vector2 CursorDirection => PrevCursorPos.DirectionToTarget(CursorPosition);
        public Vector2 CursorVelocity  => PrevCursorPos - CursorPosition;
        public float CursorX => CursorPosition.X;
        public float CursorY => CursorPosition.Y;
        public int MouseX { get; private set; }
        public int MouseY { get; private set; }
        public bool MouseMoved { get; private set; }

        public bool WasAnyKeyPressed => KeysCurr.GetPressedKeys().Length > 0;

        // Mouse Clicks
        public bool RightMouseClick    => MouseButtonClicked(MouseCurr.RightButton, MousePrev.RightButton);
        public bool LeftMouseClick     => MouseButtonClicked(MouseCurr.LeftButton, MousePrev.LeftButton);
        public bool BackMouseClick     => MouseButtonClicked(MouseCurr.XButton1, MousePrev.XButton1);
        public bool ForwardMouseClick  => MouseButtonClicked(MouseCurr.XButton2, MousePrev.XButton2);
        public bool MiddleMouseClick   => MouseButtonClicked(MouseCurr.MiddleButton, MousePrev.MiddleButton);
        public bool LeftMouseReleased  => MouseButtonReleased(MouseCurr.LeftButton, MousePrev.LeftButton);
        public bool RightMouseReleased => MouseButtonReleased(MouseCurr.RightButton, MousePrev.RightButton);
        public bool LeftMouseDown      => MouseCurr.LeftButton  == XnaInput.ButtonState.Pressed;
        public bool RightMouseDown     => MouseCurr.RightButton == XnaInput.ButtonState.Pressed;
        public bool LeftMouseUp        => MouseCurr.LeftButton  != XnaInput.ButtonState.Pressed;
        public bool RightMouseUp       => MouseCurr.RightButton != XnaInput.ButtonState.Pressed;

        static bool MouseButtonClicked(XnaInput.ButtonState current, XnaInput.ButtonState prev)
            => current == XnaInput.ButtonState.Pressed && prev == XnaInput.ButtonState.Released;

        static bool MouseButtonReleased(XnaInput.ButtonState current, XnaInput.ButtonState prev)
            => current == XnaInput.ButtonState.Released && prev == XnaInput.ButtonState.Pressed;

        public bool IsKeyDown(Keys key) => KeysCurr.IsKeyDown((XnaInput.Keys)key);

        // key was pressed down (previous state was up)
        public bool KeyPressed(Keys key) => KeysCurr.IsKeyDown((XnaInput.Keys)key) && KeysPrev.IsKeyUp((XnaInput.Keys)key);

        public bool GamepadClicked(Buttons button)
            => GamepadCurr.IsButtonDown((XnaInput.Buttons)button) && GamepadPrev.IsButtonUp((XnaInput.Buttons)button);

        public bool GamepadHeld(Buttons button) => GamepadCurr.IsButtonDown((XnaInput.Buttons)button);

        public bool LeftStickFlickDown => GamepadCurr.ThumbSticks.Left.Y < 0f && GamepadPrev.ThumbSticks.Left.Y >= 0f;
        public bool LeftStickFlickUp   => GamepadCurr.ThumbSticks.Left.Y > 0f && GamepadPrev.ThumbSticks.Left.Y <= 0f;

        //Ingame 
        //UniverseScreen
        public bool PauseGame            => KeyPressed(Keys.Space) && !IsShiftKeyDown;
        public bool QuickSave            => KeyPressed(Keys.F5) && !IsShiftKeyDown;
        public bool UseRealLights        => IsShiftKeyDown && KeyPressed(Keys.F5);
        public bool ShowExceptionTracker => KeyPressed(Keys.F6);
        public bool SpeedReset           => KeyPressed(Keys.Space) && IsShiftKeyDown;
        public bool SpeedUp              => KeyPressed(Keys.OemPlus) || KeyPressed(Keys.Add);
        public bool SpeedDown            => KeyPressed(Keys.OemMinus) || KeyPressed(Keys.Subtract);
        public bool ScrapShip            => KeyPressed(Keys.Back) || KeyPressed(Keys.Delete);
        public bool ZoomToShip           => KeyPressed(Keys.PageUp);
        public bool ZoomOut              => KeyPressed(Keys.PageDown);
        public bool DeepSpaceBuildWindow => KeyPressed(Keys.B);
        public bool PlanetListScreen     => KeyPressed(Keys.L);
        public bool ExoticListScreen     => KeyPressed(Keys.G);
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
        public bool AddToFleet           => IsCtrlKeyDown && IsShiftKeyDown;
        public bool ReplaceFleet         => IsCtrlKeyDown && !IsShiftKeyDown;
        public bool QueueAction          => IsShiftKeyDown;
        public bool ShipPieMenu          => KeyPressed(Keys.Q);
        
        // IngameWiki
        public bool ExitWiki => KeyPressed(Keys.P);

        // FleetDesignScreen
        public bool FleetRemoveSquad => KeyPressed(Keys.Back) || KeyPressed(Keys.Delete);
        public bool FleetExitScreen => KeyPressed(Keys.J) || KeyPressed(Keys.Escape);

        // debug
        public bool DebugMode        => LeftCtrlShift && (KeyPressed(Keys.OemTilde) || KeyPressed(Keys.Tab));
        public bool GetMemory        => KeyPressed(Keys.G);
        public bool ShowDebugWindow  => KeyPressed(Keys.H);  
        public bool EmpireToggle     => IsKeyDown(Keys.LeftShift);
        public bool RemnantToggle    => IsAltKeyDown;
        public bool SpawnShip        => KeyPressed(Keys.C);
        public bool SpawnFleet1      => KeyPressed(Keys.Z) && !IsKeyDown(Keys.LeftControl);
        public bool SpawnFleet2      => IsKeyDown(Keys.LeftControl) && KeyPressed(Keys.Z);
        public bool DebugKillShip         => KeyPressed(Keys.X) || KeyPressed(Keys.Delete);
        public bool SpawnRemnant     => KeyPressed(Keys.V);
        public bool SpawnPlayerTroop => KeyPressed(Keys.Z);

        public bool BlowExplodingModule      => KeyPressed(Keys.N);
        public bool ToggleSpatialManagerType => KeyPressed(Keys.G) && !IsKeyDown(Keys.LeftWindows);
        // Ingame controls
        public bool PreviousTarget  => BackMouseClick;
        public bool TacticalIcons   => IsKeyDown(Keys.LeftAlt);
        public bool CinematicMode   => KeyPressed(Keys.F11);

        public bool IsAltKeyDown    => IsKeyDown(Keys.LeftAlt)     || IsKeyDown(Keys.RightAlt);
        public bool IsCtrlKeyDown   => IsKeyDown(Keys.LeftControl) || IsKeyDown(Keys.RightControl);
        public bool IsShiftKeyDown  => IsKeyDown(Keys.LeftShift)   || IsKeyDown(Keys.RightShift);
        public bool IsCapsLockDown  => System.Windows.Forms.Control.IsKeyLocked(System.Windows.Forms.Keys.Capital);
        public bool IsEnterOrEscape => IsKeyDown(Keys.Enter)       || IsKeyDown(Keys.Escape);
        public bool IsBackSpace     => IsKeyDown(Keys.Back);

        // ship selection
        public bool SelectSameDesign      => IsCtrlKeyDown && IsAltKeyDown;
        public bool SelectSameHull        => IsAltKeyDown;
        public bool SelectSameRoleAndHull => IsCtrlKeyDown;

        // researchScreen
        public bool ResearchExitScreen => KeyPressed(Keys.R);

        public bool ShipDesignExit => KeyPressed(Keys.Y) && !IsCtrlKeyDown;
        public bool ShipYardArcMove()
        {
            if (GlobalStats.AltArcControl)
                return LeftMouseDown && IsAltKeyDown;
            return LeftMouseHeld();
        }

        public bool Undo => IsCtrlKeyDown && KeyPressed(Keys.Z); // Ctrl+Z
        public bool Redo => IsCtrlKeyDown && (KeyPressed(Keys.Y) || (IsShiftKeyDown && KeyPressed(Keys.Z))); // Ctrl+Y or Ctrl+Shift+Z
        public bool LeftCtrlShift => IsKeyDown(Keys.LeftControl) && IsKeyDown(Keys.LeftShift);

        public bool AButtonDown   => GamepadClicked(Buttons.A);
        public bool BButtonDown   => GamepadClicked(Buttons.B);
        public bool BButtonHeld   => GamepadHeld(Buttons.B);
        public bool C             => KeyPressed(Keys.C);

        public bool OpenInventory => KeyPressed(Keys.I) || GamepadClicked(Buttons.DPadDown);
        public bool Escaped       => KeyPressed(Keys.Escape);

        public bool ExitScreen    => GamepadClicked(Buttons.Back);

        public bool InGameSelect  => LeftMouseClick || GamepadClicked(Buttons.A);
        public bool Land          => KeyPressed(Keys.L);

        public bool LeftShoulderDown => GamepadClicked(Buttons.LeftShoulder);

        public bool MenuCancel => KeyPressed(Keys.Escape) || GamepadClicked(Buttons.B) || GamepadClicked(Buttons.Back);
        public bool MenuSelect => KeyPressed(Keys.Space)  || KeyPressed(Keys.Enter)    || GamepadClicked(Buttons.A) || GamepadClicked(Buttons.Start);
        public bool MenuUp     => KeyPressed(Keys.Up)     || GamepadClicked(Buttons.DPadUp)   || LeftStickFlickUp;
        public bool MenuDown   => KeyPressed(Keys.Down)   || GamepadClicked(Buttons.DPadDown) || LeftStickFlickDown;

        public bool OpenMap => KeyPressed(Keys.M);

        public bool Up    => KeyPressed(Keys.Up)    || KeyPressed(Keys.W) || GamepadClicked(Buttons.DPadUp);
        public bool Down  => KeyPressed(Keys.Down)  || KeyPressed(Keys.S) || GamepadClicked(Buttons.DPadDown);
        public bool Left  => KeyPressed(Keys.Left)  || KeyPressed(Keys.A) || GamepadClicked(Buttons.DPadLeft);
        public bool Right => KeyPressed(Keys.Right) || KeyPressed(Keys.D) || GamepadClicked(Buttons.DPadRight);

        // WSAD and Arrow Keys
        public bool KeysUpHeld(bool arrowKeys = true)    => (arrowKeys && IsKeyDown(Keys.Up))    || IsKeyDown(Keys.W) || GamepadHeld(Buttons.DPadUp);
        public bool KeysDownHeld(bool arrowKeys = true)  => (arrowKeys && IsKeyDown(Keys.Down))  || IsKeyDown(Keys.S) || GamepadHeld(Buttons.DPadDown);
        public bool KeysLeftHeld(bool arrowKeys = true)  => (arrowKeys && IsKeyDown(Keys.Left))  || IsKeyDown(Keys.A) || GamepadHeld(Buttons.DPadLeft);
        public bool KeysRightHeld(bool arrowKeys = true) => (arrowKeys && IsKeyDown(Keys.Right)) || IsKeyDown(Keys.D) || GamepadHeld(Buttons.DPadRight);

        public bool WASDUp    => IsKeyDown(Keys.W);
        public bool WASDDown  => IsKeyDown(Keys.S);
        public bool WASDLeft  => IsKeyDown(Keys.A);
        public bool WASDRight => IsKeyDown(Keys.D);

        public bool ArrowRight => KeyPressed(Keys.Right) || KeyPressed(Keys.NumPad6);
        public bool ArrowUp    => KeyPressed(Keys.Up) || KeyPressed(Keys.NumPad8);
        public bool ArrowDown  => KeyPressed(Keys.Down) || KeyPressed(Keys.NumPad2);
        public bool ArrowLeft  => KeyPressed(Keys.Left) || KeyPressed(Keys.NumPad4);

        public bool ScrollIn  => MouseCurr.ScrollWheelValue > ScrollWheelPrev;
        public bool ScrollOut => MouseCurr.ScrollWheelValue < ScrollWheelPrev;

        public bool RightShoulderDown => GamepadClicked(Buttons.RightShoulder);
        public bool StartButtonDown   => GamepadClicked(Buttons.Start);

        public bool Tab         => KeyPressed(Keys.Tab);
        public bool XButtonDown => GamepadClicked(Buttons.X);
        public bool YButtonDown => GamepadClicked(Buttons.Y);
        public bool XButtonHeld => GamepadHeld(Buttons.X);
        public bool YButtonHeld => GamepadHeld(Buttons.Y);

        public bool DesignMirrorToggled => KeyPressed(Keys.M);

        public InputState()
        {
            Provider = new DefaultInputProvider();
        }

        public void Update(UpdateTimes elapsed)
        {
            KeysPrev    = KeysCurr;
            GamepadPrev = GamepadCurr;
            MousePrev   = MouseCurr;
            ScrollWheelPrev = MouseCurr.ScrollWheelValue;
            PrevCursorPos = CursorPosition;

            MouseCurr = Provider.GetMouse();
            KeysCurr = Provider.GetKeyboard();
            GamepadCurr = Provider.GetGamePad();

            MouseX = MouseCurr.X;
            MouseY = MouseCurr.Y;
            CursorPosition = new Vector2(MouseX, MouseY);
            MouseMoved = CursorPosition.Distance(PrevCursorPos) > 1;

            UpdateDoubleClick(elapsed);
            UpdateHolding(elapsed);
        }
    }
}