﻿using System;
using System.IO;
using System.Threading;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game;
using Vector2 = SDGraphics.Vector2;
using SDGraphics.Input;

namespace UnitTests
{
    public class TestGameDummy : GameDummy
    {
        readonly AutoResetEvent Started;
        public InputState Input => ScreenManager.input;
        bool CachedVisibility;
        public bool Visible;

        public TestGameDummy(AutoResetEvent started, int width, int height, bool show)
            : base(width, height, show)
        {
            Started = started;
            CachedVisibility = Visible = show;
            if (Directory.GetCurrentDirectory() == @"C:\Projects\BlackBox\UnitTests\bin\Release")
                Directory.SetCurrentDirectory("/Projects/BlackBox/game");
            GlobalStats.XRES = (int)ScreenSize.X; // Required for DrawLine...
            GlobalStats.YRES = (int)ScreenSize.Y;
            ScreenCenter = ScreenSize * 0.5f;
            IsFixedTimeStep = false;
        }
        
        protected override void BeginRun()
        {
            base.BeginRun();
            Fonts.LoadFonts(Content, GlobalStats.Language);
            ResourceManager.CreateCoreGfxResources();
            Started.Set();
        }

        protected override void Update(float deltaTime)
        {
            if (CachedVisibility != Visible)
            {
                CachedVisibility = Visible;
                Form.Visible = Visible;
            }

            // Always Update, even if game is not visible
            // Let the ScreenManager/GameScreen system figure out what to do
            base.Update(deltaTime);

            if (Input.IsKeyDown(Keys.Escape))
                Visible = false;
        }

        protected override void Draw()
        {
            // Always Draw, even if game is not visible
            // Because we want our unit tests to go through the entire system
            ScreenManager.UpdateGraphicsDevice();
            ScreenManager.ClearScreen(new Color(40,40,40));

            try
            {
                Batch.SafeBegin();
                ScreenManager.Draw();
                DrawComponents();
            }
            finally
            {
                Batch.SafeEnd();
            }
        }

        public void AddComponent(TestGameComponent component)
        {
            component.Game = this;
            Components.Add(component);
        }

        public void RemoveComponent(TestGameComponent component)
        {
            Components.Remove(component);
        }

        public void DrawText(float x, float y, string text)
        {
            Batch.DrawString(Fonts.Arial14Bold, text, new Vector2(x,y), Color.White);
        }

        public void DrawText(float x, float y, string text, Color color)
        {
            Batch.DrawString(Fonts.Arial14Bold, text, new Vector2(x,y), color);
        }

        public void ShowAndRun(TestGameComponent component = null,
                               GameScreen screen = null)
        {
            if (component != null)
                AddComponent(component);
            if (screen != null)
                ScreenManager.AddScreenAndLoadContent(screen);

            Visible = true;
            while (Visible)
            {
                if (component is { Visible: false } || screen is { Visible: false })
                    break;
                RunOne();
                Tick();
            }
            
            if (component != null)
                RemoveComponent(component);
            if (screen != null)
                ScreenManager.RemoveScreen(screen);

            Thread.Sleep(100); // ughh, some weird window related bug
        }
    }
}