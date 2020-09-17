using System;
using System.Runtime.InteropServices;

namespace Ship_Game.Spatial.Native
{
    [StructLayout(LayoutKind.Sequential)]
    struct NativeSearchOptions
    {
        public int OriginX;
        public int OriginY;
        public int SearchRadius;
        public int MaxResults;
        public int FilterByType;
        public int FilterExcludeObjectId;
        public int FilterExcludeByLoyalty;
        public int FilterIncludeOnlyByLoyalty;
        public IntPtr FilterFunction;
    };
}
