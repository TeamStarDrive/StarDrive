using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Ship_Game.Gameplay;
using Microsoft.Xna.Framework;
using SynapseGaming.LightingSystem.Rendering;


namespace Ship_Game
{
    class DevekSplash : ZeroSplash
    {
        public void Update2(GameTime gameTime)
        {
            foreach (var type in typeof(SplashScreen).GetFields(BindingFlags.Static | BindingFlags.NonPublic).Where(type => type.GetValue(null) is bool && type.Name == "a"))
            {
                type.SetValue(null, true);
                return;
            }
        }

        // ReSharper disable once InconsistentNaming
        public static void k2()
        {

        }
    }
}
