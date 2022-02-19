using System;
using Microsoft.Xna.Framework;
using Ship_Game.Ships;
using Ship_Game.Spatial;

namespace Ship_Game.Gameplay
{
    public sealed class SpatialManager
    {
        SpatialType Type = SpatialType.Qtree;
        ISpatial Spatial;
        ISpatial ResetToNewSpatial;
        int UniverseWidth;

        public readonly AggregatePerfTimer UpdateTime = new AggregatePerfTimer();
        public readonly AggregatePerfTimer CollisionTime = new AggregatePerfTimer();

        public string Name => Spatial?.Name ?? "";
        public int Collisions { get; private set; }
        public int Count => Spatial?.Count ?? 0;

        public VisualizerOptions VisOpt = new VisualizerOptions();

        public void Setup(float universeRadius)
        {
            UniverseWidth = (int)(universeRadius * 2f);
            Spatial = Create(Type);
            ResetToNewSpatial = null;
        }

        ISpatial Create(SpatialType type)
        {
            Type = type;
            ISpatial newSpatial;
            switch (type)
            {
                default:
                case SpatialType.Grid:         newSpatial = new NativeSpatial(type, UniverseWidth, 8_000); break;
                case SpatialType.Qtree:        newSpatial = new NativeSpatial(type, UniverseWidth, 1024); break;
                case SpatialType.GridL2:       newSpatial = new NativeSpatial(type, UniverseWidth, 16_000, 500); break;
                case SpatialType.ManagedQtree: newSpatial = new Qtree(UniverseWidth, 512); break;
            }
            Log.Info($"SpatialManager {newSpatial.Name} Width: {UniverseWidth}  FullSize: {(int)newSpatial.FullSize}");
            return newSpatial;
        }

        public void ToggleSpatialType()
        {
            ResetToNewSpatial = Create( Type.IncrementWithWrap(1) );
        }

        // only clears objects
        public void Clear()
        {
            Spatial?.Clear();
        }

        // Destroys everything
        public void Destroy()
        {
            Spatial?.Clear();
            Spatial = null;
        }

        public void DebugVisualize(UniverseScreen screen)
        {
            Spatial.DebugVisualize(screen, VisOpt);
        }

        public void Update(GameplayObject[] allObjects)
        {
            if (ResetToNewSpatial != null)
            {
                // wipe all objects from spatial
                for (int i = 0; i < allObjects.Length; ++i)
                    allObjects[i].SpatialIndex = -1;

                Spatial.Clear();
                Spatial = ResetToNewSpatial;
                ResetToNewSpatial = null;

                UpdateTime.Clear();
                CollisionTime.Clear();
            }

            UpdateTime.Start();
            Spatial.UpdateAll(allObjects);
            UpdateTime.Stop();
        }

        public void CollideAll(FixedSimTime timeStep, bool showCollisions)
        {
            CollisionTime.Start();
            Collisions = Spatial.CollideAll(timeStep, showCollisions);
            CollisionTime.Stop();
        }

        public GameplayObject[] FindNearby(ref SearchOptions opt)
        {
            return Spatial.FindNearby(ref opt);
        }

        /// <param name="type"></param>
        /// <param name="obj"></param>
        /// <param name="radius"></param>
        /// <param name="maxResults">Maximum results to get.
        /// PROTIP: if numResults > maxResults, then results are sorted by distance and far objects are discarded</param>
        public GameplayObject[] FindNearby(GameObjectType type,
                                           GameplayObject obj, float radius,
                                           int maxResults,
                                           Empire excludeLoyalty = null,
                                           Empire onlyLoyalty = null,
                                           int debugId = 0)
        {
            var opt = new SearchOptions(obj.Position, radius, type)
            {
                MaxResults = maxResults,
                Exclude = obj,
                ExcludeLoyalty = excludeLoyalty,
                OnlyLoyalty = onlyLoyalty,
                DebugId = debugId
            };

            return Spatial.FindNearby(ref opt);
        }

        public GameplayObject[] FindNearby(GameObjectType type,
                                           Vector2 worldPos, float radius,
                                           int maxResults,
                                           Empire excludeLoyalty = null,
                                           Empire onlyLoyalty = null,
                                           int debugId = 0)
        {
            var opt = new SearchOptions(worldPos, radius, type)
            {
                MaxResults = maxResults,
                ExcludeLoyalty = excludeLoyalty,
                OnlyLoyalty = onlyLoyalty,
                DebugId = debugId
            };

            return Spatial.FindNearby(ref opt);
        }

        public GameplayObject[] FindNearby(GameObjectType type, 
                                           in AABoundingBox2D searchArea,
                                           int maxResults,
                                           Empire excludeLoyalty = null,
                                           Empire onlyLoyalty = null,
                                           int debugId = 0)
        {
            var opt = new SearchOptions(searchArea, type)
            {
                MaxResults = maxResults,
                ExcludeLoyalty = excludeLoyalty,
                OnlyLoyalty = onlyLoyalty,
                DebugId = debugId
            };

            return Spatial.FindNearby(ref opt);
        }


        // @note This is called every time an exploding projectile hits a target and dies
        //       so everything nearby receives additional splash damage
        //       usually the recipient is only 1 ship, but ships can overlap and cause more results
        public void ProjectileExplode(Projectile source, float damageAmount, float damageRadius, Vector2 center)
        {
            if (damageRadius <= 0f)
                return;

            GameplayObject[] ships = FindNearby(GameObjectType.Ship, source, damageRadius,
                                                    maxResults:32, excludeLoyalty:source.Owner?.Loyalty);
            ships.SortByDistance(center);

            foreach (GameplayObject go in ships)
            {
                var ship = (Ship)go;
                if (!ship.Active)
                    continue;

                // Doctor: Up until now, the 'Reactive Armour' bonus used in the vanilla tech tree did exactly nothing. Trying to fix - is meant to reduce effective explosion radius.
                // Doctor: Reset the radius on every foreach loop in case ships of different loyalty are to be affected:
                float modifiedRadius = damageRadius;

                // Doctor: Reduces the effective explosion radius on ships with the 'Reactive Armour' type radius reduction in their empire traits.
                if (ship.Loyalty?.data.ExplosiveRadiusReduction > 0f)
                    modifiedRadius *= 1f - ship.Loyalty.data.ExplosiveRadiusReduction;

                ship.DamageModulesExplosive(source, damageAmount, center, modifiedRadius, source.IgnoresShields);
            }
        }

        // Refactored by RedFox
        public void ExplodeAtModule(GameplayObject damageSource, ShipModule hitModule, 
                                    bool ignoresShields, float damageAmount, float damageRadius)
        {
            if (damageRadius <= 0.0f || damageAmount <= 0.0f)
                return;

            Ship shipToDamage    = hitModule.GetParent();
            if (shipToDamage.Dying || !shipToDamage.Active)
                return;

            float modifiedRadius = damageRadius;
            if (shipToDamage.Loyalty?.data.ExplosiveRadiusReduction > 0f)
                modifiedRadius *= 1f - shipToDamage.Loyalty.data.ExplosiveRadiusReduction;

            shipToDamage.DamageModulesExplosive(damageSource, damageAmount, hitModule.Position, modifiedRadius, ignoresShields);
        }

        // @note This is called quite rarely, so optimization is not a priority
        public void ShipExplode(Ship thisShip, float damageAmount, Vector2 position, float damageRadius)
        {
            if (damageRadius <= 0.0f || damageAmount <= 0.0f)
                return;

            Vector2 explosionCenter = position;

            // find any nearby ship -- even allies
            GameplayObject[] nearby = FindNearby(GameObjectType.Ship, thisShip, damageRadius + 64, maxResults:32);

            for (int i = 0; i < nearby.Length; ++i)
            {
                var otherShip = (Ship)nearby[i];
                // FB: Ships will be lucky to not get caught in the explosion, based on their level as well
                if (RandomMath.RollDice(otherShip.ExplosionEvadeBaseChance() + otherShip.Level))
                    continue;

                ShipModule nearest = otherShip.FindClosestUnshieldedModule(explosionCenter);
                if (nearest == null)
                    continue;

                float reducedDamageRadius = damageRadius - explosionCenter.Distance(nearest.Position);
                if (reducedDamageRadius <= 0.0f)
                    continue;

                float damageFalloff = ShipModule.DamageFalloff(explosionCenter, nearest.Position, damageRadius, nearest.Radius);
                // First damage all shields covering the module
                damageAmount *= damageFalloff;
                foreach (ShipModule shield in otherShip.GetAllActiveShieldsCoveringModule(nearest))
                {
                    shield.DamageShield(damageAmount, null, null, out damageAmount);
                    if (damageAmount <= 0)
                        break;
                }

                // Then explode at the module if any excess damage left
                if (damageAmount > 0)
                    ExplodeAtModule(thisShip, nearest, false, damageAmount, reducedDamageRadius);

                if (!otherShip.Dying)
                {
                    float rotationImpulse = damageRadius / (float)Math.Pow(otherShip.Mass, 1.3);
                    otherShip.YRotation = otherShip.YRotation > 0.0f ? rotationImpulse : -rotationImpulse;
                    otherShip.YRotation = otherShip.YRotation.Clamped(-otherShip.MaxBank, otherShip.MaxBank);
                }

                // apply some impulse from the explosion
                Vector2 impulse = 3f * (otherShip.Position - explosionCenter);
                if (impulse.Length() > 200f)
                    impulse = impulse.Normalized() * 200f;

                if (!float.IsNaN(impulse.X))
                    otherShip.ApplyForce(impulse);
            }
        }
    }
}
