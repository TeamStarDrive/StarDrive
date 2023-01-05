using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Graphics;
using SDGraphics;
using SDUtils;
using Vector4 = SDGraphics.Vector4;
using Rectangle = SDGraphics.Rectangle;
using System.IO;

namespace Ship_Game
{
    public static class TypeExt
    {
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
        public static unsafe int GetOrdinal<TEnum>(this TEnum value) where TEnum : Enum
        {
            int ordinal;
            Unsafe.Copy(&ordinal, ref value);
            return ordinal;
        }

        /// <summary>
        /// Faster than `Enum.HasFlag(flag)` by roughly 10x in Release, because it skips type check and uses raw pointer copy.
        ///
        /// However raw bits `(flags & MyEnum.Flag1) != 0` is still roughly 3-4x faster than this
        ///
        /// Some relative performance metrics from 100000 iterations in `Release` build:
        ///   raw bits:  0.110ms
        ///   IsSet ext: 0.350ms
        ///   HasFlag:   4.750ms
        /// </summary>
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe bool IsSet<TEnum>(this TEnum flags, TEnum flag) where TEnum : Enum
        {
            int flagsValue, flagValue;
            Unsafe.Copy(&flagsValue, ref flags);
            Unsafe.Copy(&flagValue, ref flag);
            return (flagsValue & flagValue) != 0;
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
            public override string ToString() => $"Pair A={A} B={B}";
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
                if (Equals(A, other.A)) return Equals(B, other.B);
                if (Equals(A, other.B)) return Equals(B, other.A);
                return false;
            }
        }

        class MemberwiseComparer
        {
            readonly Array<string> Errors = new();
            readonly HashSet<ObjectPair> Checked = new();
            readonly HashSet<Type> BuiltinTypes = new();

            public MemberwiseComparer()
            {
                BuiltinTypes = new()
                {
                    typeof(string), typeof(decimal), typeof(Guid), typeof(DateTime), typeof(TimeSpan),
                    typeof(Vector2), typeof(Vector3), typeof(Vector4), typeof(Rectangle), typeof(Viewport), 
                    typeof(LocalizedText),
                };
            }


            void Error(object parent, MemberInfo member, string err)
            {
                Type parentType = parent.GetType();
                Type type = (member is PropertyInfo p) ? p.PropertyType : ((FieldInfo)member).FieldType;
                string text = $"{parentType.GetTypeName()} -> {member.DeclaringType.GetTypeName()}::{member.Name} {type.GetTypeName()} {err}";
                Errors.Add(text);
                Log.Warning(text);
            }

            // if a type is a "Built-In", then object.Equals() must have been sufficient!
            bool IsBuiltIn(Type type)
            {
                if (type.IsPrimitive)
                    return true;
                return BuiltinTypes.Contains(type);
            }
            
            bool CompareCollection(object parent, MemberInfo member, ICollection col1, ICollection col2)
            {
                if (col1.Count != col2.Count)
                {
                    Error(parent, member, $"Count {col1.Count} != {col2.Count}");
                    return false;
                }

                int i = 0;
                IEnumerator en2 = col2.GetEnumerator();
                foreach (object o1 in col1)
                {
                    en2.MoveNext();
                    object o2 = en2.Current;
                    bool equal = CheckEqual(parent, member, o1, o2);
                    if (!equal)
                    {
                        Error(parent, member, $"elements at [{i}] were not equal: {o1} != {o2}");
                        return false;
                    }
                    ++i;
                }
                return true; // all elements were equal
            }

            bool CompareDictionary(object parent, MemberInfo member, IDictionary dict1, IDictionary dict2)
            {
                if (dict1.Count != dict2.Count)
                {
                    Error(parent, member, $"Count {dict1.Count} != {dict2.Count}");
                    return false;
                }

                IDictionaryEnumerator de = dict1.GetEnumerator();
                while (de.MoveNext())
                {
                    if (!dict2.Contains(de.Key))
                    {
                        Error(parent, member, $"key=[{de.Key}] not found in second dictionary");
                        return false;
                    }
                    object o1 = de.Value;
                    object o2 = dict2[de.Key];
                    if (!CheckEqual(parent, member, o1, o2))
                    {
                        Error(parent, member, $"values with key=[{de.Key}] were not equal: {o1} != {o2}");
                        return false;
                    }
                }
                return true; // all elements were equal
            }
            
            bool CompareEnumerable(object parent, MemberInfo member, IEnumerable en1, IEnumerable en2)
            {
                var items1 = new Array<object>();
                var items2 = new Array<object>();
                foreach (object o in en1) items1.Add(o);
                foreach (object o in en2) items2.Add(o);

                if (items1.Count != items2.Count)
                {
                    Error(parent, member, $"Count {items1.Count} != {items2.Count}");
                    return false;
                }

                for (int i = 0; i < items1.Count; ++i)
                {
                    object o1 = items1[i];
                    object o2 = items2[i];
                    if (!CheckEqual(parent, member, o1, o2))
                    {
                        Error(parent, member, $"elements at [{i}] were not equal: {o1} != {o2}");
                        return false;
                    }
                }
                return true; // all elements were equal
            }

            bool CheckEqual(object parent, MemberInfo member, object val1, object val2)
            {
                if (val1 == null && val2 == null)
                    return true;

                if (val1 == null || val2 == null)
                {
                    Error(parent, member, $"One of the values was null: first={val1} second={val2}");
                    return false;
                }

                if (val1.Equals(val2))
                    return true;

                Type subType = val1.GetType();
                if (subType == typeof(FileInfo))
                {
                    if (((FileInfo)val1).FullName == ((FileInfo)val2).FullName)
                        return true;
                    Error(parent, member, $"first={((FileInfo)val1).FullName} != second={((FileInfo)val2).FullName}");
                    return false;
                }

                // for floats we need special treatment because of parser issues
                if (subType == typeof(float))
                {
                    if (((float)val1).AlmostEqual((float)val2, 0.0001f))
                        return true;
                    Error(parent, member, $"first={val1:0.#####} != second={val2:0.#####}");
                    return false;
                }

                // in this case, the Equals() check was sufficient
                if (subType.IsEnum || IsBuiltIn(subType))
                {
                    Error(parent, member, $"first={val1} != second={val2}");
                    return false;
                }

                if (val1 is IDictionary dict1)
                    return CompareDictionary(parent, member, dict1, (IDictionary)val2);

                if (val1 is ICollection col1)
                    return CompareCollection(parent, member, col1, (ICollection)val2);

                if (val1 is IEnumerable en1)
                    return CompareEnumerable(parent, member, en1, (IEnumerable)val2);

                // this is a class with fields and properties, recurse:
                return CompareFields(subType, val1, val2);
            }

            bool CompareFields(Type type, object firstObj, object secondObj)
            {
                // if this pair was already compared, ignore it,
                // otherwise we'll run into cyclic reference issues
                var pair = new ObjectPair(firstObj, secondObj);
                if (Checked.Contains(pair))
                    return true;

                Checked.Add(pair);

                //Log.Info($"Compare type {type.GetTypeName()}");

                var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
                var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                //int numFieldsMismatched = 0;
                foreach (FieldInfo field in fields)
                {
                    object val1 = field.GetValue(firstObj);
                    object val2 = field.GetValue(secondObj);
                    if (!CheckEqual(firstObj, field, val1, val2))
                    {
                        return false; // DEBUG: give up immediately
                        //if (++numFieldsMismatched > 2) return false; // give up at this point
                    }
                }
                foreach (PropertyInfo prop in properties)
                {
                    var getMethod = prop.GetMethod;
                    if (getMethod == null) continue; // no getter, it's a set-only property
                    
                    var parameters = getMethod.GetParameters();
                    if (parameters.Length > 0) continue; // indexer property with multiple args
                    try
                    {
                        object val1 = prop.GetValue(firstObj);
                        object val2 = prop.GetValue(secondObj);
                        if (!CheckEqual(firstObj, prop, val1, val2))
                        {
                            return false; // DEBUG: give up immediately
                            //if (++numFieldsMismatched > 2) return false; // give up at this point
                        }
                    }
                    catch (Exception e)
                    {
                        throw new($"Error while getting Property {firstObj.GetType().GetTypeName()} -> {type.Name}::{prop.Name}", e);
                    }
                }
                return true;// numFieldsMismatched == 0;
            }

            public Array<string> Compare(object first, object second)
            {
                Errors.Clear();
                Checked.Clear();
                CompareFields(first.GetType(), first, second);
                Errors.Reverse();
                return Errors;
            }
        }

        /// <summary>
        /// Does a member-wise deep compare of 2 objects and returns
        /// error string of mismatched fields.
        ///
        /// Used for Unit Testing
        /// </summary>
        public static Array<string> MemberwiseCompare<T>(this T first, T second)
        {
            return new MemberwiseComparer().Compare(first, second);
        }
    }
}
