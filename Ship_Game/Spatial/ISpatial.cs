using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        /// <summary>
        /// Clears the entire state of the Spatial collection
        /// </summary>
        void Clear();

        /// <summary>
        /// Insert/Update/Remove all objects
        /// </summary>
        void UpdateAll(SpatialObjectBase[] allObjects);

        /// <summary>
        /// Collides all objects
        /// </summary>
        /// <param name="timeStep">Simulation timestep</param>
        /// <param name="showCollisions">If true, collision results are stored for debugging purposes (SLOW)</param>
        /// <returns></returns>
        int CollideAll(FixedSimTime timeStep, bool showCollisions);
        
        /// <summary>
        /// Finds nearby GameplayObjects using multiple filters
        /// WARNING: DO NOT USE `in` Attribute in Interfaces, it adds a +70% perf hit for no damn reason
        /// </summary>
        SpatialObjectBase[] FindNearby(in SearchOptions opt);

        /// <summary>
        /// Performs a linear search instead of using the Quadtree
        /// WARNING: DO NOT USE `in` Attribute in Interfaces, it adds a +70% perf hit for no damn reason
        /// </summary>
        SpatialObjectBase[] FindLinear(in SearchOptions opt);

        /// <summary>
        /// Visualize this Spatial collection for debugging purposes
        /// </summary>
        void DebugVisualize(GameScreen screen, VisualizerOptions opt);
    }
}
