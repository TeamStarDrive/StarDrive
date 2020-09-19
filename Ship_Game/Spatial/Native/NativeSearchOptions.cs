using System;
using System.Runtime.InteropServices;

namespace Ship_Game.Spatial.Native
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate int SearchFilter(int objectId);

    [StructLayout(LayoutKind.Sequential)]
    public struct NativeSearchOptions
    {
        public int OriginX;
        public int OriginY;
        public int SearchRadius;
        public int MaxResults;
        public int FilterByType;
        public int FilterExcludeObjectId;
        public int FilterExcludeByLoyalty;
        public int FilterIncludeOnlyByLoyalty;
        public SearchFilter FilterFunction;
    };
}
