using System;
using SDGraphics;
using SDUtils;
using Ship_Game.Ships;
using Ship_Game.Spatial;
using Vector2 = SDGraphics.Vector2;

namespace Ship_Game.Gameplay
{
    public sealed class SpatialManager
    {
        SpatialType Type = SpatialType.Qtree;
        ISpatial Spatial;
        ISpatial ResetToNewSpatial;
        int UniverseWidth;

        public readonly AggregatePerfTimer UpdateTime = new();
        public readonly AggregatePerfTimer CollisionTime = new();

        public string Name => Spatial?.Name ?? "";
        public int Collisions { get; private set; }
        public int Count => Spatial?.Count ?? 0;

        public VisualizerOptions VisOpt = new();

        public SpatialManager(float universeWidth)
        {
            UniverseWidth = (int)universeWidth;
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
            Spatial?.DebugVisualize(screen, VisOpt);
        }

        public void Update(SpatialObjectBase[] allObjects)
        {
            if (ResetToNewSpatial != null)
            {
                // wipe all objects from spatial
                for (int i = 0; i < allObjects.Length; ++i)
                    allObjects[i].SpatialIndex = -1;

                Spatial?.Clear();
                Spatial = ResetToNewSpatial;
                ResetToNewSpatial = null;

                UpdateTime.Clear();
                CollisionTime.Clear();
            }

            UpdateTime.Start();
            Spatial?.UpdateAll(allObjects);
            UpdateTime.Stop();
        }

        public void CollideAll(FixedSimTime timeStep, bool showCollisions)
        {
            CollisionTime.Start();
            Collisions = Spatial?.CollideAll(timeStep, showCollisions) ?? 0;
            CollisionTime.Stop();
        }

        public SpatialObjectBase[] FindNearby(ref SearchOptions opt)
        {
            return Spatial?.FindNearby(ref opt) ?? Empty<SpatialObjectBase>.Array;
        }

        /// <summary>
        /// Finds nearby objects by GameObjectType and additional excludeLoyalty or onlyLoyalty filters
        /// </summary>
        /// <param name="type"></param>
        /// <param name="source">The SOURCE GameObject to search from, and this object will also be excluded</param>
        /// <param name="radius">The search radius</param>
        /// <param name="maxResults">Maximum results to get.
        /// PROTIP: if numResults > maxResults, then results are sorted by distance and far objects are discarded</param>
        /// <param name="excludeLoyalty">If not null, exclude objects that have this loyalty</param>
        /// <param name="onlyLoyalty">If not null, only return objects that have this loyalty</param>
        /// <param name="debugId">If set to nonzero, this will display unique debugging info during debug visualize</param>
        public SpatialObjectBase[] FindNearby(GameObjectType type,
                                              GameObject source, float radius,
                                              int maxResults,
                                              Empire excludeLoyalty = null,
                                              Empire onlyLoyalty = null,
                                              int debugId = 0)
        {
            SearchOptions opt = new(source.Position, radius, type)
            {
                MaxResults = maxResults,
                Exclude = source,
                ExcludeLoyalty = excludeLoyalty,
                OnlyLoyalty = onlyLoyalty,
                DebugId = debugId
            };

            return Spatial?.FindNearby(ref opt) ?? Empty<SpatialObjectBase>.Array;
        }

        public SpatialObjectBase[] FindNearby(GameObjectType type,
                                              Vector2 worldPos, float radius,
                                              int maxResults,
                                              Empire excludeLoyalty = null,
                                              Empire onlyLoyalty = null,
                                              int debugId = 0)
        {
            SearchOptions opt = new(worldPos, radius, type)
            {
                MaxResults = maxResults,
                ExcludeLoyalty = excludeLoyalty,
                OnlyLoyalty = onlyLoyalty,
                DebugId = debugId
            };

            return Spatial?.FindNearby(ref opt) ?? Empty<SpatialObjectBase>.Array;
        }

        public SpatialObjectBase[] FindNearby(GameObjectType type,
                                              in AABoundingBox2D searchArea,
                                              int maxResults,
                                              Empire excludeLoyalty = null,
                                              Empire onlyLoyalty = null,
                                              int debugId = 0)
        {
            SearchOptions opt = new(searchArea, type)
            {
                MaxResults = maxResults,
                ExcludeLoyalty = excludeLoyalty,
                OnlyLoyalty = onlyLoyalty,
                DebugId = debugId
            };

            return Spatial?.FindNearby(ref opt) ?? Empty<SpatialObjectBase>.Array;
        }

        public void ProjectileExplode(Projectile source, ShipModule victim)
        {
            // damage radius is increased during Module init to include module's own radius
            // so if exploding module is 1x1 and data file radius is 8, then actual radius
            // will be 8 + 8 = 16, meaning it will affect a 3x3 area
            float radius = source.DamageRadius;
            float damage = source.DamageAmount;

            // if radius is huge, the projectile acts like a space nuke and affects all ships in range
            // this is very rare
            if (radius >= 256f)
            {
                Vector2 center = source.Position;
                SpatialObjectBase[] ships = FindNearby(GameObjectType.Ship, center, radius,
                                                       maxResults:32, excludeLoyalty:source.Owner?.Loyalty);
                foreach (SpatialObjectBase go in ships)
                {
                    var ship = (Ship)go;
                    if (ship.Active && !ship.Dying)
                        ship.DamageExplosive(source, damage, center, radius, source.IgnoresShields);
                }
            }
            else
            {
                victim.GetParent().DamageExplosive(source, damage, source.Position, radius, source.IgnoresShields);
            }
        }

        // @note This is called quite rarely, so optimization is not a priority
        public void ShipExplode(Ship thisShip, float damageAmount, Vector2 explosionCenter, float damageRadius)
        {
            if (damageRadius <= 0.0f || damageAmount <= 0.0f)
                return;

            // find any nearby ship -- even allies
            SpatialObjectBase[] nearby = FindNearby(GameObjectType.Ship, thisShip, damageRadius + 64, maxResults:32);

            for (int i = 0; i < nearby.Length && damageAmount > 0f; ++i)
            {
                var otherShip = (Ship)nearby[i];
                // FB: Ships will be lucky to not get caught in the explosion, based on their level as well
                if (thisShip.Loyalty.Random.RollDice(otherShip.ExplosionEvadeBaseChance() + otherShip.Level))
                    continue;

                // First damage all shields covering the explosion center
                while (true)
                {
                    ShipModule shield = otherShip.HitTestShields(explosionCenter, 16f);
                    if (shield == null) break;
                    shield.DamageShield(damageAmount, null, out damageAmount);
                    if (damageAmount <= 0f)
                        return;
                }

                ShipModule nearest = otherShip.FindClosestModule(explosionCenter);
                if (nearest == null)
                    continue;

                float reducedRadius = damageRadius - explosionCenter.Distance(nearest.Position);
                if (reducedRadius <= 0f)
                    continue;

                float falloff = ShipModule.DamageFalloff(explosionCenter, nearest.Position, damageRadius, nearest.Radius);
                damageAmount *= falloff;
                if (damageAmount <= 0f)
                    return;

                // Then explode at the module if any excess damage left
                // Ignoring shields because we already checked shields above
                otherShip.DamageExplosive(thisShip, damageAmount, nearest.Position, reducedRadius, true);

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
