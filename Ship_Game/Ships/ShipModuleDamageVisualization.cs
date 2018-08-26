using Microsoft.Xna.Framework;
using Particle3DSample;

namespace Ship_Game.Ships
{
    public class ShipModuleDamageVisualization
    {
        private readonly ParticleEmitter Lightning;
        private readonly ParticleEmitter Trail;
        private readonly ParticleEmitter Smoke;
        private readonly ParticleEmitter Flame;
        private readonly float LightningVelZ;
        private readonly float Area;


        public static bool CanVisualize(ShipModule module)
        {
            int area = module.XSIZE * module.YSIZE;
            if (module.ModuleType == ShipModuleType.Armor && area <= 1)
                return false; // Small armor modules don't have damage visualization
            return true;
        }

        public ShipModuleDamageVisualization(ShipModule module)
        {
            Area                = module.XSIZE * module.YSIZE;
            Vector3 center      = module.GetCenter3D;            
            ShipModuleType type = module.ModuleType;
            
            float modelZ = module.GetParent().BaseHull.ModelZ;
            modelZ = modelZ.Clamped(0, 200) * -1;
            switch (type) // FB: other special effects based on some module types, use main moduletypes for performance sake
            {
                case ShipModuleType.Shield:
                    Lightning = Empire.Universe.sparks.NewEmitter(40f, center.ToVec2(), -10f + modelZ);
                    LightningVelZ = -6f;
                    break;
                case ShipModuleType.PowerConduit:
                    Lightning = Empire.Universe.sparks.NewEmitter(25f, center.ToVec2(), -10f + modelZ);
                    LightningVelZ = -3f;
                    return;
                case ShipModuleType.PowerPlant:
                    Lightning = Empire.Universe.photonExplosionParticles.NewEmitter(Area * 6f, center, modelZ);
                    LightningVelZ = -4;
                    break;
            }

            // after all the special cases and removing irrelevant modules, we come to smoke emitters
            Trail = Empire.Universe.smokePlumeParticles.NewEmitter(Area * 0.7f , center, modelZ);
            Smoke = Empire.Universe.explosionSmokeParticles.NewEmitter(Area * 3f, center, modelZ);

            // armor doesnt produce flames. 
            if (type == ShipModuleType.Armor)
                return;
            Flame = Area >= 15f ? Empire.Universe.flameParticles.NewEmitter(Area * 2 * GlobalStats.DamageIntensity , center, modelZ) : 
                                 Empire.Universe.SmallflameParticles.NewEmitter(Area * 3f * GlobalStats.DamageIntensity, center, modelZ);
        }

        // This is called when module is OnFire or completely dead
        public void Update(float elapsedTime, Vector3 center, bool isAlive)
        {
            Lightning?.Update(elapsedTime, center, zVelocity: LightningVelZ);
            //added zaxis offeset contructor. bury flame into model a bit. 
            Flame?.Update(elapsedTime, center, zVelocity: Area / -2  - RandomMath.RandomBetween(2f, 6f), zAxisPos: Area, jitter: Area * 2);

            // only spawn smoke from dead modules
            if (isAlive) return;
            Trail?.Update(elapsedTime, center, zVelocity: -0.1f);
            Smoke?.Update(elapsedTime, center, zVelocity: -2.0f);
        }
    }
}
