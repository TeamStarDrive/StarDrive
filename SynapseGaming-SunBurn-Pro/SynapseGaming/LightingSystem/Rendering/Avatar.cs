// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Rendering.Avatar
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.GamerServices;
using ns3;
using SynapseGaming.LightingSystem.Core;

namespace SynapseGaming.LightingSystem.Rendering
{
    /// <summary>
    /// Avatar implementation that provides properties necessary for avatar
    /// rendering.
    /// </summary>
    public class Avatar : IMovableObject, IAvatar
    {
        private static List<Matrix> list_0 = new List<Matrix>();
        private const int int_0 = 71;
        private Matrix matrix_0;
        private BoundingBox boundingBox_0;
        private BoundingBox boundingBox_1;

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
        public ObjectType ObjectType { get; set; }

        /// <summary>World space transform of the object.</summary>
        public Matrix World
        {
            get => this.matrix_0;
            set
            {
                if (this.matrix_0.Equals(value))
                    return;
                this.matrix_0 = value;
                ++this.MoveId;
                this.method_0();
            }
        }

        /// <summary>
        /// Array of bone transforms for the skeleton's current pose. The matrix index is the
        /// same as the bone order used by the avatar.
        /// </summary>
        public IList<Matrix> SkinBones { get; set; }

        /// <summary>The current avatar facial expression.</summary>
        public AvatarExpression Expression { get; set; }

        /// <summary>
        /// Defines how the avatar is rendered.
        /// 
        /// This enumeration is a Flag, which allows combining multiple values using the
        /// Logical OR operator (example: "ObjectVisibility.Rendered | ObjectVisibility.CastShadows",
        /// both renders the avatar and casts shadows from it).
        /// </summary>
        public ObjectVisibility Visibility { get; set; } = ObjectVisibility.RenderedAndCastShadows;

        /// <summary>World space bounding area of the object.</summary>
        public BoundingBox WorldBoundingBox { get; private set; }

        /// <summary>World space bounding area of the object.</summary>
        public BoundingSphere WorldBoundingSphere { get; private set; }

        /// <summary>
        /// Extended world space bounding area of the object. This area is roughly twice the size
        /// to accommodate avatar animations that fall outside the normal bounds.
        /// </summary>
        public BoundingBox WorldBoundingBoxProxy { get; private set; }

        /// <summary>AvatarRenderer used to render the avatar.</summary>
        public AvatarRenderer Renderer { get; }

        /// <summary>
        /// Description of the avatar size, clothing, features, and more.
        /// </summary>
        public AvatarDescription Description { get; }

        /// <summary>
        /// Determines if the avatar casts shadows base on the current ObjectVisibility options.
        /// </summary>
        public bool CastShadows => (this.Visibility & ObjectVisibility.CastShadows) != ObjectVisibility.None;

        /// <summary>
        /// Determines if the avatar is visible base on the current ObjectVisibility options.
        /// </summary>
        public bool Visible => (this.Visibility & ObjectVisibility.Rendered) != ObjectVisibility.None;

        /// <summary>Creates a new Avatar instance.</summary>
        /// <param name="avatarrenderer">AvatarRenderer used to render the avatar.</param>
        /// <param name="description">Description of the avatar size, clothing, features, and more.</param>
        public Avatar(AvatarRenderer avatarrenderer, AvatarDescription description)
        {
            if (list_0.Count < 71)
            {
                for (int count = list_0.Count; count < 71; ++count)
                    list_0.Add(Matrix.Identity);
            }
            this.SkinBones = list_0;
            this.Renderer = avatarrenderer;
            this.Description = description;
            this.boundingBox_0 = new BoundingBox(new Vector3(-0.5f, 0.0f, -0.5f), new Vector3(0.5f, this.Description.Height, 0.5f));
            this.boundingBox_1 = CoreUtils.smethod_5(this.boundingBox_0, Matrix.CreateScale(2f));
            this.matrix_0 = Matrix.Identity;
            this.method_0();
        }

        /// <summary>
        /// Sets both the avatar bone transforms and expression using an AvatarAnimation object.
        /// </summary>
        /// <param name="animation"></param>
        public void ApplyAnimation(AvatarAnimation animation)
        {
            this.SkinBones = animation.BoneTransforms;
            this.Expression = animation.Expression;
        }

        private void method_0()
        {
            this.WorldBoundingBox = CoreUtils.smethod_5(this.boundingBox_0, this.matrix_0);
            this.WorldBoundingSphere = BoundingSphere.CreateFromBoundingBox(this.WorldBoundingBox);
            this.WorldBoundingBoxProxy = CoreUtils.smethod_5(this.boundingBox_1, this.matrix_0);
        }
    }
}
