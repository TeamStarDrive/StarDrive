// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Lights.AmbientLight
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Runtime.Serialization;
using System.Security.Permissions;
using Microsoft.Xna.Framework;
using ns3;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Shadows;

namespace SynapseGaming.LightingSystem.Lights
{
    /// <summary>
    /// Provides ambient light information for rendering lighting.
    /// </summary>
    [Serializable]
    public class AmbientLight : INamedObject, IEditorObject, ISerializable, ILight, IAmbientSource
    {
        private static readonly BoundingBox InfiniteBBox = new BoundingBox(new Vector3(float.MinValue, float.MinValue, float.MinValue), new Vector3(float.MaxValue, float.MaxValue, float.MaxValue));
        private static readonly BoundingSphere InfiniteBSphere = new BoundingSphere(new Vector3(), float.MaxValue);
        private bool IsEnabled = true;
        private Vector3 Diffuse = new Vector3(0.0f, 0.0f, 0.0f);
        private float LightIntensity = 1f;
        private float NormalDepth = 0.15f;
        private string ObjectName = "";

        /// <summary>
        /// Turns illumination on and off without removing the light from the scene.
        /// </summary>
        public bool Enabled
        {
            get => IsEnabled;
            set => IsEnabled = value;
        }

        /// <summary>Direct lighting color given off by the light.</summary>
        public Vector3 DiffuseColor
        {
            get => Diffuse;
            set => Diffuse = value;
        }

        /// <summary>Intensity of the light.</summary>
        public float Intensity
        {
            get => LightIntensity;
            set => LightIntensity = value;
        }

        /// <summary>Unused.</summary>
        public bool FillLight
        {
            get
            {
                return true;
            }
            set
            {
            }
        }

        /// <summary>
        /// Controls how quickly lighting falls off over distance (unused in this light type).
        /// </summary>
        public float FalloffStrength
        {
            get
            {
                return 0.0f;
            }
            set
            {
            }
        }

        /// <summary>
        /// The combined light color and intensity (provided for convenience).
        /// </summary>
        public Vector3 CompositeColorAndIntensity => Diffuse * LightIntensity;

        /// <summary>Bounding area of the light's influence.</summary>
        public BoundingBox WorldBoundingBox => InfiniteBBox;

        /// <summary>Bounding area of the light's influence.</summary>
        public BoundingSphere WorldBoundingSphere => InfiniteBSphere;

        /// <summary>Shadow source the light's shadows are generated from.</summary>
        public IShadowSource ShadowSource
        {
            get
            {
                return null;
            }
            set
            {
            }
        }

        /// <summary>World space transform of the light.</summary>
        public Matrix World
        {
            get
            {
                return Matrix.Identity;
            }
            set
            {
            }
        }

        /// <summary>
        /// Indicates the object bounding area spans the entire world and
        /// the object is always visible.
        /// </summary>
        public bool InfiniteBounds => true;

        /// <summary>
        /// Indicates the current move. This value increments each time the object
        /// is moved (when the World transform changes).
        /// </summary>
        public int MoveId => 0;

        /// <summary>
        /// Defines how movement is applied. Updates to Dynamic objects
        /// are automatically applied, where Static objects must be moved
        /// manually using [manager].Move().
        /// 
        /// Important note: ObjectType can be changed at any time, HOWEVER managers
        /// will only see the change after removing and resubmitting the object.
        /// </summary>
        public ObjectType ObjectType
        {
            get
            {
                return ObjectType.Static;
            }
            set
            {
            }
        }

        /// <summary>The object's current name.</summary>
        public string Name
        {
            get => ObjectName;
            set => ObjectName = value;
        }

        /// <summary>
        /// Notifies the editor that this object is partially controlled via code. The editor
        /// will display information to the user indicating some property values are
        /// overridden in code and changes may not take effect.
        /// </summary>
        public bool AffectedInCode { get; set; }

        /// <summary>
        /// Increases the detail of normal mapped surfaces during the ambient lighting pass (deferred rendering only).
        /// </summary>
        public float Depth
        {
            get => NormalDepth;
            set => NormalDepth = MathHelper.Clamp(value, 0.0f, 0.5f);
        }

        /// <summary>Creates a new AmbientLight instance.</summary>
        public AmbientLight()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected AmbientLight(SerializationInfo info, StreamingContext context)
        {
            foreach (SerializationEntry serializationEntry in info)
            {
                switch (serializationEntry.Name)
                {
                    case "Enabled":      info.GetValue("Enabled", out IsEnabled); continue;
                    case "DiffuseColor": info.GetValue("DiffuseColor", out Diffuse); continue;
                    case "Intensity":    info.GetValue("Intensity", out LightIntensity); continue;
                    case "Name":       info.GetValue("Name", out ObjectName); continue;
                    case "Depth":      info.GetValue("Depth", out float depth);
                        Depth = depth; continue;
                    default:
                        continue;
                }
            }
        }

        /// <summary>Returns a String that represents the current Object.</summary>
        /// <returns></returns>
        public override string ToString()
        {
            return CoreUtils.NamedObject(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Name", Name);
            info.AddValue("Enabled", Enabled);
            info.AddValue("DiffuseColor", DiffuseColor);
            info.AddValue("Intensity", Intensity);
            info.AddValue("Depth", Depth);
        }
    }
}
