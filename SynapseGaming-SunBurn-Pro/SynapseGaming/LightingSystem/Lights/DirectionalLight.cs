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
    /// Provides directional light (sunlight) information for rendering lighting and shadows.
    /// </summary>
    [Serializable]
    public class DirectionalLight : INamedObject, IEditorObject, ISerializable, ILight, IDirectionalSource, IShadowSource
    {
        private static readonly BoundingBox InfiniteBBox = new BoundingBox(new Vector3(float.MinValue, float.MinValue, float.MinValue), new Vector3(float.MaxValue, float.MaxValue, float.MaxValue));
        private static readonly BoundingSphere InfineBSphere = new BoundingSphere(new Vector3(), float.MaxValue);
        private bool IsEnabled = true;
        private Vector3 Diffuse = new Vector3(0.7f, 0.6f, 0.5f);
        private float LightIntensity = 1f;
        private ShadowType SType = ShadowType.AllObjects;
        private float SQuality = 1f;
        private float SPrimaryBias = 1f;
        private float SSecondaryBias = 0.2f;
        private bool SPerSurfaceLod = true;
        private Matrix WorldMatrix = Matrix.Identity;
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
                return false;
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
        public BoundingSphere WorldBoundingSphere => InfineBSphere;

        /// <summary>Shadow source the light's shadows are generated from.</summary>
        public IShadowSource ShadowSource
        {
            get
            {
                return this;
            }
            set
            {
            }
        }

        /// <summary>
        /// Defines the type of objects that cast shadows from the light.
        /// Does not affect an object's ability to receive shadows.
        /// </summary>
        public ShadowType ShadowType
        {
            get => SType;
            set => SType = value;
        }

        /// <summary>Position in world space of the shadow source.</summary>
        public Vector3 ShadowPosition => Direction * -1000000f;

        /// <summary>Adjusts the visual quality of casts shadows.</summary>
        public float ShadowQuality
        {
            get => SQuality;
            set => SQuality = MathHelper.Clamp(value, 0.0f, 2f);
        }

        /// <summary>Main property used to eliminate shadow artifacts.</summary>
        public float ShadowPrimaryBias
        {
            get => SPrimaryBias;
            set => SPrimaryBias = value;
        }

        /// <summary>
        /// Additional fine-tuned property used to eliminate shadow artifacts.
        /// </summary>
        public float ShadowSecondaryBias
        {
            get => SSecondaryBias;
            set => SSecondaryBias = value;
        }

        /// <summary>
        /// Enables independent level-of-detail per cubemap face on point-based lights.
        /// </summary>
        public bool ShadowPerSurfaceLOD
        {
            get => SPerSurfaceLod;
            set => SPerSurfaceLod = value;
        }

        /// <summary>Unused.</summary>
        public bool ShadowRenderLightsTogether => false;

        /// <summary>Direction in world space of the light's influence.</summary>
        public Vector3 Direction
        {
            get
            {
                return WorldMatrix.Forward;
            }
            set
            {
                if (value == Vector3.Zero)
                    WorldMatrix = Matrix.Identity;
                else
                    WorldMatrix = CoreUtils.smethod_14(Vector3.Forward, Vector3.Normalize(value));
            }
        }

        /// <summary>World space transform of the light.</summary>
        public Matrix World
        {
            get => WorldMatrix;
            set
            {
                WorldMatrix = value;
                WorldMatrix.Translation = Vector3.Zero;
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

        /// <summary>Creates a new DirectionalLight instance.</summary>
        public DirectionalLight()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected DirectionalLight(SerializationInfo info, StreamingContext context)
        {
            foreach (SerializationEntry serializationEntry in info)
            {
                switch (serializationEntry.Name)
                {
                    case "Enabled":      info.GetValue("Enabled", out IsEnabled); continue;
                    case "DiffuseColor": info.GetValue("DiffuseColor", out Diffuse); continue;
                    case "Intensity":    info.GetValue("Intensity", out LightIntensity); continue;
                    case "ShadowType":   info.GetEnum("ShadowType", out SType); continue;
                    case "Direction":    info.GetValue("Direction", out Vector3 direction); Direction = direction; continue;
                    case "Name":         info.GetValue("Name", out ObjectName); continue;
                    case "ShadowQuality":       info.GetValue("ShadowQuality", out SQuality); continue;
                    case "ShadowPrimaryBias":   info.GetValue("ShadowPrimaryBias", out SPrimaryBias); continue;
                    case "ShadowSecondaryBias": info.GetValue("ShadowSecondaryBias", out SSecondaryBias); continue;
                    case "ShadowPerSurfaceLOD": info.GetValue("ShadowPerSurfaceLOD", out SPerSurfaceLod); continue;
                    default: continue;
                }
            }
        }

        /// <summary>
        /// Returns a hash code that uniquely identifies the shadow source
        /// and its current state.  Changes to ShadowPosition affects the
        /// hash code, which is used to trigger updates on related shadows.
        /// </summary>
        /// <returns>Shadow hash code.</returns>
        public int GetShadowSourceHashCode()
        {
            return ShadowPosition.GetHashCode();
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
            info.AddValue("ShadowType", ShadowType);
            info.AddValue("Direction", Direction);
            info.AddValue("ShadowQuality", ShadowQuality);
            info.AddValue("ShadowPrimaryBias", ShadowPrimaryBias);
            info.AddValue("ShadowSecondaryBias", ShadowSecondaryBias);
            info.AddValue("ShadowPerSurfaceLOD", ShadowPerSurfaceLOD);
        }
    }
}
