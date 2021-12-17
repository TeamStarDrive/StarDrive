using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Graphics.Particles;

namespace Ship_Game.Ships
{
    public class EngineTrail
    {
        public static void Update(ParticleManager particles,
                                  in Vector3 pos,
                                  in Vector3 forwardDir,
                                  float thrustScale, // size on screen
                                  float thrustPower, // velocity multiplier
                                  Color thrust1,
                                  Color thrust2)
        {
            Vector3 thrustDir = -forwardDir;
            Vector3 thrustVel = thrustDir * thrustPower;
            
            var thrustFx = particles.ThrustEffect;
            
            for (int x = 0; x < 3; ++x)
            {
                thrustFx.AddParticle(pos + thrustDir*(x*3f), thrustVel, thrustScale, thrust2);
            }

            var trailFx = particles.EngineTrail;
            Vector3 trailOffset = thrustDir*16f;
            trailFx.AddParticle(pos + trailOffset, thrustVel*0.5f, thrustScale, thrust1);
        }
    }
}
