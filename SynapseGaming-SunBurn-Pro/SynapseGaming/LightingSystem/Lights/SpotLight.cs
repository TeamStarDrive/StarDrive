// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Lights.SpotLight
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
    /// Provides spotlight information for rendering lighting and shadows.
    /// </summary>
    [Serializable]
    public class SpotLight : IMovableObject, INamedObject, IEditorObject, ISerializable, ILight, IPointSource, IDirectionalSource, ISpotSource, IShadowSource
    {
        private bool bool_0 = true;
        private Vector3 vector3_0 = new Vector3(0.7f, 0.6f, 0.5f);
        private float float_1 = 1f;
        private float float_2 = 0.5f;
        private float float_3 = 1f;
        private float float_4 = 0.2f;
        private bool bool_2 = true;
        private float LightRadius = 10f;
        private float LightInfluenceAngle = 45f;
        private Matrix WorldTransform = Matrix.Identity;
        private string ObjectName = "";
        private bool bool_1;
        private ObjectType objectType_0;
        private float float_0;
        private ShadowType shadowType_0;
        private float LightVolume;
        private IShadowSource ishadowSource_0;

        /// <summary>
        /// Turns illumination on and off without removing the light from the scene.
        /// </summary>
        public bool Enabled
        {
            get => this.bool_0;
            set => this.bool_0 = value;
        }

        /// <summary>Direct lighting color given off by the light.</summary>
        public Vector3 DiffuseColor
        {
            get => this.vector3_0;
            set => this.vector3_0 = value;
        }

        /// <summary>Intensity of the light.</summary>
        public float Intensity
        {
            get => this.float_1;
            set => this.float_1 = value;
        }

        /// <summary>
        /// Provides softer indirect-like illumination without "hot-spots".
        /// </summary>
        public bool FillLight
        {
            get => this.bool_1;
            set => this.bool_1 = value;
        }

        /// <summary>
        /// Controls how quickly lighting falls off over distance (only available in deferred rendering).
        /// Value ranges from 0.0f to 1.0f.
        /// </summary>
        public float FalloffStrength
        {
            get => this.float_0;
            set => this.float_0 = MathHelper.Clamp(value, 0.0f, 1f);
        }

        /// <summary>
        /// The combined light color and intensity (provided for convenience).
        /// </summary>
        public Vector3 CompositeColorAndIntensity => this.vector3_0 * this.float_1;

        /// <summary>Bounding area of the light's influence.</summary>
        public BoundingBox WorldBoundingBox { get; private set; }

        /// <summary>Bounding area of the light's influence.</summary>
        public BoundingSphere WorldBoundingSphere { get; private set; }

        /// <summary>
        /// Shadow source the light's shadows are generated from.
        /// Allows sharing shadows between point light sources.
        /// </summary>
        public IShadowSource ShadowSource
        {
            get
            {
                if (this.ishadowSource_0 == null)
                    throw new InvalidOperationException("ShadowSource is null. This can result in poor rendering performance.");
                return this.ishadowSource_0;
            }
            set
            {
                if (value == null)
                    this.ishadowSource_0 = this;
                else
                    this.ishadowSource_0 = value;
            }
        }

        /// <summary>
        /// Defines the type of objects that cast shadows from the light.
        /// Does not affect an object's ability to receive shadows.
        /// </summary>
        public ShadowType ShadowType
        {
            get => this.shadowType_0;
            set => this.shadowType_0 = value;
        }

        /// <summary>Position in world space of the shadow source.</summary>
        public Vector3 ShadowPosition => this.WorldTransform.Translation;

        /// <summary>Adjusts the visual quality of casts shadows.</summary>
        public float ShadowQuality
        {
            get => this.float_2;
            set => this.float_2 = MathHelper.Clamp(value, 0.0f, 1f);
        }

        /// <summary>Main property used to eliminate shadow artifacts.</summary>
        public float ShadowPrimaryBias
        {
            get => this.float_3;
            set => this.float_3 = value;
        }

        /// <summary>
        /// Additional fine-tuned property used to eliminate shadow artifacts.
        /// </summary>
        public float ShadowSecondaryBias
        {
            get => this.float_4;
            set => this.float_4 = value;
        }

        /// <summary>
        /// Enables independent level-of-detail per cubemap face on point-based lights.
        /// </summary>
        public bool ShadowPerSurfaceLOD
        {
            get => bool_2;
            set => bool_2 = value;
        }

        /// <summary>Unused.</summary>
        public bool ShadowRenderLightsTogether => false;

        /// <summary>Position in world space of the light.</summary>
        public Vector3 Position
        {
            get => WorldTransform.Translation;
            set
            {
                WorldTransform.Translation = value;
                ++MoveId;
                UpdateBounds();
            }
        }

        /// <summary>
        /// Maximum distance in world space of the light's influence.
        /// </summary>
        public float Radius
        {
            get => LightRadius;
            set
            {
                LightRadius = value;
                UpdateBounds();
            }
        }

        /// <summary>Direction in world space of the light's influence.</summary>
        public Vector3 Direction
        {
            get => WorldTransform.Forward;
            set
            {
                Matrix matrix = Matrix.Identity;
                if (value != Vector3.Zero)
                    matrix = CoreUtils.smethod_14(Vector3.Forward, Vector3.Normalize(value));
                matrix.Translation = WorldTransform.Translation;
                WorldTransform = matrix;
                UpdateBounds();
            }
        }

        /// <summary>Angle in degrees of the light's influence.</summary>
        public float Angle
        {
            get => LightInfluenceAngle;
            set
            {
                LightInfluenceAngle = value;
                UpdateBounds();
            }
        }

        /// <summary>Intensity of the light's 3D light beam.</summary>
        public float Volume
        {
            get => LightVolume;
            set => LightVolume = value;
        }

        /// <summary>World space transform of the light.</summary>
        public Matrix World
        {
            get => WorldTransform;
            set
            {
                WorldTransform = value;
                ++MoveId;
                UpdateBounds();
            }
        }

        /// <summary>
        /// Indicates the object bounding area spans the entire world and
        /// the object is always visible.
        /// </summary>
        public bool InfiniteBounds => false;

        /// <summary>
        /// Indicates the current move. This value increments each time the object
        /// is moved (when the World transform changes).
        /// </summary>
        public int MoveId { get; private set; }

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
            get => this.objectType_0;
            set => this.objectType_0 = value;
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

        /// <summary>Creates a new SpotLight instance.</summary>
        public SpotLight()
        {
            this.ishadowSource_0 = this;
            this.UpdateBounds();
        }

        /// <summary>
        /// Create a new spotlight
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected SpotLight(SerializationInfo serializationInfo_0, StreamingContext streamingContext_0)
        {
            Vector3 gparam_0 = new Vector3();
            foreach (SerializationEntry serializationEntry in serializationInfo_0)
            {
                switch (serializationEntry.Name)
                {
                    case "Enabled":
                        serializationInfo_0.GetValue("Enabled", out this.bool_0);
                        continue;
                    case "DiffuseColor":
                        serializationInfo_0.GetValue("DiffuseColor", out this.vector3_0);
                        continue;
                    case "Intensity":
                        serializationInfo_0.GetValue("Intensity", out this.float_1);
                        continue;
                    case "FillLight":
                        serializationInfo_0.GetValue("FillLight", out this.bool_1);
                        continue;
                    case "FalloffStrength":
                        serializationInfo_0.GetValue("FalloffStrength", out this.float_0);
                        continue;
                    case "ShadowType":
                        serializationInfo_0.GetEnum("ShadowType", out this.shadowType_0);
                        continue;
                    case "Position":
                        serializationInfo_0.GetValue("Position", out gparam_0);
                        this.Position = gparam_0;
                        continue;
                    case "Radius":
                        serializationInfo_0.GetValue("Radius", out this.LightRadius);
                        continue;
                    case "Direction":
                        serializationInfo_0.GetValue("Direction", out gparam_0);
                        this.Direction = gparam_0;
                        continue;
                    case "Angle":
                        serializationInfo_0.GetValue("Angle", out this.LightInfluenceAngle);
                        continue;
                    case "Volume":
                        serializationInfo_0.GetValue("Volume", out this.LightVolume);
                        continue;
                    case "Name":
                        serializationInfo_0.GetValue("Name", out this.ObjectName);
                        continue;
                    case "ShadowQuality":
                        serializationInfo_0.GetValue("ShadowQuality", out this.float_2);
                        continue;
                    case "ShadowPrimaryBias":
                        serializationInfo_0.GetValue("ShadowPrimaryBias", out this.float_3);
                        continue;
                    case "ShadowSecondaryBias":
                        serializationInfo_0.GetValue("ShadowSecondaryBias", out this.float_4);
                        continue;
                    case "ShadowPerSurfaceLOD":
                        serializationInfo_0.GetValue("ShadowPerSurfaceLOD", out this.bool_2);
                        continue;
                    default:
                        continue;
                }
            }
            this.UpdateBounds();
        }

        private void UpdateBounds()
        {
            float num = (float) Math.Tanh(MathHelper.ToRadians(MathHelper.Clamp(this.LightInfluenceAngle, 1f / 1000f, 179.99f) * 0.5f)) * this.LightRadius;
            this.WorldBoundingBox = CoreUtils.smethod_5(new BoundingBox(new Vector3(-num, -num, -this.LightRadius), new Vector3(num, num, 0.0f)), this.WorldTransform);
            this.WorldBoundingSphere = BoundingSphere.CreateFromBoundingBox(this.WorldBoundingBox);
        }

        /// <summary>
        /// Returns a hash code that uniquely identifies the shadow source
        /// and its current state.  Changes to ShadowPosition affects the
        /// hash code, which is used to trigger updates on related shadows.
        /// </summary>
        /// <returns>Shadow hash code.</returns>
        public int GetShadowSourceHashCode()
        {
            return this.ShadowPosition.GetHashCode();
        }

        /// <summary>Returns a String that represents the current Object.</summary>
        /// <returns></returns>
        public override string ToString()
        {
            return CoreUtils.NamedObject(this);
        }

        /// <summary>
        /// Get object data for serialization
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Name", this.Name);
            info.AddValue("Enabled", this.Enabled);
            info.AddValue("DiffuseColor", this.DiffuseColor);
            info.AddValue("Intensity", this.Intensity);
            info.AddValue("FillLight", this.FillLight);
            info.AddValue("FalloffStrength", this.FalloffStrength);
            info.AddValue("ShadowType", this.ShadowType);
            info.AddValue("Position", this.Position);
            info.AddValue("Radius", this.Radius);
            info.AddValue("Direction", this.Direction);
            info.AddValue("Angle", this.Angle);
            info.AddValue("Volume", this.Volume);
            info.AddValue("ShadowQuality", this.ShadowQuality);
            info.AddValue("ShadowPrimaryBias", this.ShadowPrimaryBias);
            info.AddValue("ShadowSecondaryBias", this.ShadowSecondaryBias);
            info.AddValue("ShadowPerSurfaceLOD", this.ShadowPerSurfaceLOD);
        }
    }
}
