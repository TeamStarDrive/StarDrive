using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SDGraphics;
using SDUtils;
using Ship_Game;

namespace UnitTests;

public partial class StarDriveTest
{
    /////////////////////////////////////////////////////////////////////////////////////////////
    ////// Assertion utils
    /////////////////////////////////////////////////////////////////////////////////////////////
    
    public const float DefaultTolerance = 0.000001f;
    public const double DefaultToleranceD = 0.000001;

    static bool Eq(float tolerance, float expected, float actual) => expected == actual || expected.AlmostEqual(actual, tolerance) || object.Equals(expected, actual);
    static bool Eq(double tolerance, double expected, double actual) => expected == actual || expected.AlmostEqual(actual, tolerance) || object.Equals(expected, actual);
    static bool Eq(float expected, float actual) => Eq(DefaultTolerance, expected, actual);
    static bool Eq(double expected, double actual) => Eq(DefaultToleranceD, expected, actual);

    static void Assert(float tolerance, float expected, float actual, string message)
    {
        if (!Eq(tolerance, expected, actual)) throw new AssertFailedException(Msg(message, expected, actual));
    }

    static void Assert(double tolerance, double expected, double actual, string message)
    {
        if (!Eq(tolerance, expected, actual)) throw new AssertFailedException(Msg(message, expected, actual));
    }

    static void Assert(int expected, int actual, string message)
    {
        if (expected != actual) throw new AssertFailedException(Msg(message, expected, actual));
    }

    static void Assert(object expected, object actual, string message)
    {
        if (!object.Equals(expected, actual)) throw new AssertFailedException(Msg(message, expected, actual));
    }

    /////////////////////////////////////////////////////////////////////////////////////////////
    
    public static void AssertTrue(bool value, string message = "")
    {
        if (!value) throw new AssertFailedException(Msg(message, expected:true, actual:false));
    }
    
    public static void AssertFalse(bool value, string message = "")
    {
        if (value) throw new AssertFailedException(Msg(message, expected:false, actual:true));
    }

    /////////////////////////////////////////////////////////////////////////////////////////////

    public static void AssertEqual(int expected, int actual, string message = "")
        => Assert(expected, actual, message);

    public static void AssertEqual(float expected, float actual, string message = "")
        => Assert(DefaultTolerance, expected, actual, message);

    public static void AssertEqual(float tolerance, float expected, float actual, string message = "")
        => Assert(tolerance, expected, actual, message);

    public static void AssertEqual(double expected, double actual, string message = "")
        => Assert(DefaultToleranceD, expected, actual, message);

    public static void AssertEqual(double tolerance, double expected, double actual, string message = "")
        => Assert(tolerance, expected, actual, message);

    public static void AssertEqual(in Point expected, in Point actual, string message = "")
    {
        if (Eq(expected.X, actual.X) &&
            Eq(expected.Y, actual.Y))
            return; // OK
        throw new AssertFailedException(Msg(message, expected, actual));
    }

    public static void AssertEqual(float tolerance, in Vector2 expected, in Vector2 actual, string message = "")
    {
        if (Eq(tolerance, expected.X, actual.X) &&
            Eq(tolerance, expected.Y, actual.Y))
            return; // OK
        throw new AssertFailedException(Msg(message, expected, actual));
    }

    public static void AssertEqual(float tolerance, in Vector3 expected, in Vector3 actual, string message = "")
    {
        if (Eq(tolerance, expected.X, actual.X) &&
            Eq(tolerance, expected.Y, actual.Y) &&
            Eq(tolerance, expected.Z, actual.Z))
            return; // OK
        throw new AssertFailedException(Msg(message, expected, actual));
    }

    public static void AssertEqual(float tolerance, in Vector4 expected, in Vector4 actual, string message = "")
    {
        if (Eq(tolerance, expected.X, actual.X) &&
            Eq(tolerance, expected.Y, actual.Y) &&
            Eq(tolerance, expected.Z, actual.Z) &&
            Eq(tolerance, expected.W, actual.W))
            return; // OK
        throw new AssertFailedException(Msg(message, expected, actual));
    }

    // compares two tuples of Vector2-s
    public static void AssertEqual(float tolerance, 
        in (Vector2, Vector2) expected,
        in (Vector2, Vector2) actual,
        string message = "")
    {
        if (Eq(tolerance, expected.Item1.X, actual.Item1.X) &&
            Eq(tolerance, expected.Item1.Y, actual.Item1.Y) &&
            Eq(tolerance, expected.Item2.X, actual.Item2.X) &&
            Eq(tolerance, expected.Item2.Y, actual.Item2.Y))
            return; // OK
        throw new AssertFailedException(Msg(message, expected, actual));
    }

    /////////////////////////////////////////////////////////////////////////////////////////////

    public static void AssertEqual(object expected, object actual, string message = "")
    {
        if (expected == null)
        {
            if (actual == null) return;
            throw new AssertFailedException(Msg(message, null, actual));
        }

        if (expected is IEnumerable expectedEnumerable)
        {
            if (expected is ICollection expectedCollection)
            {
                if (actual is ICollection actualCollection)
                    AssertEqual(expectedCollection, actualCollection);
                else
                    throw new AssertFailedException(Msg($"{message}. Expected a Collection!", expected, actual));
            }
            else if (actual is IEnumerable actualEnumerable)
                AssertEqual(expectedEnumerable, actualEnumerable);
            else
                throw new AssertFailedException(Msg($"{message}. Expected an Enumerable!", expected, actual));
            return;
        }

        if (IsKeyValuePair(expected) && IsKeyValuePair(actual))
        {
            var (key1, val1) = DecomposeKeyValuePair(expected);
            var (key2, val2) = DecomposeKeyValuePair(actual);
            AssertEqual(key1, key2, message);
            AssertEqual(val1, val2, message);
            return;
        }

        Assert(expected, actual, message);
    }

    public static void AssertEqual(IEnumerable expected, IEnumerable actual, string message = "")
    {
        if (expected == null)
        {
            if (actual == null) return;
            throw new AssertFailedException(Msg(message, null, actual));
        }

        object[] e = ToArray(expected);
        object[] a = ToArray(actual);
        if (e.Length != a.Length)
            throw new AssertFailedException(Msg(message, e, a));
        try
        {
            for (int i = 0; i < e.Length; ++i)
                AssertEqual(e[i], a[i], message);
        }
        catch (AssertFailedException)
        {
            throw new AssertFailedException(Msg(message, e, a));
        }
    }

    public static void AssertEqualCollections<T>(ICollection<T> expected, ICollection<T> actual, string message = "")
    {
        if (expected == null)
        {
            if (actual == null) return;
            throw new AssertFailedException(Msg(message, null, actual));
        }
            
        T[] e = ToArray(expected);
        T[] a = ToArray(actual);
        if (expected.Count != actual.Count)
            throw new AssertFailedException(Msg(message, e, a));
        try
        {
            for (int i = 0; i < e.Length; ++i)
                AssertEqual(e[i], a[i], message);
        }
        catch (AssertFailedException)
        {
            throw new AssertFailedException(Msg(message, e, a));
        }
    }

    /// <summary>
    /// Compare an object by individual member properties/fields
    /// </summary>
    public static void AssertMemberwiseEqual<T>(T expected, T actual, string message = "")
    {
        Array<string> mismatches = expected.MemberwiseCompare(actual);
        if (mismatches.Count > 0)
        {
            string mismatchText = string.Join("\n", mismatches);
            throw new AssertFailedException($"MemberwiseEqual found {mismatches.Count} mismatches: {MsgLine(message)}{mismatchText}");
        }
    }

    /// <summary>
    /// Asserts two strings are equal. On failure, generates a comparison
    /// </summary>
    public static void AssertEqual(string expected, string actual)
    {
        if (string.Equals(expected, actual))
            return;

        if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(actual))
            throw new AssertFailedException($"Expected:<{expected}>\nActual:<{actual}>");

        int mismatch = 0;
        for (int i = 0; i < expected.Length && i < actual.Length; ++i, ++mismatch)
            if (expected[i] != actual[i])
                break;

        const int offset = 5;
        int from = mismatch - offset;
        if (from < 0) from = 0;

        string s1 = SafeSubstring(expected, from, offset*3, out _);
        string s2 = SafeSubstring(actual, from, offset*3, out int inserted);
        int actualOffset = (mismatch - from) + inserted;

        throw new AssertFailedException(
            $"Expected: \"{s1}\"\n"+
            $"Actual:   \"{s2}\"\n"+
            $"           {new string('.', actualOffset)}^\n"+
            $"Strings mismatch at index {mismatch}\n"+
            $"Expected:<{expected}>. Actual:<{actual}>."
        );
    }

    /// <summary>
    /// Asserts that `actual` value is less than provided value
    /// e.g. AssertLessThan(healthPercent, 0.5f);
    /// </summary>
    public static void AssertLessThan<T>(T actual, T lessThan, string message = "") where T : IComparable<T>
    {
        if (actual.CompareTo(lessThan) >= 0)
            throw new AssertFailedException($"LessThan failed: {actual} < {lessThan}  {message}");
    }

    /// <summary>
    /// Asserts that `actual` value is greater than provided value
    /// e.g. AssertGreaterThan(healthPercent, 0.5f);
    /// </summary>
    public static void AssertGreaterThan<T>(T actual, T greaterThan, string message = "") where T : IComparable<T>
    {
        if (actual.CompareTo(greaterThan) <= 0)
            throw new AssertFailedException($"Greater Than failed: {actual} > {greaterThan}  {message}");
    }

    /////////////////////////////////////////////////////////////////////////////////////////////
            
    static string Msg(string message, object expected, object actual)
    {
        string expectedString = Stringify(expected);
        string actualString = Stringify(actual);
        return MsgLine(message)
            + $"-- Expected: {expectedString}\n"
            + $"-- Actual:   {actualString}";
    }

    static string MsgLine(string msg) => msg.IsEmpty() ? "" : msg+"\n";

    static string Stringify(object o)
    {
        if (o == null) return "null";
        if (o is ICollection c) return CollectionToString(c);
        if (o is IEnumerable e) return CollectionToString(ToArray(e));
        return o.ToString();
    }

    static string SafeSubstring(string s, int startIndex, int length, out int inserted)
    {
        inserted = 0;
        string result = "";
        for (int i = startIndex, n = 0; i < s.Length && n < length; ++i, ++n)
        {
            char ch = s[i];
            if      (ch == '\n') { result += "\\n"; ++inserted; }
            else if (ch == '\t') { result += "\\t"; ++inserted; }
            else result += ch;
        }
        return result;
    }

    static bool IsKeyValuePair(object instance)
    {
        Type type = instance.GetType();
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>);
    }

    static (object key, object value) DecomposeKeyValuePair(object instance)
    {
        Type type = instance.GetType();
        object key = type.GetProperty("Key")?.GetValue(instance);
        object value = type.GetProperty("Value")?.GetValue(instance);
        return (key, value);
    }

    static object[] ToArray(IEnumerable enumerable)
    {
        if (enumerable is ICollection c)
        {
            var items = new object[c.Count];
            int i = 0;
            foreach (object o in c)
                items[i++] = o;
            return items;
        }
        else
        {
            var items = new Array<object>();
            var e = enumerable.GetEnumerator();
            while (e.MoveNext()) items.Add(e.Current);
            return items.ToArray();
        }
    }

    static T[] ToArray<T>(ICollection<T> c)
    {
        var items = new T[c.Count];
        c.CopyTo(items, 0);
        return items;
    }

    static string CollectionToString(ICollection c)
    {
        var sb = new StringBuilder();
        sb.Append("[");
        int i = 0, count = c.Count;
        foreach (object o in c)
        {
            sb.Append(o);
            if (++i != count) sb.Append(", ");
        }
        sb.Append("]");
        return sb.ToString();
    }

    /////////////////////////////////////////////////////////////////////////////////////////////
}
