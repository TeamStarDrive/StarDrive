using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
    public static class Colors
    {
        //Standard Colors used in universeScreen
        //Line Colors for selected ship(s) tarets and desitingations
        public static Color WayPoints(byte alpha = 255)     => new Color(Color.Lime, alpha);         //waypoint lines
        public static Color Attack(byte alpha = 255)        => new Color(Color.Red, alpha);          //attacking some object
        public static Color CombatOrders(byte alpha = 255)  => new Color(Color.MediumPurple, alpha); //doing an offensive ation generally requiring war
        public static Color Orders(byte alpha = 255)        => new Color(Color.Aqua, alpha);         //non offensive ship orders
        public static Color Error(byte alpha = 255)         => new Color(Color.Orange, alpha);       //indicate to user that command will fail
        public static Color Warning(byte alpha = 255)       => new Color(Color.Yellow, alpha);       //indicate to user that command may fail


        // multiplies color R,G,B with multiplier, leaving alpha untouched
        public static Color MultiplyRgb(this Color color, float multiplier)
        {
            byte r = (byte)(color.R * multiplier).Clamped(0, 255);
            byte g = (byte)(color.G * multiplier).Clamped(0, 255);
            byte b = (byte)(color.B * multiplier).Clamped(0, 255);
            return new Color(r, g, b, color.A);
        }

        // adds [0.0-1.0] float to existing color.
        // where 1.0 == 255, and 0.5 == 128
        public static Color AddRgb(this Color color, float add)
        {
            byte r = (byte)(color.R + add*255f).Clamped(0, 255);
            byte g = (byte)(color.G + add*255f).Clamped(0, 255);
            byte b = (byte)(color.B + add*255f).Clamped(0, 255);
            return new Color(r, g, b, color.A);
        }
    }
}
