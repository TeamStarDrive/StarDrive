using Ship_Game.Gameplay;
using Ship_Game.Ships;

namespace Ship_Game
{
    public sealed partial class Quadtree
    {
        // XOR Bitmask for True Projectile matching
        const int TrueProjXORMask = (int)GameObjectType.Proj; // 0000_0100

        public void CollideAll(FixedSimTime timeStep)
        {
            float simTimeStep = timeStep.FixedTime;
            FindResultBuffer buffer = GetThreadLocalTraversalBuffer(Root);

            // deepest nodes first, to early filter LastCollided
            Array<QtreeNode> deepestNodesFirst = DeepestNodesFirstTraversal;
            int numNodes = deepestNodesFirst.Count;
            QtreeNode[] items = deepestNodesFirst.GetInternalArrayItems();

            for (int nodeIndex = 0; nodeIndex < numNodes; ++nodeIndex)
            {
                QtreeNode node = items[nodeIndex];
                for (int i = 0; i < node.Count; ++i)
                {
                    ref SpatialObj so = ref node.Items[i];
                    if (so.Active != 0) // 0: already collided in this loop ?
                    {
                        // each collision instigator type has a very specific recursive handler
                        if (so.Type == GameObjectType.Beam)
                        {
                            var beam = (Beam)so.Obj;
                            if (!beam.BeamCollidedThisFrame)
                                CollideBeamAtNode(node, beam, ref so, BeamHitCache);
                        }
                        else if (so.Type == GameObjectType.Proj)
                        {
                            var projectile = (Projectile)so.Obj;
                            if (CollideProjAtNode(simTimeStep, node, projectile, ref so) && projectile.DieNextFrame)
                                MarkForRemoval(so.Obj, ref so);
                        }
                        else if (so.Type == GameObjectType.Ship)
                        {
                            CollideShipAtNodeIterative(simTimeStep, buffer, node, ref so);
                        }
                    }
                }
            }
        }

        // ship collision; this can collide with multiple projectiles..
        // beams are ignored because they may intersect multiple objects and thus require special CollideBeamAtNode
        void CollideShipAtNodeIterative(float simTimeStep, FindResultBuffer buffer, QtreeNode localRoot, ref SpatialObj ship)
        {
            buffer.ResetAndPush(localRoot);
            byte shipLoyalty = ship.Loyalty;
            do
            {
                QtreeNode node = buffer.Pop();
                int count = node.Count;
                SpatialObj[] items = node.Items;
                for (int i = 0; i < count; ++i)
                {
                    ref SpatialObj proj = ref items[i]; // potential projectile ?

                    //// for the next part, we mask all of the attributes together
                    //// this avoids branches and speeds up the overall loop
                    //// if any of these is 0, then the entire result is 0 and we skip this object

                    //// object is being removed, no collision
                    //// (0:alive - 1) --> -1
                    //// (1:dead  - 1) --> 0
                    //int all_passed = (proj.PendingRemove - 1)
                    //         // No friendly fire in this game mode
                    //         // (enemy - friend) --> -1 [-X ... +X]
                    //         // (friend - friend) --> 0
                    //         & (proj.Loyalty - shipLoyalty)
                    //         // if this is not a true projectile type, skip
                    //         // Beam-Projectiles are handled by CollideBeamAtNode
                    //         //   Proj: (0000_0100 & 1111_1011) --> 0
                    //         //   Beam: (0000_1100 & 1111_1011) --> 0000_1000 (8)
                    //         & ((int)proj.Type ^ TrueProjXORMask);
                    //if (all_passed == 0)
                    //    continue;

                    if (proj.Active == 0 ||
                        proj.Loyalty == shipLoyalty ||
                        proj.Type != GameObjectType.Proj)
                        continue;

                    if (proj.HitTestProj(simTimeStep, ref ship, out ShipModule hitModule))
                    {
                        GameplayObject victim = hitModule ?? ship.Obj;
                        var projectile = (Projectile)proj.Obj;

                        if (IsObjectDead(victim))
                        {
                            Log.Warning($"Ship dead but still in Quadtree: {ship.Obj}");
                            MarkForRemoval(ship.Obj, ref ship);
                        }
                        else if (projectile.Touch(victim) && IsObjectDead(projectile))
                        {
                            MarkForRemoval(projectile, ref proj);
                        }
                    }
                }

                if (node.NW != null)
                {
                    buffer.NodeStack[++buffer.NextNode] = node.SW;
                    buffer.NodeStack[++buffer.NextNode] = node.SE;
                    buffer.NodeStack[++buffer.NextNode] = node.NE;
                    buffer.NodeStack[++buffer.NextNode] = node.NW;
                }
            } while (buffer.NextNode >= 0);
        }

    }
}