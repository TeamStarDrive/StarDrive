using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Lights;

using System;
using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed class Shield
    {
        public static GameContentManager content;

        public float texscale;

        public float displacement;

        public Matrix World;

        public float Radius;

        //public Matrix View;          //Not referenced in code, removing to save memory

        //public Matrix Projection;          //Not referenced in code, removing to save memory

        public float Rotation;

        public GameplayObject Owner;

        public Vector3 Center;

        public Model shieldModel;

        public Texture2D shieldTexture;

        public Texture2D gradientTexture;

        public Effect ShieldEffect;

        public PointLight pointLight = new PointLight();

        public bool LightAdded = false;

        public Shield()
        {
        }

        public void AddLight()
        {
            if (LightAdded)                           
                return;
            
            LightAdded = true;
            Empire.Universe.AddLight(pointLight);
        }

        public void RemoveLight()
        {
            LightAdded = false;
            Empire.Universe.RemoveLight(pointLight);
        }

        public void LoadContent()
        {
            this.shieldModel = Shield.content.Load<Model>("Model/Projectiles/shield");
            this.shieldTexture = Shield.content.Load<Texture2D>("Model/Projectiles/shield_d");
            this.gradientTexture = Shield.content.Load<Texture2D>("Model/Projectiles/shieldgradient");
            this.ShieldEffect = Shield.content.Load<Effect>("Effects/scale");
            
        }

        public void HitShield(GameplayObject source)
        {
            AddLight();
            Rotation = source.Rotation - 3.14159274f;
            displacement = 0f;
            texscale = 2.8f;
            
        }

        public void HitShield(ShipModule hitPoint, Beam beam)
        {
            float intensity = (10f).Clamp(1, beam.DamageAmount / hitPoint.ShieldPower);
            AddLight();
            Rotation                = hitPoint.Center.RadiansToTarget(beam.Source);
            pointLight.World        = Matrix.CreateTranslation(new Vector3(beam.ActualHitDestination, 0f));
            pointLight.DiffuseColor = new Vector3(0.5f, 0.5f, 1f);
            pointLight.Radius       = hitPoint.shield_radius * 2f;
            pointLight.Intensity    = RandomMath.RandomBetween(intensity *.5f, 10f);
            displacement            = 0f;
            Radius                  = hitPoint.ShieldHitRadius;
            displacement            = 0.085f * RandomMath.RandomBetween(intensity, 10f);
            texscale                = 2.8f;
            texscale                = 2.8f - 0.185f * RandomMath.RandomBetween(intensity, 10f);
            pointLight.Enabled      = true;
            if (RandomMath.RandomBetween(0f, 100f) > 90f && hitPoint.GetParent().InFrustum)
            {
                Empire.Universe.flash.AddParticleThreadA(new Vector3(beam.ActualHitDestination, hitPoint.GetCenter3D.Z), Vector3.Zero);
            }
            if (hitPoint.GetParent().InFrustum)
            {
                Vector2 vel = (beam.Source - hitPoint.Center).Normalized();
                for (int i = 0; i < 20; i++)
                {
                    Empire.Universe.sparks.AddParticleThreadA(new Vector3(beam.ActualHitDestination, hitPoint.GetCenter3D.Z), new Vector3(vel * RandomMath.RandomBetween(40f, 80f), RandomMath.RandomBetween(-25f, 25f)));
                }
            }

        }
        public void HitShield(ShipModule hitPoint, Projectile proj)
        {
            AddLight();
            float intensity = (10f).Clamp(1, proj.DamageAmount / hitPoint.ShieldPower);
            GameAudio.PlaySfxAsync("sd_impact_shield_01", hitPoint.GetParent().SoundEmitter);
            Radius = hitPoint.ShieldHitRadius;
            displacement = 0.085f * RandomMath.RandomBetween(intensity *.5f, 10f);
            texscale = 2.8f;
            texscale = 2.8f - 0.185f * RandomMath.RandomBetween(intensity, 10f);
            pointLight.World = proj.WorldMatrix;
            pointLight.DiffuseColor = new Vector3(0.5f, 0.5f, 1f);
            pointLight.Radius = Radius;
            pointLight.Intensity = 8f;
            pointLight.Enabled = true;
            // this can use the beam class method.
            Vector2 vel = proj.Center - hitPoint.Center.Normalized();
            Empire.Universe.flash.AddParticleThreadB(new Vector3(proj.Center, hitPoint.GetCenter3D.Z), Vector3.Zero);
            for (int i = 0; i < 20; i++)
            {
                Empire.Universe.sparks.AddParticleThreadB(new Vector3(proj.Center, hitPoint.GetCenter3D.Z)
                    , new Vector3(vel * RandomMath.RandomBetween(40f, 80f), RandomMath.RandomBetween(-25f, 25f)));
            }


        }
    }
}