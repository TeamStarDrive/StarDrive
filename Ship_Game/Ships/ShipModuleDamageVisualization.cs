using Microsoft.Xna.Framework;
using Ship_Game.Graphics.Particles;

namespace Ship_Game.Ships
{
    public class ShipModuleDamageVisualization
    {
        readonly ParticleEmitter Lightning;
        readonly ParticleEmitter Dust;
        readonly ParticleEmitter Smoke;
        readonly ParticleEmitter Flame;

        public static bool CanVisualize(ShipModule module)
        {
            int area = module.XSize * module.YSize;
            if (module.ModuleType == ShipModuleType.Armor && area <= 1)
                return false; // Small armor modules don't have damage visualization
            return true;
        }

        public ShipModuleDamageVisualization(ShipModule module)
        {
            float area = module.Area;
            Vector3 center = module.Center3D;
            ShipModuleType type = module.ModuleType;

            ParticleManager particles = Empire.Universe.Particles;

            switch (type) // FB: other special effects based on some module types, use main moduletypes for performance sake
            {
                case ShipModuleType.Shield: Lightning = particles.Lightning.NewEmitter(4, center, 0.1f*area); break;
                case ShipModuleType.PowerPlant: Lightning = particles.PhotonExplosion.NewEmitter(area, center, 0.25f); break;
                case ShipModuleType.PowerConduit: Lightning = particles.Sparks.NewEmitter(12, center); return; // no other effects!
            }

            // after all the special cases and removing irrelevant modules, we come to smoke emitters
            Dust = particles.SmokePlume.NewEmitter(0.5f, center);
            Smoke = particles.ExplosionSmoke.NewEmitter(0.5f, center);

            // armor doesnt produce flames
            if (type != ShipModuleType.Armor)
            {
                Flame = area >= 8f ? particles.Fire.NewEmitter(14 * GlobalStats.DamageIntensity, center) :
                                      particles.ModuleSmoke.NewEmitter(20 * GlobalStats.DamageIntensity, center);
            }
        }

        // This is called when module is OnFire /or/ completely dead
        public void Update(FixedSimTime timeStep, in Vector3 center, float scale, bool isAlive)
        {
            // the module is on fire
            Flame?.Update(timeStep.FixedTime, center, zVelocity: -4f, scale: scale);
            Lightning?.Update(timeStep.FixedTime, center, zVelocity: -4f, scale: scale);

            // only spawn smoke from dead modules
            if (!isAlive)
            {
                Dust?.Update(timeStep.FixedTime, center, zVelocity: -0.1f, scale: scale);
                Smoke?.Update(timeStep.FixedTime, center, zVelocity: -2f, scale: scale);
            }
        }
    }
}
