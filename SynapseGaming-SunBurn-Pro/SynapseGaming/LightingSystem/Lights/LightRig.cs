// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Lights.LightRig
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;
using Microsoft.Xna.Framework;
using ns3;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Editor;
using SynapseGaming.LightingSystem.Shadows;

namespace SynapseGaming.LightingSystem.Lights
{
    /// <summary>
    /// Light rig container object used for storing, sharing, and organizing scene lights.
    /// </summary>
    [Serializable]
    public class LightRig : IDisposable, IProjectFile, ISerializable, ILightRig, ILightFactory
    {
        internal string LightRigFile = "";
        internal string ProjectFile = "";
        private BaseLightManager baseLightManager_0 = new BaseLightManager();
        private bool IsDisposed;
        private static SerializeTypeDictionary serializeTypeDictionary_0;

        /// <summary>Light groups contained by the light rig.</summary>
        public List<ILightGroup> LightGroups { get; } = new List<ILightGroup>(16);

        /// <summary>The object's current name.</summary>
        public string Name { get; set; }

        /// <summary>
        /// Notifies the editor that this object is partially controlled via code. The editor
        /// will display information to the user indicating some property values are
        /// overridden in code and changes may not take effect.
        /// </summary>
        public bool AffectedInCode { get; set; }


        string IProjectFile.ProjectFile => ProjectFile;

        /// <summary>
        /// Used to support serializing user defined lights and groups. Register any additional
        /// classes and their xml element names to support persisting custom lights, groups, and their contained objects.
        /// </summary>
        public static SerializeTypeDictionary SerializeTypeDictionary
        {
            get
            {
                if (serializeTypeDictionary_0 == null)
                {
                    serializeTypeDictionary_0 = new SerializeTypeDictionary();
                    serializeTypeDictionary_0.RegisterType("PointLight", typeof (PointLight));
                    serializeTypeDictionary_0.RegisterType("DirectionalLight", typeof (DirectionalLight));
                    serializeTypeDictionary_0.RegisterType("SpotLight", typeof (SpotLight));
                    serializeTypeDictionary_0.RegisterType("AmbientLight", typeof (AmbientLight));
                    serializeTypeDictionary_0.RegisterType("LightRig", typeof (LightRig));
                    serializeTypeDictionary_0.RegisterType("LightList", typeof (List<ILight>));
                    serializeTypeDictionary_0.RegisterType("ShadowType", typeof (ShadowType));
                    serializeTypeDictionary_0.RegisterType("Vector3", typeof (Vector3));
                    serializeTypeDictionary_0.RegisterType("LightGroup", typeof (LightGroup));
                    serializeTypeDictionary_0.RegisterType("GroupList", typeof (List<ILightGroup>));
                    serializeTypeDictionary_0.RegisterType("List_x0060_1_-1531622459", typeof (List<ILight>));
                }
                return serializeTypeDictionary_0;
            }
        }

        /// <summary>Creates a LightRig instance.</summary>
        public LightRig()
        {
            LightingSystemEditor.OnCreateResource(this);
        }

        protected LightRig(SerializationInfo serializationInfo_0, StreamingContext streamingContext_0)
        {
            foreach (SerializationEntry serializationEntry in serializationInfo_0)
            {
                switch (serializationEntry.Name)
                {
                    case "LightGroups":
                        this.LightGroups = (List<ILightGroup>) serializationInfo_0.GetValue("LightGroups", typeof (List<ILightGroup>));
                        continue;
                    default:
                        continue;
                }
            }
        }

        /// <summary>
        /// Finds all contained objects that match a set of filter attributes
        /// and overlap with or are contained in a bounding area.
        /// </summary>
        /// <param name="foundobjects">List used to store found objects during the query.</param>
        /// <param name="worldbounds">Bounding area used to limit query results.</param>
        /// <param name="objectfilter">Filter used to limit query results to objects with specific attributes.</param>
        public void Find(List<ILight> foundobjects, BoundingFrustum worldbounds, ObjectFilter objectfilter)
        {
            this.baseLightManager_0.Find(foundobjects, worldbounds, objectfilter);
        }

        /// <summary>
        /// Finds all contained objects that match a set of filter attributes
        /// and overlap with or are contained in a bounding area.
        /// </summary>
        /// <param name="foundobjects">List used to store found objects during the query.</param>
        /// <param name="worldbounds">Bounding area used to limit query results.</param>
        /// <param name="objectfilter">Filter used to limit query results to objects with specific attributes.</param>
        public void Find(List<ILight> foundobjects, BoundingBox worldbounds, ObjectFilter objectfilter)
        {
            this.baseLightManager_0.Find(foundobjects, worldbounds, objectfilter);
        }

        /// <summary>
        /// Finds all contained objects that match a set of filter attributes.
        /// </summary>
        /// <param name="foundobjects">List used to store found objects during the query.</param>
        /// <param name="objectfilter">Filter used to limit query results to objects with specific attributes.</param>
        public void Find(List<ILight> foundobjects, ObjectFilter objectfilter)
        {
            this.baseLightManager_0.Find(foundobjects, objectfilter);
        }

        /// <summary>
        /// Quickly finds all objects near a bounding area without the overhead of
        /// filtering by object type, checking if objects are enabled, or verifying
        /// containment within the bounds.
        /// </summary>
        /// <param name="foundobjects">List used to store found objects during the query.</param>
        /// <param name="worldbounds">Bounding area used to limit query results.</param>
        public void FindFast(List<ILight> foundobjects, BoundingBox worldbounds)
        {
            this.baseLightManager_0.FindFast(foundobjects, worldbounds);
        }

        /// <summary>
        /// Quickly finds all objects without the overhead of filtering by object
        /// type or checking if objects are enabled.
        /// </summary>
        /// <param name="foundobjects">List used to store found objects during the query.</param>
        public void FindFast(List<ILight> foundobjects)
        {
            this.baseLightManager_0.FindFast(foundobjects);
        }

        /// <summary>
        /// Generates approximate lighting for an area in world space. The returned composite
        /// lighting is packed into a single directional and ambient light for fast single-pass lighting.
        /// 
        /// Note: because this information is approximated smaller world space areas will
        /// result in more accurate lighting. Also the approximation is calculated on the
        /// cpu and cannot take into account shadowing.
        /// </summary>
        /// <param name="worldbounds">Bounding area used to determine approximate lighting.</param>
        /// <param name="ambientblend">Blending value (0.0f - 1.0f) that determines how much approximate lighting
        /// contributes to ambient lighting. Approximate lighting can create highly directional lighting, using
        /// a higher blending value can create softer, more realistic lighting.</param>
        /// <returns>Composite lighting packed into a single directional and ambient light.</returns>
        public CompositeLighting GetCompositeLighting(BoundingBox worldbounds, float ambientblend)
        {
            return this.baseLightManager_0.GetCompositeLighting(worldbounds, ambientblend);
        }

        /// <summary>
        /// Generates approximate lighting for an area in world space using a custom set of lights.
        /// The returned composite lighting is packed into a single directional and ambient light for
        /// fast single-pass lighting.
        /// 
        /// Note: because this information is approximated smaller world space areas will
        /// result in more accurate lighting. Also the approximation is calculated on the
        /// cpu and cannot take into account shadowing.
        /// </summary>
        /// <param name="sourcelights">Lights used to generate approximate lighting.</param>
        /// <param name="worldbounds">Bounding area used to determine approximate lighting.</param>
        /// <param name="ambientblend">Blending value (0.0f - 1.0f) that determines how much approximate lighting
        /// contributes to ambient lighting. Approximate lighting can create highly directional lighting, using
        /// a higher blending value can create softer, more realistic lighting.</param>
        /// <returns>Composite lighting packed into a single directional and ambient light.</returns>
        public CompositeLighting GetCompositeLighting(List<ILight> sourcelights, BoundingBox worldbounds, float ambientblend)
        {
            return this.baseLightManager_0.GetCompositeLighting(sourcelights, worldbounds, ambientblend);
        }

        /// <summary>
        /// Applies changes made to contained lights and groups to the light rig's
        /// internal scenegraph.  This must be called after making changes and before
        /// rendering the light rig.
        /// </summary>
        public void CommitChanges()
        {
            this.baseLightManager_0.Clear();
            foreach (ILightGroup lightGroup1 in this.LightGroups)
            {
                if (lightGroup1.ShadowGroup && lightGroup1.ShadowRenderLightsTogether)
                {
                    int num = 0;
                    ILightGroup lightGroup2 = lightGroup1;
                    for (int index = 0; index < lightGroup1.Lights.Count; ++index)
                    {
                        if (num >= BaseLightManager.MaxLightsPerGroup)
                        {
                            LightGroup lightGroup3 = new LightGroup();
                            lightGroup3.method_0(lightGroup2);
                            lightGroup2 = lightGroup3;
                            num = 0;
                        }
                        ILight light = lightGroup1.Lights[index];
                        light.ShadowSource = lightGroup2;
                        this.baseLightManager_0.Submit(light);
                        ++num;
                    }
                }
                else
                {
                    for (int index = 0; index < lightGroup1.Lights.Count; ++index)
                        this.baseLightManager_0.Submit(lightGroup1.Lights[index]);
                }
            }
            this.baseLightManager_0.Optimize();
        }

        /// <summary>Releases resources allocated by this object.</summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            this.Clear();
            this.baseLightManager_0.Dispose();
            if (this.IsDisposed)
                return;
            this.IsDisposed = true;
            LightingSystemEditor.OnDisposeResource(this);
        }

        /// <summary>Saves the object back to its originating file.</summary>
        public void Save()
        {
            SaveToExistingXml(LightRigFile);
        }

        /// <summary>Removes all lights and light groups.</summary>
        public void Clear()
        {
            this.LightGroups.Clear();
            this.baseLightManager_0.Clear();
        }

        /// <summary>Creates an instance of a directional light.</summary>
        /// <returns></returns>
        public ILight CreateDirectionalLight()
        {
            return new DirectionalLight();
        }

        /// <summary>Creates an instance of a point light.</summary>
        /// <returns></returns>
        public ILight CreatePointLight()
        {
            return new PointLight();
        }

        /// <summary>Creates an instance of a spot light.</summary>
        /// <returns></returns>
        public ILight CreateSpotLight()
        {
            return new SpotLight();
        }

        /// <summary>Creates an instance of a ambient light.</summary>
        /// <returns></returns>
        public ILight CreateAmbientLight()
        {
            return new AmbientLight();
        }

        internal void InitFromXml(string xmlData)
        {
            this.Clear();
            var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(xmlData));
            var lightRig = (LightRig)new Class29(SerializeTypeDictionary).Deserialize(memoryStream);
            if (lightRig != null)
            {
                LightGroups.AddRange(lightRig.LightGroups);
                lightRig.Dispose();
            }
            memoryStream.Close();
            memoryStream.Dispose();
            CommitChanges();
        }

        internal void SaveToExistingXml(string xmlFile)
        {
            if (!File.Exists(xmlFile))
                return;
            using (FileStream fileStream = File.Create(xmlFile))
            {
                new Class29(SerializeTypeDictionary).Serialize(fileStream, this);
                fileStream.Flush();
            }
        }

        /// <summary>
        /// Gets some object data I guess?
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("LightGroups", this.LightGroups);
        }
    }
}
