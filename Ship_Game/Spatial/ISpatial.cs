using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Ship_Game.Spatial;

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
        GameplayObject[] FindNearby(in SearchOptions opt);

        /// <summary>
        /// Performs a linear search instead of using the Quadtree
        /// </summary>
        GameplayObject[] FindLinear(in SearchOptions opt);

        /// <summary>
        /// Visualize this Spatial collection for debugging purposes
        /// </summary>
        void DebugVisualize(GameScreen screen, VisualizerOptions opt);
    }
}
