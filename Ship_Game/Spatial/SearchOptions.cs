using Microsoft.Xna.Framework;
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
        /// The Search Area in World coordinates
        /// </summary>
        public AABoundingBox2D SearchRect;

        /// <summary>
        /// Radial filter
        /// If set, search results will have an additional filtering pass using this circle
        /// </summary>
        public Vector2 FilterOrigin;
        public float FilterRadius;

        /// <summary>
        /// Maximum number of filtered final results until search is terminated
        /// Must be at least 1
        /// </summary>
        public int MaxResults;

        /// <summary>
        /// Filter search results by object type
        /// 0: disabled
        /// </summary>
        public GameObjectType FilterByType;

        /// <summary>
        /// Filter search results by excluding this specific object
        /// </summary>
        public GameplayObject FilterExcludeObject;

        /// <summary>
        /// Filter search results by excluding objects with this loyalty
        /// </summary>
        public Empire FilterExcludeByLoyalty;

        /// <summary>
        /// Filter search results by only matching objects with this loyalty
        /// </summary>
        public Empire FilterIncludeOnlyByLoyalty;

        /// <summary>
        /// Filter search results by passing the matched object through this function
        /// null: disabled
        /// Return false: filter failed, object discarded
        /// Return true: filter passed, object added to results
        /// </summary>
        public SearchFilterFunc FilterFunction;

        /// <summary>
        /// If set to nonzero, this search will be displayed
        /// as an unique entry
        /// </summary>
        public int DebugId;

        /// <summary>
        /// Creates search options from point and radius,
        /// only matching results inside the radius
        /// </summary>
        /// <param name="center">Center of the search area</param>
        /// <param name="radius">Strict radius of the search area</param>
        /// <param name="type">Type of objects to accept in results</param>
        public SearchOptions(Vector2 center, float radius, GameObjectType type = GameObjectType.Any)
        {
            SearchRect = new AABoundingBox2D(center, radius);
            FilterOrigin = center;
            FilterRadius = radius;
            MaxResults = 128;
            FilterByType = type;
            FilterExcludeObject = null;
            FilterExcludeByLoyalty = null;
            FilterIncludeOnlyByLoyalty = null;
            FilterFunction = null;
            DebugId = 0;
        }

        
        /// <summary>
        /// Creates search options from a rectangular search area
        /// </summary>
        /// <param name="searchArea">Rectangular search area</param>
        /// <param name="type">Type of objects to accept in results</param>
        public SearchOptions(AABoundingBox2D searchArea, GameObjectType type = GameObjectType.Any)
        {
            SearchRect = searchArea;
            FilterOrigin = default;
            FilterRadius = 0;
            MaxResults = 128;
            FilterByType = type;
            FilterExcludeObject = null;
            FilterExcludeByLoyalty = null;
            FilterIncludeOnlyByLoyalty = null;
            FilterFunction = null;
            DebugId = 0;
        }
    }
}
