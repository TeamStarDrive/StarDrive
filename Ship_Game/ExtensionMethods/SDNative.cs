using System.Runtime.InteropServices;

namespace Ship_Game
{

    /**
     * SDNative.dll  rpp::strview
     */
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public unsafe struct CStrView
    {
        readonly sbyte* Str;
        readonly int Len;
        public string AsString         => Len != 0 ? new string(Str, 0, Len) : string.Empty;
        public string AsInterned       => Len != 0 ? string.Intern(new string(Str, 0, Len)) : string.Empty;
        public string AsInternedOrNull => Len != 0 ? string.Intern(new string(Str, 0, Len)) : null;
        public bool Empty    => Len == 0;
        public bool NotEmpty => Len > 0;
        public override string ToString() { return AsString; }
    }

}
