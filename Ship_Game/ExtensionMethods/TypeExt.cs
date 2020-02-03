using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Ship_Game
{
    public static class TypeExt
    {
        static void GetGenericTypeName(this Type type, StringBuilder sb)
        {
            sb.Append(type.Name.Split('`')[0]);
            Type[] args = type.GenericTypeArguments;
            if (args.Length > 0)
            {
                sb.Append('<');
                for (int i = 0; i < args.Length; ++i)
                {
                    GetGenericTypeName(args[i], sb);
                    if (i != args.Length - 1) sb.Append(',');
                }
                sb.Append('>');
            }
        }

        static readonly Map<Type, string> GenericTypeNames = new Map<Type, string>();

        public static string GetTypeName(this Type type)
        {
            if (!type.IsGenericType)
                return type.Name;
            
            if (GenericTypeNames.TryGetValue(type, out string typeName))
                return typeName;

            var sb = new StringBuilder();
            GetGenericTypeName(type, sb);
            typeName = sb.ToString();
            GenericTypeNames.Add(type, typeName);
            return typeName;
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

        /// <returns>Ordinal integer value of the Enum value</returns>
        public static int GetOrdinal<TEnum>(this TEnum value)
            where TEnum : Enum
        {
            return ((IConvertible)value).ToInt32(null);
        }

        /// <summary>
        /// Increments an enum value and applies a wraparound,
        /// so the value is always in the enum range
        /// </summary>
        public static TEnum IncrementWithWrap<TEnum>(this TEnum value, int increment)
            where TEnum : Enum
        {
            var enumValues = (TEnum[])typeof(TEnum).GetEnumValues();

            TEnum first = enumValues[0];
            TEnum last = enumValues[enumValues.Length - 1];

            int iFirst = first.GetOrdinal();
            int iLast = last.GetOrdinal();
            int newValue = value.GetOrdinal() + increment;
            if      (newValue < iFirst) newValue = iLast;
            else if (newValue > iLast)  newValue = iFirst;
            
            return (TEnum)Enum.ToObject(typeof(TEnum), newValue);
        }
    }
}
