using Microsoft.Xna.Framework.Graphics;
using Ship_Game.Audio;
using Ship_Game.Gameplay;
using SDGraphics;
using Ship_Game.Data.Mesh;

namespace Ship_Game
{
    public sealed class Bomb
    {
        public Vector3 Position;
        public Vector3 Velocity;
        private Planet TargetPlanet;
        public Matrix World { get; private set; }

        public IWeaponTemplate Weapon;

        private ParticleEmitter TrailEmitter;
        private ParticleEmitter FireTrailEmitter;
        public readonly int TroopDamageMin;
        public readonly int TroopDamageMax;
        public readonly int HardDamageMin;
        public readonly int HardDamageMax;
        public readonly float PopKilled;
        public readonly float FertilityDamage;
        public readonly string SpecialAction;
        public Empire Owner;
        private float PlanetRadius;
        public readonly int ShipLevel;
        public readonly float ShipHealthPercent;

        public readonly SubTexture Texture;
        public readonly StaticMesh Model;

        public Bomb(Vector3 position, Empire empire, string weaponName, int shipLevel, float shipHealthPercent)
        {
            Owner = empire;

            const string TextureName = "projBall_02_orange";
            const string ModelName   = "projBall";
            Texture = ResourceManager.ProjTexture(TextureName);
            Model = ResourceManager.ProjectileMesh(ModelName, out var model) ? model : null;
            if (Model == null) Log.Error($"Failed to find Bomb ModelName: {ModelName}");

            Position    = position;
            ShipLevel   = shipLevel;
            Weapon = ResourceManager.GetWeaponTemplate(weaponName)
                  ?? ResourceManager.GetWeaponTemplate("NuclearBomb");

            TroopDamageMin = Weapon.BombTroopDamageMin;
            TroopDamageMax = Weapon.BombTroopDamageMax;
            HardDamageMin  = Weapon.BombHardDamageMin;
            HardDamageMax  = Weapon.BombHardDamageMax;
            PopKilled      = Weapon.BombPopulationKillPerHit;
            FertilityDamage = Weapon.FertilityDamage;
            SpecialAction   = Weapon.HardCodedAction;
            ShipHealthPercent = shipHealthPercent;
        }

        public void DoImpact()
        {
            TargetPlanet.DropBomb(this);
            Owner.Universe.Screen.BombList.QueuePendingRemoval(this);
        }

        private void SurfaceImpactEffects()
        {
            if (Owner.Universe.IsSystemViewOrCloser &&
                TargetPlanet.ParentSystem.InFrustum)
            {
                TargetPlanet.PlayPlanetSfx("sd_bomb_impact_01", Position);
                ExplosionManager.AddExplosionNoFlames(Owner.Universe.Screen, Position, 200f, 7.5f);
                Owner.Universe.Screen.Particles.Flash.AddParticle(Position, Vector3.Zero);
                for (int i = 0; i < 50; i++)
                    Owner.Universe.Screen.Particles.Explosion.AddParticle(Position, Vector3.Zero);
            }
        }

        public void PlayCombatScreenEffects(Planet planet, OrbitalDrop od)
        {
            if (Owner.Universe.Screen.IsViewingCombatScreen(planet))
            {
                GameAudio.PlaySfxAsync("Explo1");
                if (Owner.Universe.Screen.workersPanel is CombatScreen cs)
                    cs.AddExplosion(od.TargetTile.ClickRect, 4);
            }
            else
                SurfaceImpactEffects(); // If viewing the planet from space
        }

        public void ResolveSpecialBombActions(Planet planet)
        {
            if (SpecialAction.IsEmpty() || SpecialAction != "Free Owlwoks")
                return;

            Empire cordrazine = planet.Universe.Cordrazine;
            if (planet.Owner == null || planet.Owner != cordrazine)
                return;

            bool owlwoksFreed = false;
            foreach (Troop troop in planet.Troops.GetTroopsOf(cordrazine))
            {
                if (troop.TargetType == TargetType.Soft)
                {
                    owlwoksFreed = true;
                    troop.SetOwner(Owner);
                    troop.Name = cordrazine.data.TroopName.Text;
                    troop.Description = cordrazine.data.TroopDescription.Text;
                }
            }

            if (owlwoksFreed)
            {
                StarDriveGame.Instance?.SetSteamAchievement("Owlwoks_Freed");
            }
        }

        public void SetTarget(Planet p)
        {
            TargetPlanet = p;
            PlanetRadius = TargetPlanet.Radius;
            Vector3 vtt = TargetPlanet.Position3D + 
                new Vector3(RandomMath2.Float(-500f, 500f) * p.Scale, 
                            RandomMath2.Float(-500f, 500f) * p.Scale, 0f) - Position;
            Velocity = vtt.Normalized(1350f);
        }

        public void Update(FixedSimTime timeStep)
        {
            Position += Velocity * timeStep.FixedTime;
            World    = Matrix.CreateTranslation(Position);
                        //* Matrix.CreateRotationZ(Facing);

            Vector3 planetPos = TargetPlanet.Position3D;

            float impactRadius = TargetPlanet.ShieldStrengthCurrent > 0f ? 100f : 30f;
            if (Position.InRadius(planetPos, PlanetRadius + impactRadius))
                DoImpact();


            // fiery trail radius:
            if (!Position.InRadius(planetPos, PlanetRadius + 1000f))
                return;

            if (TrailEmitter == null)
            {
                Velocity *= 0.65f;
                TrailEmitter     = Owner.Universe.Screen.Particles.ProjectileTrail.NewEmitter(500f, Position);
                FireTrailEmitter = Owner.Universe.Screen.Particles.FireTrail.NewEmitter(500f, Position);
            }
            TrailEmitter.Update(timeStep.FixedTime, Position);
            FireTrailEmitter.Update(timeStep.FixedTime, Position);
        }
    }
}