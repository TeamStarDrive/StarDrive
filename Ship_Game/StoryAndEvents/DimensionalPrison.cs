using System;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDGraphics.Sprites;
using Ship_Game.AI;
using Ship_Game.Gameplay;
using Ship_Game.Graphics;
using Ship_Game.Ships;
using Ship_Game.Universe;
using Vector2 = SDGraphics.Vector2;
using Vector3 = SDGraphics.Vector3;

namespace Ship_Game
{
    public sealed class DimensionalPrison : Anomaly
    {
        public string PlatformName = "Mysterious Platform";
        public Vector2 PlaformCenter;
        public string PrisonerId;
        private BackgroundItem Prison;
        private readonly Ship Platform1;
        private readonly Ship Platform2;
        private readonly Ship Platform3;

        private int NumSpawnedRemnants;
        private int NumRemnantsToSpawn = 9;
        private float SpawnCountdown;
        UniverseState Universe;

        public DimensionalPrison(UniverseState universe, Vector2 plaformCenter)
        {
            Universe = universe;
            PlaformCenter = plaformCenter;
            CreateDimensionalPrison(plaformCenter, 400);
            Platform1 = SpawnAncientRepulsor(universe, plaformCenter + new Vector2(0f, -400f));
            Platform2 = SpawnAncientRepulsor(universe, plaformCenter + new Vector2(-400f, 400f));
            Platform3 = SpawnAncientRepulsor(universe, plaformCenter + new Vector2(400f, -400f));
        }

        private Ship SpawnAncientRepulsor(UniverseState us, Vector2 repulsorPos)
        {
            Ship repulsor = Ship.CreateShipAtPoint(us, PlatformName, us.Unknown, repulsorPos);
            Weapon weapon = ResourceManager.CreateWeapon("AncientRepulsor");
            var beam = new Beam(us.CreateId(), weapon, repulsor, PlaformCenter, 75);
            beam.Infinite = true;
            beam.Range = 2500f;
            beam.PowerCost = 0f;
            beam.DamageAmount = 0f;
            return repulsor;
        }

        void CreateDimensionalPrison(Vector2 center, float radius)
        {
            var r = new RectF(center.X - radius, center.Y - radius, radius*2, radius*2);
            Prison = new BackgroundItem(ResourceManager.Texture("star_neutron"), r, 0);
        }

        public override void Draw(SpriteRenderer sr)
        {
            for (int i = 0; i < 20; i++)
            {
                Universe.Screen.Particles.Sparks.AddParticle(new Vector3(PlaformCenter, 0f) + GenerateRandomWithin(100f), GenerateRandomWithin(25f));
            }
            if (Universe.Random.Float(0f, 100f) > 97f)
            {
                Universe.Screen.Particles.Flash.AddParticle(new Vector3(PlaformCenter, 0f));
            }
            Prison.Draw(sr, Color.White);
        }

        Vector2 GenerateRandomV2(float radius)
        {
            return Universe.Random.Vector2D(radius);
        }

        Vector3 GenerateRandomWithin(float radius)
        {
            return Universe.Random.Vector3D(radius);
        }

        public override void Update(FixedSimTime timeStep)
        {
            // spawn a bunch of drones when the player has killed all platforms :)))
            if (!Platform1.Active && !Platform2.Active && !Platform3.Active)
            {
                SpawnCountdown -= timeStep.FixedTime;
                if (SpawnCountdown <= 0f)
                {
                    Ship enemy = Ship.CreateShipAtPoint(Universe, "Heavy Drone", Universe.Remnants, PlaformCenter);
                    enemy.Velocity = GenerateRandomV2(100f);
                    SpawnCountdown = 2f;
                    ++NumSpawnedRemnants;
                }
                if (NumSpawnedRemnants == NumRemnantsToSpawn)
                {
                    Universe.Screen.anomalyManager.AnomaliesList.Remove(this);
                }
            }
        }
    }
}