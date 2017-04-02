// Decompiled with JetBrains decompiler
// Type: Class35
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using SynapseGaming.LightingSystem.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

internal static class Class35
{
  private static Dictionary<string, object> dictionary_0 = new Dictionary<string, object>();
  private static string string_0 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Synapse Gaming\\SunBurn " + LightingSystemManager.Edition + " Editor - " + LightingSystemManager.Version + " - Settings.xml");

  public static void smethod_0<T>(string string_1, T gparam_0)
  {
    if (!((object) gparam_0 is string) && !((object) gparam_0 is int) && (!((object) gparam_0 is bool) && !((object) gparam_0 is float)))
      throw new Exception("Type " + typeof (T).Name + " is not supported in EditorSettings.");
    if (!Class35.dictionary_0.ContainsKey(string_1))
      Class35.dictionary_0.Add(string_1, (object) gparam_0);
    else
      Class35.dictionary_0[string_1] = (object) gparam_0;
  }

  public static T smethod_1<T>(string string_1)
  {
    if (Class35.dictionary_0.ContainsKey(string_1) && Class35.dictionary_0[string_1] is T)
      return (T) Class35.dictionary_0[string_1];
    return default (T);
  }

  public static T smethod_2<T>(string string_1, T gparam_0)
  {
    if (!Class35.dictionary_0.ContainsKey(string_1))
      Class35.dictionary_0.Add(string_1, (object) gparam_0);
    else if (!(Class35.dictionary_0[string_1] is T))
      return gparam_0;
    return (T) Class35.dictionary_0[string_1];
  }

  public static void smethod_3()
  {
    try
    {
      XmlDocument xmlDocument = new XmlDocument();
      XmlElement element1 = xmlDocument.CreateElement("EditorSettings");
      xmlDocument.AppendChild((XmlNode) element1);
      foreach (KeyValuePair<string, object> keyValuePair in Class35.dictionary_0)
      {
        XmlElement element2 = xmlDocument.CreateElement(keyValuePair.Key);
        element1.AppendChild((XmlNode) element2);
        element2.InnerText = keyValuePair.Value.ToString();
        Type type = keyValuePair.Value.GetType();
        XmlAttribute attribute = xmlDocument.CreateAttribute("Type");
        attribute.Value = type.Name;
        element2.Attributes.Append(attribute);
      }
      xmlDocument.Save(Class35.string_0);
    }
    catch
    {
    }
  }

  public static void smethod_4()
  {
    if (!File.Exists(Class35.string_0))
      return;
    XmlDocument xmlDocument = new XmlDocument();
    try
    {
      xmlDocument.Load(Class35.string_0);
      XmlNode firstChild = xmlDocument.FirstChild;
      if (firstChild == null)
        return;
      foreach (XmlNode childNode in firstChild.ChildNodes)
      {
        XmlAttribute attribute = childNode.Attributes["Type"];
        if (attribute != null)
        {
          object obj = (object) null;
          switch (attribute.Value)
          {
            case "Boolean":
              bool result1;
              if (bool.TryParse(childNode.InnerText, out result1))
              {
                obj = (object) result1;
                break;
              }
              break;
            case "Int32":
              int result2;
              if (int.TryParse(childNode.InnerText, out result2))
              {
                obj = (object) result2;
                break;
              }
              break;
            case "Single":
              float result3;
              if (float.TryParse(childNode.InnerText, out result3))
              {
                obj = (object) result3;
                break;
              }
              break;
            case "String":
              obj = (object) childNode.InnerText;
              break;
          }
          if (obj != null)
            Class35.dictionary_0.Add(childNode.Name, obj);
        }
      }
    }
    catch
    {
    }
  }
}
