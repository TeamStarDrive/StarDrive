using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;

namespace Ship_Game.Spatial.Native
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct NativeSpatialObj
    {
        public byte PendingRemove; // 1 if this item is pending removal
        public byte Loyalty;       // if loyalty == 0, then this is a STATIC world object !!!
        public byte Type;          // GameObjectType : byte
        public byte Reserved;

        public int ObjectId; // the object

        public float CX, CY; // Center x y
        public float Radius; // radius for collision test
        public float X1, Y1, X2, Y2; // bounding box of this spatial obj

        public NativeSpatialObj(GameplayObject go, int objectId)
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
                X1 = Math.Min(source.X, target.X);
                Y1 = Math.Min(source.Y, target.Y);
                X2 = Math.Max(source.X, target.X);
                Y2 = Math.Max(source.Y, target.Y);
                CX = 0f;
                CY = 0f;
                Radius = 0f;
            }
            else
            {
                CX = go.Center.X;
                CY = go.Center.Y;
                Radius = go.Radius;
                X1 = CX - Radius;
                Y1 = CY - Radius;
                X2 = CX + Radius;
                Y2 = CY + Radius;
            }
        }
    }
}
