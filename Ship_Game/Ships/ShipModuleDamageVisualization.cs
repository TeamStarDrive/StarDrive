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
                case ShipModuleType.Shield: 
                    Lightning     = Empire.Universe.photonExplosionParticles.NewEmitter(Area * 6f, center);
                    LightningVelZ = -3;
                    return;
                case ShipModuleType.PowerPlant:
                    if (Area >= 4)
                    {
                        Lightning = Empire.Universe.lightning.NewEmitter(8f, center);
                        LightningVelZ = -6f;
                    }
                    break;
                case ShipModuleType.PowerConduit:  // power conduit get only sparks
                    Lightning     = Empire.Universe.sparks.NewEmitter(25f, center.ToVec2(), -10f);
                    LightningVelZ = -2f;
                    return;
            }

            // after all the special cases and removing irrelevant modules, we come to smoke emitters
            Trail = Empire.Universe.smokePlumeParticles.NewEmitter(Area * 0.7f , center);
            Smoke = Empire.Universe.explosionSmokeParticles.NewEmitter(Area * 3f, center, -80f);

            // armor doesnt produce flames. 
            if (type == ShipModuleType.Armor)
                return;
            Flame = Area >= 15f ? Empire.Universe.flameParticles.NewEmitter(Area * 2 , center) : 
                                 Empire.Universe.SmallflameParticles.NewEmitter(Area * 3f , center, -7f);
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
