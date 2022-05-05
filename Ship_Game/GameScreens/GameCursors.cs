using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Graphics.Color;
using SDGraphics;

namespace Ship_Game.GameScreens
{
    public class GameCursor
    {
        public Cursor OSCursor;
        public Texture2D SoftwareCursor;
        public Vector2 HotSpot;
    }

    public static class GameCursors
    {
        // fallback OS cursor
        public static GameCursor DefaultOSCursor;
        
        // The Standard game Cursors
        public static GameCursor Regular;
        public static GameCursor RegularNav; // WayPoints

        // Miniature cursor for Cinematic Universe View
        public static GameCursor Cinematic;

        // Aggressive Stance Cursor
        public static GameCursor Aggressive;
        public static GameCursor AggressiveNav;

        // StandGround Stance Cursor
        public static GameCursor StandGround;
        public static GameCursor StandGroundNav;

        static GameCursor CurrentCursor;
        static Cursor CurrentOSCursor;
        static Form TargetForm;

        public static void Initialize(GameBase game, bool software)
        {
            DefaultOSCursor = LoadCursor(game, software:false, "Cursors/Regular.png");
            if (DefaultOSCursor == null)
                throw new NullReferenceException("GameCursors.Initialize: Default OS Cursor cannot be null! [Cursors/Cursor.png]");

            Regular    = LoadCursor(game, software, "Cursors/Regular.png");
            RegularNav = LoadCursor(game, software, "Cursors/RegularNav.png");
            Cinematic  = LoadCursor(game, software, "Cursors/Cinematic.png", 0.5f, 0.5f);

            Aggressive    = LoadCursor(game, software, "Cursors/Aggressive.png");
            AggressiveNav = LoadCursor(game, software, "Cursors/AggressiveNav.png");

            StandGround    = LoadCursor(game, software, "Cursors/StandGround.png");
            StandGroundNav = LoadCursor(game, software, "Cursors/StandGroundNav.png");

            TargetForm = game.Form;
            CurrentCursor = Regular;
            game.IsMouseVisible = !software;
        }

        public static void SetCurrentCursor(GameCursor cursor)
        {
            CurrentCursor = cursor;
        }

        public static void Draw(GameBase game, SpriteBatch batch, Vector2 cursorScreenPos, bool software)
        {
            if (DefaultOSCursor == null)
                return; // unit tests don't load cursors

            // attempt to draw software cursor, if that fails, draw OS cursor instead
            if (software && CurrentCursor.SoftwareCursor?.IsDisposed == false)
            {
                game.IsMouseVisible = false;
                batch.Begin();
                batch.Draw(CurrentCursor.SoftwareCursor, cursorScreenPos, null, Color.White, 0f, 
                           CurrentCursor.HotSpot, 1f, SpriteEffects.None, 1f);
                batch.End();
            }
            else
            {
                game.IsMouseVisible = true;
                var osCursor = CurrentCursor.OSCursor ?? DefaultOSCursor.OSCursor;
                if (CurrentOSCursor != osCursor)
                {
                    CurrentOSCursor = osCursor;
                    TargetForm.Cursor = osCursor;
                }
            }
        }

        static GameCursor LoadCursor(GameBase game, bool software, string fileName, float hotSpotX=0f, float hotSpotY=0f)
        {
            FileInfo file = ResourceManager.GetModOrVanillaFile(fileName);
            if (file == null)
            {
                Log.Error($"GameCursors.LoadCursor failed: {fileName} not found!");
                return Regular;
            }

            var wrappedCursor = new GameCursor();
            if (software)
            {
                Texture2D texture = game.Content.LoadTexture(file);
                wrappedCursor.SoftwareCursor = texture;
                wrappedCursor.HotSpot = new Vector2(hotSpotX*texture.Width, hotSpotY*texture.Height);
            }
            else
            {
                // useIcm: to use color correction for this Bitmap
                var bitmap = new Bitmap(file.FullName, useIcm: true);
                //var cursor = new Cursor(bitmap.GetHicon());
                int hotX = (int)(bitmap.Width * hotSpotX);
                int hotY = (int)(bitmap.Height * hotSpotY);
                var cursor = CreateCursorNoResize(bitmap, hotX, hotY);
                wrappedCursor.OSCursor = cursor;
                wrappedCursor.HotSpot = new Vector2(hotX, hotY);
            }
            return wrappedCursor;
        }

        public struct IconInfo
        {
            public bool fIcon;
            public int xHotspot;
            public int yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetIconInfo(IntPtr hIcon, ref IconInfo pIconInfo);

        [DllImport("user32.dll")]
        public static extern IntPtr CreateIconIndirect(ref IconInfo icon);

        /// <summary>
        /// Create a cursor from a bitmap without resizing and with the specified hot spot
        /// </summary>
        public static Cursor CreateCursorNoResize(Bitmap bmp, int xHotSpot, int yHotSpot)
        {
            IntPtr ptr = bmp.GetHicon();
            IconInfo tmp = new IconInfo();
            GetIconInfo(ptr, ref tmp);
            tmp.xHotspot = xHotSpot;
            tmp.yHotspot = yHotSpot;
            tmp.fIcon = false; // FALSE: cursor, TRUE: icon
            ptr = CreateIconIndirect(ref tmp);
            return new Cursor(ptr);
        }
    }
}
