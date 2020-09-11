using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;

namespace Ship_Game.Spatial.Native
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct NativeQtreeNode
    {
        public float X, Y, LastX, LastY;
        public NativeQtreeNode* NW;
        public NativeQtreeNode* NE;
        public NativeQtreeNode* SE;
        public NativeQtreeNode* SW;
        public int Count;
        public int Capacity;
        public NativeSpatialObj* Items;
        public int Id;
        public int Level;
        public int TotalTreeDepthCount;


        public bool Overlaps(in Vector2 topLeft, in Vector2 botRight)
        {
            return X <= botRight.X && LastX > topLeft.X
                && Y <= botRight.Y && LastY > topLeft.Y;
        }
    }
}
