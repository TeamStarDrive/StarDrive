// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.LightingSystemPreferences
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Permissions;
using ns3;

namespace SynapseGaming.LightingSystem.Core
{
    /// <summary>
    /// Provides user and hardware specific preferences to the Lighting System.
    /// </summary>
    [Serializable]
    public class LightingSystemPreferences : IPreferences, ISerializable, ILightingSystemPreferences
    {
        SamplingPreference textureSampling = SamplingPreference.Trilinear;
        int maxAnisotropy = 4;
        DetailPreference shadowDetail = DetailPreference.Medium;
        float shadowQuality = 1f;
        DetailPreference textureQuality;
        DetailPreference effectDetail;
        DetailPreference postProcessingDetail;
        static SerializeTypeDictionary serializeTypeDictionary_0;

        /// <summary>
        /// Sets the user preferred balance of texture sampling quality and performance.
        /// </summary>
        public SamplingPreference TextureSampling
        {
            get => textureSampling;
            set => textureSampling = value;
        }

        /// <summary>
        /// Sets the user preferred balance of texture resolution and performance.
        /// </summary>
        public DetailPreference TextureQuality
        {
            get => textureQuality;
            set => textureQuality = value;
        }

        /// <summary>
        /// Sets the maximum anisotropy level when TextureSampling is set to Anisotropic.
        /// </summary>
        public int MaxAnisotropy
        {
            get => maxAnisotropy;
            set => maxAnisotropy = value;
        }

        /// <summary>
        /// Sets the user preferred balance of shadow filtering quality and performance.
        /// </summary>
        public DetailPreference ShadowDetail
        {
            get => shadowDetail;
            set => shadowDetail = value;
        }

        /// <summary>
        /// Sets the user preferred balance of shadow resolution and performance.
        /// </summary>
        public float ShadowQuality
        {
            get => shadowQuality;
            set => shadowQuality = value;
        }

        /// <summary>
        /// Sets the user preferred balance of LightingEffect detail and performance.
        /// </summary>
        public DetailPreference EffectDetail
        {
            get => effectDetail;
            set => effectDetail = value;
        }

        /// <summary>
        /// Sets the user preferred balance of post-processing effect detail and performance.
        /// </summary>
        public DetailPreference PostProcessingDetail
        {
            get => postProcessingDetail;
            set => postProcessingDetail = value;
        }

        /// <summary>
        /// Used to support serializing user defined preferences. Register any additional
        /// classes and their xml element names to support persisting custom preference objects.
        /// </summary>
        public static SerializeTypeDictionary SerializeTypeDictionary
        {
            get
            {
                if (serializeTypeDictionary_0 == null)
                {
                    serializeTypeDictionary_0 = new SerializeTypeDictionary();
                    serializeTypeDictionary_0.RegisterType("Preferences", typeof(LightingSystemPreferences));
                    serializeTypeDictionary_0.RegisterType("Sampling", typeof(SamplingPreference));
                    serializeTypeDictionary_0.RegisterType("Detail", typeof(DetailPreference));
                }
                return serializeTypeDictionary_0;
            }
        }

        /// <summary>Creates a new LightingSystemPreferences object.</summary>
        public LightingSystemPreferences()
        {
        }

        /// <summary />
        protected LightingSystemPreferences(SerializationInfo info, StreamingContext context)
        {
            foreach (SerializationEntry serializationEntry in info)
            {
                switch (serializationEntry.Name)
                {
                    case "TextureSampling": info.GetEnum("TextureSampling", out textureSampling); continue;
                    case "TextureQuality":  info.GetEnum("TextureQuality",  out textureQuality);  continue;
                    case "MaxAnisotropy":   info.GetValue("MaxAnisotropy",  out maxAnisotropy);           continue;
                    case "ShadowDetail":    info.GetEnum("ShadowDetail",    out shadowDetail);    continue;
                    case "ShadowQuality":   info.GetValue("ShadowQuality",  out shadowQuality);   continue;
                    case "EffectDetail":    info.GetEnum("EffectDetail",    out effectDetail);    continue;
                    case "PostProcessingDetail": info.GetEnum("PostProcessingDetail", out postProcessingDetail); continue;
                    default: continue;
                }
            }
        }

        /// <summary>
        /// Loads preferences from file (available on Windows only – Xbox 360 implementations
        /// using LightingSystemPreferences should set preferences via code as all target
        /// hardware is the same).
        /// </summary>
        /// <param name="filename">Path and name of file.</param>
        public void LoadFromFile(string filename)
        {
            FileStream fileStream = File.OpenRead(filename);
            object object_0 = new Class29(SerializeTypeDictionary).Deserialize(fileStream);
            if (object_0 != null)
                Class12.smethod_1(object_0, this);
            fileStream.Flush();
            fileStream.Close();
            fileStream.Dispose();
        }

        /// <summary>
        /// Saves preferences to file (available on Windows only – Xbox 360 implementations
        /// using LightingSystemPreferences should set preferences via code as all target
        /// hardware is the same).
        /// </summary>
        /// <param name="filename">Path and name of file.</param>
        public void SaveToFile(string filename)
        {
            FileStream fileStream = File.Create(filename);
            new Class29(SerializeTypeDictionary).Serialize(fileStream, this);
            fileStream.Flush();
            fileStream.Close();
            fileStream.Dispose();
        }

        /// <summary />
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("TextureSampling", textureSampling);
            info.AddValue("TextureQuality", textureQuality);
            info.AddValue("MaxAnisotropy", maxAnisotropy);
            info.AddValue("ShadowDetail", shadowDetail);
            info.AddValue("ShadowQuality", shadowQuality);
            info.AddValue("EffectDetail", effectDetail);
            info.AddValue("PostProcessingDetail", postProcessingDetail);
        }
    }
}
