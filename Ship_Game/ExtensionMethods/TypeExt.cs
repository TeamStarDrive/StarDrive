using System;
using System.Collections;
using System.Reflection;
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

        /// <summary>
        /// Does a member-wise deep compare of 2 objects and returns
        /// error string of mismatched fields.
        ///
        /// Used for Unit Testing
        /// </summary>
        public static Array<string> MemberwiseCompare<T>(this T first, T second)
        {
            var errors = new Array<string>();

            bool CheckEqual(MemberInfo member, object val1, object val2)
            {
                void Error(string err)
                {
                    errors.Add($"{member.DeclaringType.GetTypeName()}::{member.Name} {member.ReflectedType.GetTypeName()} {err}");
                }

                if (val1 == null && val2 == null)
                    return true;

                if (val1 == null || val2 == null)
                {
                    Error($"One of the values was null: first={val1} second={val2}");
                    return false;
                }

                if (val1.Equals(val2))
                    return true;

                Type subType = val1.GetType();

                if (val1 is ICollection col1)
                {
                    var col2 = (ICollection)val2;
                    if (col1.Count != col2.Count)
                    {
                        Error($"Collection Count {col1.Count} != {col2.Count}");
                        return false;
                    }

                    int i = 0;
                    IEnumerator en2 = col2.GetEnumerator();
                    foreach (object o1 in col1)
                    {
                        en2.MoveNext();
                        object o2 = en2.Current;
                        if (!CheckEqual(member, o1, o2))
                        {
                            Error($"Collection elements at [{i}] were not equal: {o1} != {o2}");
                            return false;
                        }
                        ++i;
                    }
                    return true; // all elements were equal
                }

                return CompareFields(subType, val1, val2);
            }

            bool CompareFields(Type type, object firstObj, object secondObj)
            {
                int numErrors = 0;
                foreach (FieldInfo field in type.GetFields())
                {
                    object val1 = field.GetValue(firstObj);
                    object val2 = field.GetValue(secondObj);
                    if (!CheckEqual(field, val1, val2))
                        ++numErrors;
                }
                foreach (PropertyInfo prop in type.GetProperties())
                {
                    object val1 = prop.GetValue(firstObj);
                    object val2 = prop.GetValue(secondObj);
                    if (!CheckEqual(prop, val1, val2))
                        ++numErrors;
                }
                return numErrors == 0;
            }
            CompareFields(first.GetType(), first, second);
            return errors;
        }
    }
}
