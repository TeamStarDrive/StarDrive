using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Ship_Game
{
    public static class TypeExt
    {
        public static void GenericName(this Type type, StringBuilder sb)
        {
            if (!type.IsGenericType)
            {
                sb.Append(type.Name);
                return;
            }
            sb.Append(type.Name.Split('`')[0]).Append('<');
            var args = type.GenericTypeArguments;
            for (int i = 0; i < args.Length; ++i)
            {
                GenericName(args[i], sb);
                if (i != args.Length - 1) sb.Append(',');
            }
            sb.Append('>');
        }

        public static string GenericName(this Type type)
        {
            var sb = new StringBuilder();
            GenericName(type, sb);
            return sb.ToString();
        }

        // Helps to make manual disposing of objects safe and brief
        // Always call as: myDisposable?.Dispose(ref myDisposable);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // ReSharper disable once RedundantAssignment
        public static void Dispose<T>(this IDisposable obj, ref T self) where T : class, IDisposable
        {
            obj.Dispose();
            self = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NotEmpty(this string str)
        {
            // ReSharper disable once ReplaceWithStringIsNullOrEmpty
            return str != null && str.Length > 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEmpty(this string str)
        {
            // ReSharper disable once ReplaceWithStringIsNullOrEmpty
            return str == null || str.Length == 0;
        }

        /// <summary>For value types, returns sizeof structure. 
        /// For reference types, returns sizeof pointer</summary> 
        public static int SizeOfRef(this Type type)
        {
            bool reftype = type.IsClass || type.IsSealed;
            return reftype ? IntPtr.Size : Marshal.SizeOf(type);
        }
    }
}
