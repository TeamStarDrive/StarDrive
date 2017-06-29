using Microsoft.Xna.Framework;
using System;
using System.Runtime.CompilerServices;
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
    }
}
