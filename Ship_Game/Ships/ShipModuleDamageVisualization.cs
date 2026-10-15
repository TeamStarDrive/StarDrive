using Ship_Game.Graphics.Particles;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;

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

        public ShipModuleDamageVisualization(ShipModule module, ParticleManager p)
            : this(module.Area, module.Center3D, module.ModuleType, p)
        {
        }

        public ShipModuleDamageVisualization(float area, in Vector3 center, ShipModuleType type, ParticleManager p)
        {
            switch (type) // FB: other special effects based on some module types, use main moduletypes for performance sake
            {
                case ShipModuleType.Shield: Lightning = p.Lightning.NewEmitter(40, center, 0.1f*area); break;
                case ShipModuleType.PowerPlant: Lightning = p.PhotonExplosion.NewEmitter(area * 6f, center, 0.25f); break;
                case ShipModuleType.PowerConduit: Lightning = p.Sparks.NewEmitter(25, center); return; // no other effects!
            }

            Dust = p.SmokePlume.NewEmitter(area * 0.7f, center);

            bool smokeOnly = p.Random.Int(0, 1) == 1;
            Smoke = p.ExplosionSmoke.NewEmitter(area * 3f, center);

            // armor doesnt produce flames
            if (!smokeOnly && type != ShipModuleType.Armor)
            {
                float intensity = GlobalStats.Defaults.ModuleDamageVisualIntensity;
                Flame = area >= 6 ? p.Fire.NewEmitter(14 * intensity, center)
                                  : p.ModuleSmoke.NewEmitter(20 * intensity, center);
            }
        }

        // This is called when module is OnFire /or/ completely dead.
        // zVelocity is in units/sec; small values keep VFX anchored to the hull.
        public void Update(FixedSimTime timeStep, in Vector3 center, float scale, bool isAlive)
        {
            Lightning?.Update(timeStep.FixedTime, center, zVelocity: -5f, scale: scale);
            Flame?.Update(timeStep.FixedTime, center, zVelocity: -5f, scale: scale);

            // only spawn smoke from dead modules
            if (!isAlive)
            {
                Dust?.Update(timeStep.FixedTime, center, zVelocity: -0.5f, scale: scale);
                Smoke?.Update(timeStep.FixedTime, center, zVelocity: -3f, scale: scale);
            }
        }
    }
}
