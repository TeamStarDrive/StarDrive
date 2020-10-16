using System;

namespace Ship_Game.Spatial
{
    /// <summary>
    /// Customizable options for Spatial.DebugVisualize()
    /// </summary>
    public struct VisualizerOptions
    {
        public bool ObjectBounds;      // show bounding box around inserted objects
        public bool ObjectToLeafLines; // show connections from Leaf node to object center
        public bool ObjectText;    // show text ontop of each object (very, very intensive)
        public bool NodeText;      // show text ontop of a leaf or branch node
        public bool NodeBounds;    // show edges of leaf and branch nodes
        public bool SearchDebug;   // show the debug information for latest searches
        public bool SearchResults; // highlight search results
        public bool Collisions;    // show collision flashes

        public static readonly VisualizerOptions Default = new VisualizerOptions
        {
            ObjectBounds      = true,
            ObjectToLeafLines = true,
            ObjectText  = false,
            NodeText    = false,
            NodeBounds  = true,
            SearchDebug = true,
            SearchResults = true,
            Collisions    = true,
        };
    }
}
