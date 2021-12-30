using Microsoft.Xna.Framework;
using Ship_Game.Graphics.Particles;

namespace Ship_Game.Ships
{
    public class ShipModuleDamageVisualization
    {
        private readonly ParticleEmitter Lightning;
        private readonly ParticleEmitter Dust;
        private readonly ParticleEmitter Smoke;
        private readonly ParticleEmitter Flame;
        private readonly float LightningVelZ;
        private readonly float Area;

        public static bool CanVisualize(ShipModule module)
        {
            int area = module.XSize * module.YSize;
            if (module.ModuleType == ShipModuleType.Armor && area <= 1)
                return false; // Small armor modules don't have damage visualization
            return true;
        }

        public ShipModuleDamageVisualization(ShipModule module)
        {
            Area = module.XSize * module.YSize;
            Vector3 center = module.Center3D;
            ShipModuleType type = module.ModuleType;

            ParticleManager particles = Empire.Universe.Particles;

            switch (type) // FB: other special effects based on some module types, use main moduletypes for performance sake
            {
                case ShipModuleType.Shield:
                    Lightning = particles.Sparks.NewEmitter(40f, center);
                    LightningVelZ = -6f;
                    break;
                case ShipModuleType.PowerConduit:
                    Lightning = particles.Sparks.NewEmitter(25f, center);
                    LightningVelZ = -3f;
                    return;
                case ShipModuleType.PowerPlant:
                    Lightning = particles.PhotonExplosion.NewEmitter(Area * 6f, center);
                    LightningVelZ = -4;
                    break;
            }

            // after all the special cases and removing irrelevant modules, we come to smoke emitters
            Dust = particles.SmokePlume.NewEmitter(Area * 0.7f, center);
            Smoke = particles.ExplosionSmoke.NewEmitter(Area * 3f, center);

            // armor doesnt produce flames. 
            if (type != ShipModuleType.Armor)
            {
                Flame = Area >= 15f ? particles.Flame.NewEmitter(Area * 2 * GlobalStats.DamageIntensity, center) :
                                      particles.SmallFlame.NewEmitter(Area * 3f * GlobalStats.DamageIntensity, center);
            }
        }

        // This is called when module is OnFire or completely dead
        public void Update(FixedSimTime timeStep, Vector3 center, bool isAlive)
        {
            Lightning?.Update(timeStep.FixedTime, center, zVelocity: LightningVelZ);
            //added zaxis offeset contructor. bury flame into model a bit. 
            Flame?.Update(timeStep.FixedTime, center, zVelocity: Area / -2 - RandomMath.RandomBetween(2f, 6f), jitter: Area * 2);

            // only spawn smoke from dead modules
            if (isAlive) return;
            Dust.Update(timeStep.FixedTime, center, zVelocity: -0.1f);
            Smoke.Update(timeStep.FixedTime, center, zVelocity: -2.0f);
        }
    }
}
