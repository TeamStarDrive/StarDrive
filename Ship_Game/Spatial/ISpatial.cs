using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace Ship_Game
{
    /// <summary>
    /// Generic Spatial collection interface
    /// Subdivides game world so that object collision and proximity testing can be done efficiently
    /// </summary>
    public interface ISpatial
    {
        /// <summary>
        /// User friendly name to describe this spatial container
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Original size of the simulation world
        /// </summary>
        float WorldSize { get; }

        /// <summary>
        /// Full Width and Height of the spatial collection. 
        /// This is usually bigger than world size
        /// </summary>
        float FullSize { get; }

        /// <summary>
        /// Total number of objects in this Spatial collection
        /// </summary>
        int Count { get; }

        void Clear();
        void UpdateAll(Array<GameplayObject> allObjects);
        int CollideAll(FixedSimTime timeStep);
        
        /// <summary>
        /// Finds nearby GameplayObjects using multiple filters
        /// </summary>
        /// <param name="type">Type of game objects to find, eg Ships or Projectiles</param>
        /// <param name="worldPos">Origin of the search</param>
        /// <param name="radius">Radius of the search area</param>
        /// <param name="maxResults">Limit the number of results for performance consideration</param>
        /// <param name="toIgnore">Single game object to ignore (usually our own ship), null (default): no ignore</param>
        /// <param name="excludeLoyalty">Exclude results by loyalty, when searching enemies exclude allies, null (default): no filtering</param>
        /// <param name="onlyLoyalty">Filter results by loyalty, when searching friends include allies, null (default): no filtering</param>
        /// <returns>GameObjects that match the search criteria</returns>
        GameplayObject[] FindNearby(GameObjectType type,
                                    Vector2 worldPos,
                                    float radius,
                                    int maxResults,
                                    GameplayObject toIgnore,
                                    Empire excludeLoyalty,
                                    Empire onlyLoyalty,
                                    int debugId = 0);

        /// <summary>
        /// Performs a linear search instead of using the Quadtree
        /// </summary>
        GameplayObject[] FindLinear(GameObjectType type,
                                    Vector2 worldPos,
                                    float radius,
                                    int maxResults,
                                    GameplayObject toIgnore,
                                    Empire excludeLoyalty,
                                    Empire onlyLoyalty,
                                    int debugId = 0);

        void DebugVisualize(GameScreen screen);
    }
}
