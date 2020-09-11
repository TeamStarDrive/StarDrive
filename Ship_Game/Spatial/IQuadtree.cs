using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Ship_Game
{
    public interface IQuadtree
    {
        /// <summary>
        /// Intended size of the universe
        /// </summary>
        float UniverseSize { get; }

        /// <summary>
        /// Full size of the quadtree, usually slightly bigger than intended size
        /// </summary>
        float FullSize { get; }

        /// <summary>
        /// Number of levels in the quadtree
        /// </summary>
        int Levels { get; }

        /// <summary>
        /// Number of pending and active objects in the Quadtree
        /// </summary>
        int Count { get; }

        void Reset();
        void Insert(GameplayObject go);
        void Remove(GameplayObject go);

        void UpdateAll();
        void CollideAll(FixedSimTime timeStep);
        void CollideAllRecursive(FixedSimTime timeStep);

        GameplayObject[] FindNearby(Vector2 worldPos, float radius,
                                    GameObjectType filter,
                                    GameplayObject toIgnore,
                                    Empire loyaltyFilter);

        GameplayObject[] FindLinear(Vector2 worldPos, float radius,
                                    GameObjectType filter,
                                    GameplayObject toIgnore,
                                    Empire loyaltyFilter);

        void DebugVisualize(GameScreen screen);
    }
}
