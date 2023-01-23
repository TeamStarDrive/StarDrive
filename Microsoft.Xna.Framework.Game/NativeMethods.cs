// Decompiled with JetBrains decompiler
// Type: Microsoft.Xna.Framework.NativeMethods
// Assembly: Microsoft.Xna.Framework.Game, Version=3.1.0.0, Culture=neutral, PublicKeyToken=6d5c3888ef60e27d
// MVID: E4BD910E-73ED-465E-A91E-14AAAB0CE109
// Assembly location: C:\WINDOWS\assembly\GAC_32\Microsoft.Xna.Framework.Game\3.1.0.0__6d5c3888ef60e27d\Microsoft.Xna.Framework.Game.dll

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Microsoft.Xna.Framework
{
    internal static class NativeMethods
    {
        [SuppressUnmanagedCodeSecurity]
        [DllImport("kernel32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool QueryPerformanceFrequency(out long PerformanceFrequency);

        [SuppressUnmanagedCodeSecurity]
        [DllImport("kernel32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool QueryPerformanceCounter(out long PerformanceCount);

        [SuppressUnmanagedCodeSecurity]
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool PeekMessage(out Message msg, IntPtr hWnd, uint messageFilterMin, uint messageFilterMax, uint flags);

        [SuppressUnmanagedCodeSecurity]
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

        [SuppressUnmanagedCodeSecurity]
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetClientRect(IntPtr hWnd, out RECT rect);

        //[SuppressUnmanagedCodeSecurity]
        //[DllImport("user32.dll")]
        //[return: MarshalAs(UnmanagedType.Bool)]
        //internal static extern bool ClientToScreen(IntPtr hWnd, out POINT point);

        [SuppressUnmanagedCodeSecurity]
        [DllImport("user32.dll")]
        internal static extern IntPtr MonitorFromWindow(IntPtr hWnd, uint flags);

        internal enum WindowMessage : uint
        {
            Destroy                 = 2,
            Size                    = 5,
            Paint                   = 15,
            Close                   = 16,
            Quit                    = 18,
            ActivateApplication     = 28,
            SetCursor               = 32,
            GetMinMax               = 36,
            NonClientHitTest        = 132,
            KeyDown                 = 256,
            KeyUp                   = 257,
            Character               = 258,
            SystemKeyDown           = 260,
            SystemKeyUp             = 261,
            SystemCharacter         = 262,
            SystemCommand           = 274,
            MouseMove               = 512,
            LeftButtonDown          = 513,
            MouseFirst              = 513,
            LeftButtonUp            = 514,
            LeftButtonDoubleClick   = 515,
            RightButtonDown         = 516,
            RightButtonUp           = 517,
            RightButtonDoubleClick  = 518,
            MiddleButtonDown        = 519,
            MiddleButtonUp          = 520,
            MiddleButtonDoubleClick = 521,
            MouseWheel              = 522,
            XButtonDown             = 523,
            XButtonUp               = 524,
            MouseLast               = 525,
            XButtonDoubleClick      = 525,
            EnterMenuLoop           = 529,
            ExitMenuLoop            = 530,
            PowerBroadcast          = 536,
            EnterSizeMove           = 561,
            ExitSizeMove            = 562,
        }

        public enum MouseButtons
        {
            Left   = 1,
            Right  = 2,
            Middle = 16,
            Side1  = 32,
            Side2  = 64,
        }

        public struct Message
        {
            public IntPtr hWnd;
            public WindowMessage msg;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public System.Drawing.Point p;
        }

        //public struct MinMaxInformation
        //{
        //    public System.Drawing.Point reserved;
        //    public System.Drawing.Point MaxSize;
        //    public System.Drawing.Point MaxPosition;
        //    public System.Drawing.Point MinTrackSize;
        //    public System.Drawing.Point MaxTrackSize;
        //}

        //public struct MonitorInformation
        //{
        //    public uint Size;
        //    public System.Drawing.Rectangle MonitorRectangle;
        //    public System.Drawing.Rectangle WorkRectangle;
        //    public uint Flags;
        //}

        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        //public struct POINT
        //{
        //    public int X;
        //    public int Y;
        //}
    }
}
