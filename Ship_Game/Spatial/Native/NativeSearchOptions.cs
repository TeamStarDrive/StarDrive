using System;
using System.Runtime.InteropServices;

namespace Ship_Game.Spatial.Native
{
    [StructLayout(LayoutKind.Sequential)]
    struct NativeSearchOptions
    {
        public float OriginX;
        public float OriginY;
        public float SearchRadius;
        public int MaxResults;
        public int FilterByType;
        public int FilterExcludeObjectId;
        public int FilterExcludeByLoyalty;
        public int FilterIncludeOnlyByLoyalty;
        public IntPtr FilterFunction;
    };
}
