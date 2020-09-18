using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;

namespace Ship_Game.Spatial.Native
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct NativeQtreeObject
    {
        public byte PendingRemove; // 1 if this item is pending removal
        public byte Loyalty;       // if loyalty == 0, then this is a STATIC world object !!!
        public byte Type;          // GameObjectType : byte
        public byte Reserved;

        public int ObjectId; // the object

        public int X, Y; // Center x y
        public int Radius; // radius for collision test

        public NativeQtreeObject(GameplayObject go, int objectId)
        {
            PendingRemove = 0;
            Loyalty  = (byte)go.GetLoyaltyId();
            Type     = (byte)go.Type;
            Reserved = 0;
            ObjectId = objectId;

            if (Type == (byte)GameObjectType.Beam)
            {
                var beam = (Beam)go;
                Vector2 source = beam.Source;
                Vector2 target = beam.Destination;
                int x1 = (int)Math.Min(source.X, target.X);
                int y1 = (int)Math.Min(source.Y, target.Y);
                int x2 = (int)Math.Max(source.X, target.X);
                int y2 = (int)Math.Max(source.Y, target.Y);
                X = (x1 + x2) >> 1;
                Y = (y1 + y2) >> 1;
                Radius = Math.Max(x2-x1, y2-y1) >> 1;
            }
            else
            {
                X = (int)go.Center.X;
                Y = (int)go.Center.Y;
                Radius = (int)go.Radius;
                //X1 = CX - Radius;
                //Y1 = CY - Radius;
                //X2 = CX + Radius;
                //Y2 = CY + Radius;
            }
        }
    }
}
