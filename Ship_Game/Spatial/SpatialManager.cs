using Microsoft.Xna.Framework;
using System;

namespace Ship_Game.Gameplay
{
    public sealed class SpatialManager : IDisposable
    {
        private readonly Array<GameplayObject> AllObjects = new Array<GameplayObject>();
        private Quadtree QuadTree;

        public void Setup(float universeRadius)
        {
            float universeWidth = universeRadius * 2f;
            QuadTree = new Quadtree(universeWidth);
            Log.Info($"SpatialManager Width: {(int)universeWidth}  QTSize: {(int)QuadTree.FullSize}  QTLevels: {QuadTree.Levels}");
        }

        public void Destroy()
        {
            QuadTree?.Dispose(ref QuadTree);
            AllObjects.Clear();
        }

        public void Dispose()
        {
            Destroy(); GC.SuppressFinalize(this);
        }
        ~SpatialManager() { Destroy(); }

        public void DebugVisualize(UniverseScreen screen) => QuadTree.DebugVisualize(screen);

        private static bool IsSpatialType(GameplayObject obj)
            => obj.Is(GameObjectType.Ship) || obj.Is(GameObjectType.Proj)/*also Beam*/;
        
        public void Add(GameplayObject obj)
        {
            if (!IsSpatialType(obj))
                return; // not a supported spatial manager type. just ignore it

            // this is related to QuadTree fast-removal
            if (obj is Projectile proj && proj.DieNextFrame)
                return;

            obj.SpatialIndex = AllObjects.Count;
            AllObjects.Add(obj);
            QuadTree.Insert(obj);
        }

        public void Remove(GameplayObject obj)
        {
            QuadTree?.Remove(obj);
            AllObjects[obj.SpatialIndex] = null;
            obj.SpatialIndex = -1;
        }

        // this should only be called from Quadtree
        public void FastNonTreeRemoval(GameplayObject obj)
        {
            int idx = obj.SpatialIndex;
            if (idx == -1)
            {
                Log.Error($"SpatialManager attempted double remove of object: {obj}");
                return; // not in any SpatialManagers, so Remove is no-op
            }
            AllObjects[idx] = null;
            obj.SpatialIndex = -1;
        }

        // @todo OPTIMIZE THIS CLUSTERFCK
        public void GetDeepSpaceShips(Array<Ship> copyTo)
        {
            copyTo.Clear();
            for (int i = 0; i < AllObjects.Count; ++i)
            {
                GameplayObject go = AllObjects[i];
                if (go == null || !go.Active || !go.InDeepSpace || !go.Is(GameObjectType.Ship))
                    continue;

                copyTo.Add(go as Ship);
            }
        }

        public void Update(float elapsedTime)
        {
            // remove null values and remove inactive objects
            GameplayObject[] allObjects = AllObjects.GetInternalArrayItems();
            for (int i = 0; i < AllObjects.Count; ++i)
            {
                GameplayObject obj = allObjects[i];
                if (obj != null) obj.SpatialIndex = i;
                else AllObjects.RemoveAtSwapLast(i--);
            }

            QuadTree.UpdateAll();
            QuadTree.CollideAll();
        }

        public GameplayObject[] FindNearby(GameplayObject obj, float radius, GameObjectType filter = GameObjectType.None)
            => QuadTree.FindNearby(obj, radius, filter);

        // @note This is called every time an exploding projectile hits a target and dies
        //       so everything nearby receives additional splash damage
        //       usually the receipient is only 1 ship, but ships can overlap and cause more results
        public void ProjectileExplode(Projectile source, float damageAmount, float damageRadius)
        {
            if (damageRadius <= 0f)
                return;

            GameplayObject[] ships = FindNearby(source, damageRadius, GameObjectType.Ship);
            ships.SortByDistance(source.Center);

            foreach (GameplayObject go in ships)
            {
                var ship = (Ship)go;
                // Doctor: Up until now, the 'Reactive Armour' bonus used in the vanilla tech tree did exactly nothing. Trying to fix - is meant to reduce effective explosion radius.
                // Doctor: Reset the radius on every foreach loop in case ships of different loyalty are to be affected:
                float modifiedRadius = damageRadius;
                
                // Check if valid target
                if (source.Owner?.loyalty == ship.loyalty)
                    continue;

                // Doctor: Reduces the effective explosion radius on ships with the 'Reactive Armour' type radius reduction in their empire traits.
                if (ship.loyalty?.data.ExplosiveRadiusReduction > 0f)
                    modifiedRadius *= 1f - ship.loyalty.data.ExplosiveRadiusReduction;

                ship.DamageModulesInRange(source, damageAmount, source.Center, modifiedRadius, source.IgnoresShields);
            }
        }

        // Refactored by RedFox
        public void ExplodeAtModule(GameplayObject damageSource, ShipModule hitModule, 
                                    bool ignoreShields, float damageAmount, float damageRadius)
        {
            if (damageRadius <= 0.0f || damageAmount <= 0.0f)
                return;
            Ship shipToDamage = hitModule.GetParent();
            if (shipToDamage.dying || !shipToDamage.Active)
                return;

            shipToDamage.DamageModulesInRange(damageSource, damageAmount, hitModule.Center, damageRadius, ignoreShields);
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

                if (otherObj is Ship otherShip)
                {
                    ShipModule nearest = otherShip.FindClosestUnshieldedModule(explosionCenter);
                    if (nearest == null)
                        continue;

                    float reducedDamageRadius = damageRadius - explosionCenter.Distance(nearest.Center);
                    if (reducedDamageRadius <= 0.0f)
                        continue;

                    float damageFalloff = ShipModule.DamageFalloff(explosionCenter, nearest.Center, damageRadius);
                    ExplodeAtModule(thisShip, nearest, false, damageAmount * damageFalloff, reducedDamageRadius);

                    if (!otherShip.dying)
                    {
                        float rotationImpulse = damageRadius / (float)Math.Pow(otherShip.Mass, 1.3);
                        otherShip.yRotation = otherShip.yRotation > 0.0f ? rotationImpulse : -rotationImpulse;
                        otherShip.yRotation = otherShip.yRotation.Clamp(-otherShip.maxBank, otherShip.maxBank);
                    }

                    // apply some impulse from the explosion
                    Vector2 impulse = 3f * (otherShip.Center - explosionCenter);
                    if (impulse.Length() > 200.0)
                        impulse = impulse.Normalized() * 200f;
                    if (!float.IsNaN(impulse.X))
                        otherObj.Velocity += impulse;
                }
                else
                {
                    float damageFalloff = ShipModule.DamageFalloff(explosionCenter, otherObj.Center, damageRadius, 0.25f);
                    otherObj.Damage(thisShip, damageAmount * damageFalloff);
                }
            }
        }
    }
}
