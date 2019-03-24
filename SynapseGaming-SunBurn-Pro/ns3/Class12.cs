// Decompiled with JetBrains decompiler
// Type: ns3.Class12
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;

namespace ns3
{
    internal class Class12
    {
        static Dictionary<Type, Dictionary<string, PropertyInfo>> dictionary_0 = new Dictionary<Type, Dictionary<string, PropertyInfo>>(128);

        static Dictionary<string, PropertyInfo> smethod_0(Type type)
        {
            if (dictionary_0.ContainsKey(type))
                return dictionary_0[type];

            PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var dictionary = new Dictionary<string, PropertyInfo>(properties.Length);
            dictionary_0.Add(type, dictionary);
            foreach (PropertyInfo propertyInfo in properties)
            {
                if (propertyInfo.DeclaringType != typeof(Effect) || propertyInfo.Name != "CurrentTechnique")
                    dictionary.Add(propertyInfo.Name, propertyInfo);
            }
            return dictionary;
        }

        internal static void smethod_1(object object_0, object object_1)
        {
            Type type1 = object_0.GetType();
            Type type2 = object_1.GetType();
            Dictionary<string, PropertyInfo> dictionary1 = smethod_0(type1);
            Dictionary<string, PropertyInfo> dictionary2 = smethod_0(type2);
            foreach (string key in dictionary1.Keys)
            {
                if (dictionary1.ContainsKey(key) && dictionary2.ContainsKey(key))
                {
                    PropertyInfo propertyInfo1 = dictionary1[key];
                    PropertyInfo propertyInfo2 = dictionary2[key];
                    if (propertyInfo1.PropertyType == propertyInfo2.PropertyType && propertyInfo2.CanWrite && propertyInfo1.CanRead)
                        propertyInfo2.SetValue(object_1, propertyInfo1.GetValue(object_0, null), null);
                }
            }
        }
    }
}
