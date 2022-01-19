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

        // Miniature cursor for Cinematic Universe View
        public static Cursor Cinematic;

        static Cursor CurrentCursor;
        static Form TargetForm;

        public static void Initialize(GameBase game)
        {
            Regular = LoadCursor("Cursors/Cursor.png") ?? Cursors.Default;
            Cinematic = LoadCursor("Cursors/CinematicCursor.png") ?? Cursors.Default;

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

        static Cursor LoadCursor(string fileName)
        {
            FileInfo file = ResourceManager.GetModOrVanillaFile(fileName);
            if (file == null)
                return null;
            // useIcm: to use color correction for this Bitmap
            var bitmap = new Bitmap(file.FullName, useIcm: true);
            //var cursor = new Cursor(bitmap.GetHicon());
            var cursor = CreateCursorNoResize(bitmap, bitmap.Width/2, bitmap.Height/2);
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
