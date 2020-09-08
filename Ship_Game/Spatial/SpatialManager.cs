using System;
using Microsoft.Xna.Framework;
using Ship_Game.Ships;

namespace Ship_Game.Gameplay
{
    public sealed class SpatialManager
    {
        Quadtree QuadTree;

        public void Setup(float universeRadius)
        {
            float universeWidth = universeRadius * 2f;
            QuadTree = new Quadtree(universeWidth);
            Log.Info($"SpatialManager Width: {(int)universeWidth}  QTSize: {(int)QuadTree.FullSize}  QTLevels: {QuadTree.Levels}");
        }

        public void Destroy()
        {
            QuadTree = null;
        }

        public void DebugVisualize(UniverseScreen screen)
        {
            QuadTree.DebugVisualize(screen);
        }

        static bool IsSpatialType(GameplayObject obj)
            => obj.Is(GameObjectType.Ship) || obj.Is(GameObjectType.Proj)/*also Beam*/;
        
        public void Add(GameplayObject obj)
        {
            if (!IsSpatialType(obj) || QuadTree == null)
                return; // not a supported spatial manager type. just ignore it

            QuadTree.Insert(obj);
        }

        public void Remove(GameplayObject obj)
        {
            QuadTree.Remove(obj);
        }

        public void Update(FixedSimTime timeStep)
        {
            QuadTree.UpdateAll(timeStep);
            QuadTree.CollideAll();
        }

        public GameplayObject[] FindNearby(GameplayObject obj, float radius,
                                           GameObjectType filter = GameObjectType.Any,
                                           Empire loyaltyFilter = null)
        {
            return QuadTree.FindNearby(obj.Center, radius, filter, toIgnore:obj, loyaltyFilter);
        }

        public GameplayObject[] FindNearby(Vector2 worldPos, float radius,
                                           GameObjectType filter = GameObjectType.Any,
                                           Empire loyaltyFilter = null)
        {
            return QuadTree.FindNearby(worldPos, radius, filter, toIgnore:null, loyaltyFilter);
        }


        // @note This is called every time an exploding projectile hits a target and dies
        //       so everything nearby receives additional splash damage
        //       usually the recipient is only 1 ship, but ships can overlap and cause more results
        public void ProjectileExplode(Projectile source, float damageAmount, float damageRadius)
        {
            if (damageRadius <= 0f)
                return;

            // min search radius of 512. problem was that at very small search radius neighbors would not be found.
            // I tried to make the min to a the smallest cell size. 
            GameplayObject[] ships = FindNearby(source, Math.Max(damageRadius, 512), GameObjectType.Ship);
            ships.SortByDistance(source.Center);

            foreach (GameplayObject go in ships)
            {
                var ship = (Ship)go;
                // Doctor: Up until now, the 'Reactive Armour' bonus used in the vanilla tech tree did exactly nothing. Trying to fix - is meant to reduce effective explosion radius.
                // Doctor: Reset the radius on every foreach loop in case ships of different loyalty are to be affected:
                float modifiedRadius = damageRadius;
                
                // Check if valid target
                if (source.Owner?.loyalty == ship.loyalty || !ship.Active)
                    continue;

                // Doctor: Reduces the effective explosion radius on ships with the 'Reactive Armour' type radius reduction in their empire traits.
                if (ship.loyalty?.data.ExplosiveRadiusReduction > 0f)
                    modifiedRadius *= 1f - ship.loyalty.data.ExplosiveRadiusReduction;

                ship.DamageModulesExplosive(source, damageAmount, source.Center, modifiedRadius, source.IgnoresShields);
            }
        }

        // Refactored by RedFox
        public void ExplodeAtModule(GameplayObject damageSource, ShipModule hitModule, 
                                    bool ignoresShields, float damageAmount, float damageRadius)
        {
            if (damageRadius <= 0.0f || damageAmount <= 0.0f)
                return;
            Ship shipToDamage = hitModule.GetParent();
            if (shipToDamage.dying || !shipToDamage.Active)
                return;

            shipToDamage.DamageModulesExplosive(damageSource, damageAmount, hitModule.Center, damageRadius, ignoresShields);
        }

        // @note This is called quite rarely, so optimization is not a priority
        public void ShipExplode(Ship thisShip, float damageAmount, Vector2 position, float damageRadius)
        {
            if (damageRadius <= 0.0f || damageAmount <= 0.0f)
                return;

            Vector2 explosionCenter = thisShip.Center;
            GameplayObject[] nearby = FindNearby(thisShip, thisShip.Radius);

            for (int i = 0; i < nearby.Length; ++i)
            {
                GameplayObject otherObj = nearby[i];
                if (otherObj == thisShip || !otherObj.Active) continue;
                if (otherObj is Projectile) continue;
                if (!otherObj.Center.InRadius(thisShip.Center, damageRadius + otherObj.Radius))
                    continue;

                if (otherObj is Ship otherShip && RandomMath.RollDice(12 - otherShip.Level))
                {
                    // FB: Ships will be lucky to not get caught in the explosion, based on their level as well
                    ShipModule nearest = otherShip.FindClosestUnshieldedModule(explosionCenter);
                    if (nearest == null)
                        continue;

                    float reducedDamageRadius = damageRadius - explosionCenter.Distance(nearest.Center);
                    if (reducedDamageRadius <= 0.0f)
                        continue;

                    float damageFalloff = ShipModule.DamageFalloff(explosionCenter, nearest.Center, damageRadius, nearest.Radius);
                    ExplodeAtModule(thisShip, nearest, false, damageAmount * damageFalloff, reducedDamageRadius);

                    if (!otherShip.dying)
                    {
                        float rotationImpulse = damageRadius / (float)Math.Pow(otherShip.Mass, 1.3);
                        otherShip.yRotation = otherShip.yRotation > 0.0f ? rotationImpulse : -rotationImpulse;
                        otherShip.yRotation = otherShip.yRotation.Clamped(-otherShip.MaxBank, otherShip.MaxBank);
                    }

                    // apply some impulse from the explosion
                    Vector2 impulse = 3f * (otherShip.Center - explosionCenter);
                    if (impulse.Length() > 200f)
                        impulse = impulse.Normalized() * 200f;
                    if (!float.IsNaN(impulse.X))
                        otherShip.ApplyForce(impulse);
                }
                else
                {
                    float damageFalloff = ShipModule.DamageFalloff(explosionCenter, otherObj.Center, damageRadius, otherObj.Radius, 0.25f);
                    otherObj.Damage(thisShip, damageAmount * damageFalloff);
                }
            }
        }
    }
}
