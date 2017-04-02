// Decompiled with JetBrains decompiler
// Type: ns3.Class29
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using SynapseGaming.LightingSystem.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Xml;

namespace ns3
{
  internal class Class29 : IFormatter
  {
    private static Type[] type_0 = new Type[2]{ typeof (SerializationInfo), typeof (StreamingContext) };
    private const string string_0 = "root";
    private ISurrogateSelector isurrogateSelector_0;
    private SerializationBinder serializationBinder_0;
    private StreamingContext streamingContext_0;
    private SerializeTypeDictionary serializeTypeDictionary_0;

    public SerializationBinder Binder
    {
      get
      {
        return this.serializationBinder_0;
      }
      set
      {
        this.serializationBinder_0 = value;
      }
    }

    public ISurrogateSelector SurrogateSelector
    {
      get
      {
        return this.isurrogateSelector_0;
      }
      set
      {
        this.isurrogateSelector_0 = value;
      }
    }

    public StreamingContext Context
    {
      get
      {
        return this.streamingContext_0;
      }
      set
      {
        this.streamingContext_0 = value;
      }
    }

    public Class29(SerializeTypeDictionary typedictionary)
    {
      this.serializeTypeDictionary_0 = typedictionary;
    }

    public object Deserialize(Stream stream)
    {
      Dictionary<Type, MemberInfo[]> dictionary_0 = new Dictionary<Type, MemberInfo[]>(16);
      XmlDocument xmlDocument = new XmlDocument();
      xmlDocument.Load(stream);
      XmlElement documentElement = xmlDocument.DocumentElement;
      if (documentElement.Name == "root")
      {
        foreach (XmlNode childNode in documentElement.ChildNodes)
        {
          if (!(childNode.Name == "classes") && childNode is XmlElement)
            return this.method_0(childNode as XmlElement, dictionary_0);
        }
      }
      return (object) null;
    }

    private object method_0(XmlElement xmlElement_0, Dictionary<Type, MemberInfo[]> dictionary_0)
    {
      if (xmlElement_0 == null || string.IsNullOrEmpty(xmlElement_0.Name))
        return (object) null;
      string name = xmlElement_0.Name;
      Type type1 = this.serializeTypeDictionary_0.GetType(name);
      if (type1 == null)
        throw new Exception("Type '" + name + "' not registered with the serialization type dictionary.");
      object instance = Activator.CreateInstance(type1);
      if (instance == null)
        throw new Exception("Cannot create an instance of type '" + type1.FullName + "'.");
      SerializationInfo serializationInfo = new SerializationInfo(type1, (IFormatterConverter) new FormatterConverter());
      foreach (XmlNode childNode1 in xmlElement_0.ChildNodes)
      {
        if (childNode1 is XmlElement && !string.IsNullOrEmpty(childNode1.Name) && !string.IsNullOrEmpty(childNode1.InnerText))
        {
          bool flag = false;
          foreach (XmlNode childNode2 in childNode1.ChildNodes)
          {
            if (childNode2 is XmlElement)
            {
              serializationInfo.AddValue(childNode1.Name, this.method_0(childNode2 as XmlElement, dictionary_0));
              flag = true;
              break;
            }
          }
          if (!flag)
            serializationInfo.AddValue(childNode1.Name, (object) childNode1.InnerText);
        }
      }
      if (instance is ICollection)
      {
        IList list = instance as IList;
        IDictionary dictionary = instance as IDictionary;
        foreach (SerializationEntry serializationEntry in serializationInfo)
        {
          if (list != null)
            list.Add(serializationEntry.Value);
          else if (dictionary != null)
            dictionary.Add(serializationEntry.Value, (object) null);
        }
      }
      else if (instance is ISerializable)
      {
        ConstructorInfo constructor = type1.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, (System.Reflection.Binder) null, Class29.type_0, (ParameterModifier[]) null);
        if (constructor == null)
          throw new Exception("Object type '" + type1.Name + "' is ISerializable but doesn't contain the required deserialization constructor.");
        object[] parameters = new object[2]{ (object) serializationInfo, (object) this.streamingContext_0 };
        constructor.Invoke(instance, parameters);
      }
      else
      {
        MemberInfo[] serializableMembers;
        if (dictionary_0.ContainsKey(type1))
        {
          serializableMembers = dictionary_0[type1];
        }
        else
        {
          serializableMembers = FormatterServices.GetSerializableMembers(type1);
          dictionary_0.Add(type1, serializableMembers);
        }
        if (serializableMembers.Length > 0)
        {
          object[] data = new object[serializableMembers.Length];
          Dictionary<string, int> dictionary = new Dictionary<string, int>(serializableMembers.Length);
          for (int index = 0; index < serializableMembers.Length; ++index)
          {
            if (!dictionary.ContainsKey(serializableMembers[index].Name))
              dictionary.Add(serializableMembers[index].Name, index);
          }
          foreach (SerializationEntry serializationEntry in serializationInfo)
          {
            if (dictionary.ContainsKey(serializationEntry.Name))
            {
              int index = dictionary[serializationEntry.Name];
              MemberInfo memberInfo = serializableMembers[index];
              Type type2 = (Type) null;
              if (memberInfo is PropertyInfo)
                type2 = (memberInfo as PropertyInfo).PropertyType;
              else if (memberInfo is FieldInfo)
                type2 = (memberInfo as FieldInfo).FieldType;
              if (type2 != null)
                data[index] = serializationInfo.GetValue(serializationEntry.Name, type2);
            }
          }
          FormatterServices.PopulateObjectMembers(instance, serializableMembers, data);
        }
      }
      return instance;
    }

    public void Serialize(Stream stream, object rootobj)
    {
      Dictionary<Type, MemberInfo[]> dictionary_0 = new Dictionary<Type, MemberInfo[]>(16);
      Dictionary<object, int> dictionary_1 = new Dictionary<object, int>(16);
      XmlDocument xmlDocument_0 = new XmlDocument();
      XmlElement element = xmlDocument_0.CreateElement("root");
      xmlDocument_0.AppendChild((XmlNode) element);
      this.method_1(xmlDocument_0, element, rootobj, dictionary_0, dictionary_1);
      xmlDocument_0.Save(stream);
    }

    private void method_1(XmlDocument xmlDocument_0, XmlElement xmlElement_0, object object_0, Dictionary<Type, MemberInfo[]> dictionary_0, Dictionary<object, int> dictionary_1)
    {
      if (dictionary_1.ContainsKey(object_0))
        return;
      dictionary_1.Add(object_0, 0);
      Type type = object_0.GetType();
      string name = this.serializeTypeDictionary_0.GetName(type);
      if (string.IsNullOrEmpty(name))
        throw new Exception("Type '" + type.FullName + "' not registered with the serialization type dictionary.");
      FormatterServices.CheckTypeSecurity(type, TypeFilterLevel.Low);
      SerializationInfo info = new SerializationInfo(type, (IFormatterConverter) new FormatterConverter());
      MemberInfo[] serializableMembers;
      if (dictionary_0.ContainsKey(type))
      {
        serializableMembers = dictionary_0[type];
      }
      else
      {
        serializableMembers = FormatterServices.GetSerializableMembers(type);
        dictionary_0.Add(type, serializableMembers);
      }
      if (object_0 is IEnumerable)
      {
        int num = 0;
        foreach (object obj in object_0 as IEnumerable)
          info.AddValue("item_" + (object) num++, obj);
      }
      else if (object_0 is ISerializable)
      {
        (object_0 as ISerializable).GetObjectData(info, this.streamingContext_0);
      }
      else
      {
        object[] objectData = FormatterServices.GetObjectData(object_0, serializableMembers);
        for (int index = 0; index < objectData.Length; ++index)
          info.AddValue(serializableMembers[index].Name, objectData[index]);
      }
      XmlElement element1 = xmlDocument_0.CreateElement(name);
      xmlElement_0.AppendChild((XmlNode) element1);
      foreach (SerializationEntry serializationEntry in info)
      {
        XmlElement element2 = xmlDocument_0.CreateElement(serializationEntry.Name);
        element1.AppendChild((XmlNode) element2);
        if (!serializationEntry.ObjectType.IsPrimitive && !serializationEntry.ObjectType.IsEnum && !(serializationEntry.Value is string))
          this.method_1(xmlDocument_0, element2, serializationEntry.Value, dictionary_0, dictionary_1);
        else
          element2.InnerText = info.GetString(serializationEntry.Name);
      }
      dictionary_1.Remove(object_0);
    }
  }
}
