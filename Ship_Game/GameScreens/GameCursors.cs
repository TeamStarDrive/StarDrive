using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Ship_Game.GameScreens
{
    public static class GameCursors
    {
        // The Standard game Cursor
        public static Cursor Regular;
        // Standard Cursor for WayPoints
        public static Cursor RegularNav;

        // Miniature cursor for Cinematic Universe View
        public static Cursor Cinematic;

        static Cursor CurrentCursor;
        static Form TargetForm;

        public static void Initialize(GameBase game)
        {
            Regular    = LoadCursor("Cursors/Cursor.png", 0.5f, 0.5f);
            RegularNav = LoadCursor("Cursors/CursorNav.png", 0f, 0f);
            Cinematic  = LoadCursor("Cursors/CinematicCursor.png", 0.5f, 0.5f);

            TargetForm = game.Form;
            TargetForm.Cursor = Regular;
            CurrentCursor = Regular;
            game.IsMouseVisible = true;
        }

        public static void SetCurrentCursor(Cursor cursor)
        {
            if (CurrentCursor != cursor)
            {
                CurrentCursor = cursor;
                TargetForm.Cursor = cursor;
            }
        }

        static Cursor LoadCursor(string fileName, float hotSpotX, float hotSpotY)
        {
            FileInfo file = ResourceManager.GetModOrVanillaFile(fileName);
            if (file == null)
                return Regular ?? Cursors.Default;
            // useIcm: to use color correction for this Bitmap
            var bitmap = new Bitmap(file.FullName, useIcm: true);
            //var cursor = new Cursor(bitmap.GetHicon());
            int hotX = (int)(bitmap.Width * hotSpotX);
            int hotY = (int)(bitmap.Height * hotSpotY);
            var cursor = CreateCursorNoResize(bitmap, hotX, hotY);
            return cursor;
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
