using System;
using System.Runtime.CompilerServices;
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
        public static void Dispose<T>(ref T self) where T : class, IDisposable
        {
            self.Dispose();
            self = null;
        }
    }
}
