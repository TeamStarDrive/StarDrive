using System;
using System.Collections.Generic;
using System.Text;

namespace SDUtils;

public static class TypeNameExt
{
    static readonly Dictionary<Type, string> GenericTypeNames = new();

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
}