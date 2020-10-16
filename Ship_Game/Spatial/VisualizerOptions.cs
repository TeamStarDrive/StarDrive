using System;

namespace Ship_Game.Spatial
{
    /// <summary>
    /// Customizable options for Spatial.DebugVisualize()
    /// </summary>
    public class VisualizerOptions
    {
        public bool Enabled      = true;  // overrides all values below (default: true)
        public bool ObjectBounds = true;  // show bounding box around inserted objects
        public bool ObjectToLeaf = true;  // show connections from Leaf node to object center
        public bool ObjectText   = false; // show text ontop of each object (very, very intensive)
        public bool NodeText     = false; // show text ontop of a leaf or branch node
        public bool NodeBounds    = true; // show edges of leaf and branch nodes
        public bool SearchDebug   = true; // show the debug information for latest searches
        public bool SearchResults = true; // highlight search results
        public bool Collisions    = true; // show collision flashes

        public static readonly VisualizerOptions None = new VisualizerOptions
        {
            Enabled = false,
            ObjectBounds = false,
            ObjectToLeaf = false,
            ObjectText = false,
            NodeText = false,
            NodeBounds = false,
            SearchDebug = false,
            SearchResults = false,
            Collisions = false,
        };
    }
}
