using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ship_Game.Spatial
{
    public delegate bool SearchFilterFunc(GameplayObject go);

    public struct SearchOptions
    {
        /// <summary>
        /// The initial search origin X, Y coordinates
        /// </summary>
        public float OriginX;
        public float OriginY;

        /// <summary>
        /// Only objects that are within this radius are accepted
        /// </summary>
        public float SearchRadius;

        /// <summary>
        /// Maximum number of filtered final results until search is terminated
        /// Must be at least 1
        /// </summary>
        public int MaxResults;

        /// <summary>
        /// Filter search results by object type
        /// 0: disabled
        /// </summary>
        public int FilterByType;

        /// <summary>
        /// Filter search results by excluding this specific object
        /// -1: disabled
        /// </summary>
        public int FilterExcludeObjectId;

        /// <summary>
        /// Filter search results by excluding objects with this loyalty
        /// 0: disabled
        /// </summary>
        public int FilterExcludeByLoyalty;

        /// <summary>
        /// Filter search results by only matching objects with this loyalty
        /// 0: disabled
        /// </summary>
        public int FilterIncludeOnlyByLoyalty;

        /// <summary>
        /// Filter search results by passing the matched object through this function
        /// null: disabled
        /// Return 0: filter failed, object discarded
        /// Return 1: filter passed, object added to results
        /// </summary>
        public SearchFilterFunc FilterFunction;
    }
}
