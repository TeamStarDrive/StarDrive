using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

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

        readonly struct ObjectPair
        {
            public readonly object A;
            public readonly object B;
            public ObjectPair(object a, object b)
            {
                A = a;
                B = b;
            }
            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + A.GetHashCode();
                    hash = hash * 31 + B.GetHashCode();
                    return hash;
                }
            }
            public override bool Equals(object obj)
            {
                var other = (ObjectPair)obj;
                bool containsA = ReferenceEquals(A, other.A) || ReferenceEquals(A, other.B);
                if (!containsA)
                    return false;
                bool containsB = ReferenceEquals(B, other.A) || ReferenceEquals(B, other.B);
                return containsB;
            }
        }

        static Type[] BuiltinTypes;

        public static bool IsBuiltIn(this Type type)
        {
            if (type.IsPrimitive)
                return true;
            if (BuiltinTypes == null)
            {
                BuiltinTypes = new []
                {
                    typeof(string), typeof(decimal), typeof(Guid), 
                    typeof(Vector2), typeof(Vector3), typeof(Vector4), typeof(Rectangle), typeof(Viewport), 
                    typeof(LocalizedText), 
                };
            }
            return BuiltinTypes.Contains(type);
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
            var checkedObjects = new HashSet<ObjectPair>();

            void Error(MemberInfo member, string err)
            {
                Type type = (member is PropertyInfo p) ? p.PropertyType : ((FieldInfo)member).FieldType;
                string text = $"{member.DeclaringType.GetTypeName()}::{member.Name} {type.GetTypeName()} {err}";
                errors.Add(text);
                Log.Warning(text);
            }
            
            bool CompareCollection(MemberInfo member, ICollection col1, ICollection col2)
            {
                if (col1.Count != col2.Count)
                {
                    Error(member, $"Count {col1.Count} != {col2.Count}");
                    return false;
                }

                int i = 0;
                IEnumerator en2 = col2.GetEnumerator();
                foreach (object o1 in col1)
                {
                    en2.MoveNext();
                    object o2 = en2.Current;
                    bool equal = CheckEqual(member, o1, o2);
                    //if (!equal)
                    //{
                    //    Debugger.Break();
                    //}
                    if (!equal)
                    {
                        Error(member, $"elements at [{i}] were not equal: {o1} != {o2}");
                        return false;
                    }
                    ++i;
                }
                return true; // all elements were equal
            }

            bool CompareDictionary(MemberInfo member, IDictionary dict1, IDictionary dict2)
            {
                if (dict1.Count != dict2.Count)
                {
                    Error(member, $"Count {dict1.Count} != {dict2.Count}");
                    return false;
                }

                IDictionaryEnumerator de = dict1.GetEnumerator();
                while (de.MoveNext())
                {
                    if (!dict2.Contains(de.Key))
                    {
                        Error(member, $"key=[{de.Key}] not found in second dictionary");
                        return false;
                    }
                    object o1 = de.Value;
                    object o2 = dict2[de.Key];
                    if (!CheckEqual(member, o1, o2))
                    {
                        Error(member, $"values with key=[{de.Key}] were not equal: {o1} != {o2}");
                        return false;
                    }
                }
                return true; // all elements were equal
            }
            
            bool CompareEnumerable(MemberInfo member, IEnumerable en1, IEnumerable en2)
            {
                var items1 = new Array<object>();
                var items2 = new Array<object>();
                foreach (object o in en1) items1.Add(o);
                foreach (object o in en2) items2.Add(o);

                if (items1.Count != items2.Count)
                {
                    Error(member, $"Count {items1.Count} != {items2.Count}");
                    return false;
                }

                for (int i = 0; i < items1.Count; ++i)
                {
                    object o1 = items1[i];
                    object o2 = items2[i];
                    if (!CheckEqual(member, o1, o2))
                    {
                        Error(member, $"elements at [{i}] were not equal: {o1} != {o2}");
                        return false;
                    }
                }
                return true; // all elements were equal
            }

            bool CheckEqual(MemberInfo member, object val1, object val2)
            {
                if (val1 == null && val2 == null)
                    return true;

                if (val1 == null || val2 == null)
                {
                    Error(member, $"One of the values was null: first={val1} second={val2}");
                    return false;
                }

                if (val1.Equals(val2))
                    return true;

                Type subType = val1.GetType();
                
                // for floats we need special treatment because of parser issues
                if (subType == typeof(float) && !((float)val1).AlmostEqual((float)val2, 0.0001f))
                {
                    Error(member, $"first={val1} != second={val2}");
                    return false;
                }
                
                // in this case, the Equals() check was sufficient
                if (subType.IsEnum || subType.IsBuiltIn())
                {
                    Error(member, $"first={val1} != second={val2}");
                    return false;
                }

                if (val1 is IDictionary dict1)
                    return CompareDictionary(member, dict1, (IDictionary)val2);

                if (val1 is ICollection col1)
                    return CompareCollection(member, col1, (ICollection)val2);

                if (val1 is IEnumerable en1)
                    return CompareEnumerable(member, en1, (IEnumerable)val2);

                return CompareFields(subType, val1, val2);
            }

            bool CompareFields(Type type, object firstObj, object secondObj)
            {
                // if this pair was already compared, ignore it,
                // otherwise we'll run into cyclic reference issues
                var pair = new ObjectPair(firstObj, secondObj);
                if (checkedObjects.Contains(pair))
                    return true;

                checkedObjects.Add(pair);

                //Log.Info($"Compare type {type.GetTypeName()}");

                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
                var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                int numErrors = 0;
                foreach (FieldInfo field in fields)
                {
                    if (type.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                        continue;
                    object val1 = field.GetValue(firstObj);
                    object val2 = field.GetValue(secondObj);
                    if (!CheckEqual(field, val1, val2))
                        ++numErrors;
                }
                foreach (PropertyInfo prop in properties)
                {
                    if (type.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                        continue;
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
