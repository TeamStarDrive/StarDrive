using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SynapseGaming.LightingSystem.Lights;

namespace Ship_Game
{
    // help make scene object manager easier and less error prone
    public static class SceneExt
    {
        // @todo Maybe change this to AddToUniverse?
        public static void AddTo(this ILight light, GameScreen screen)
        {
            // @todo Replace with specialized locks
            lock (GlobalStats.ObjectManagerLocker)
                screen.ScreenManager.Submit(light);
        }

        public static void RemoveFrom(this ILight light, GameScreen screen)
        {
            lock (GlobalStats.ObjectManagerLocker)
                screen.ScreenManager.Remove(light);
        }

        public static void Refresh(this ILight light, GameScreen screen)
        {
            lock (GlobalStats.ObjectManagerLocker)
            {
                screen.ScreenManager.Remove(light);
                screen.ScreenManager.Submit(light);
            }
        }

        public static void AssignTo(this LightRig rig, GameScreen screen)
        {
            lock (GlobalStats.ObjectManagerLocker)
            {
                screen.ScreenManager.inter.LightManager.Clear();
                screen.ScreenManager.inter.LightManager.Submit(rig);
            }
        }
    }
}
