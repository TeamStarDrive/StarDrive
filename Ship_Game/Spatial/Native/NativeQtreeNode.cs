using System;
using System.Runtime.InteropServices;

namespace Ship_Game.Spatial.Native
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NativeQtreeNode
    {
        public NativeQtreeNode* Nodes;
        public int Size;
        public NativeQtreeObject* Items;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NativeBoundedQtreeNode
    {
        public NativeQtreeNode* Node;
        public int X, Y;
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NativeQtreeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
