using System;
using System.Collections.Generic;
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

            switch (type) // other special effects based on some module types.
            {
                case ShipModuleType.FuelCell: 
                    Lightning     = Empire.Universe.photonExplosionParticles.NewEmitter(Area * 5f, center);
                    LightningVelZ = -5;
                    return;
                case ShipModuleType.Shield:
                    Lightning     = Empire.Universe.lightning.NewEmitter(2f, center);
                    LightningVelZ = -8f;
                    break;
                case ShipModuleType.PowerPlant:
                    Lightning     = Empire.Universe.lightning.NewEmitter(8f, center);
                    LightningVelZ = -10f;
                    break;
                case ShipModuleType.PowerConduit:  // power conduit get only sparks
                    Lightning     = Empire.Universe.sparks.NewEmitter(25f, center.ToVec2(), -10f);
                    LightningVelZ = -2f;
                    return;
            }

            // after all the special cases and removing irrelevant modules, we come to smoke emitters
            Trail = Empire.Universe.smokePlumeParticles.NewEmitter(Area, center);
            Smoke = Empire.Universe.explosionSmokeParticles.NewEmitter(Area * 3f, center, -30f);

            // armor doesnt produce flames. 
            if (type != ShipModuleType.Armor &&  Area > 2f)
            {
                Flame = Empire.Universe.flameParticles.NewEmitter(Area * 2, center);
            }
        }

        // This is called when module is OnFire or completely dead
        public void Update(float elapsedTime, Vector3 center, bool isAlive)
        {
            Lightning?.Update(elapsedTime, center, zVelocity: LightningVelZ);
            //added zaxis offeset contructor. bury flame into model a bit. 
            Flame?.Update(elapsedTime, center, zVelocity: Area / -2 - RandomMath.RandomBetween(2f, 6f), zAxisPos: Area, jitter: Area * 2);

            // only spawn smoke from dead modules
            if (isAlive) return;
            Trail?.Update(elapsedTime, center, zVelocity: -0.2f);
            Smoke?.Update(elapsedTime, center, zVelocity: -1.0f);
        }
    }
}
