// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Shadows.BaseShadowManager
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Lights;

namespace SynapseGaming.LightingSystem.Shadows
{
    /// <summary>Provides base scene shadow management support.</summary>
    public abstract class BaseShadowManager : IUnloadable, IManager, IRenderableManager
    {
        private static ShadowSource shadowSource_0 = new ShadowSource();
        private static DirectionalLight directionalLight_0 = new DirectionalLight();
        private static ShadowGroup shadowGroup_0 = new ShadowGroup();
        private static ShadowGroup shadowGroup_1 = new ShadowGroup();
        private static Dictionary<IShadowSource, ShadowGroup> dictionary_0 = new Dictionary<IShadowSource, ShadowGroup>(32);
        private TrackingPool<ShadowGroup> TrackingPool0 = new TrackingPool<ShadowGroup>();

        /// <summary>
        /// The current GraphicsDeviceManager used by this object.
        /// </summary>
        public IGraphicsDeviceService GraphicsDeviceManager { get; }

        /// <summary>The current SceneState used by this object.</summary>
        protected ISceneState SceneState { get; private set; } = (ISceneState)new SceneState();

        /// <summary>Creates a new BaseShadowManager instance.</summary>
        /// <param name="graphicsdevicemanager"></param>
        public BaseShadowManager(IGraphicsDeviceService graphicsdevicemanager)
        {
            GraphicsDeviceManager = graphicsdevicemanager;
        }

        /// <summary>
        /// Use to apply user quality and performance preferences to the resources managed by this object.
        /// </summary>
        /// <param name="preferences"></param>
        public virtual void ApplyPreferences(ILightingSystemPreferences preferences)
        {
        }

        /// <summary>
        /// Sets up frame information necessary for scene shadowing.
        /// </summary>
        public virtual void BeginFrameRendering(ISceneState scenestate)
        {
            //SplashScreen.CheckProductActivation();
            SceneState = scenestate;
        }

        /// <summary>Cleans up frame information.</summary>
        public virtual void EndFrameRendering()
        {
            TrackingPool0.RecycleAllTracked();
        }

        /// <summary>
        /// Builds a list of shadow groups based on the provided light list.  Shadow
        /// groups contain a list of all lights that share a common shadow source.
        /// </summary>
        /// <param name="shadowgroups">Destination shadow group list.</param>
        /// <param name="lights">Source light list.</param>
        /// <param name="usedefaultgrouping">Determines if ungrouped lights should be placed in a
        /// single default group (recommended: true for deferred rendering and false for forward).</param>
        protected void BuildShadowGroups(List<ShadowGroup> shadowgroups, List<ILight> lights, bool usedefaultgrouping)
        {
            dictionary_0.Clear();
            shadowSource_0.ShadowType = ShadowType.None;
            shadowGroup_0.Shadow = null;
            shadowGroup_0.Lights.Clear();
            dictionary_0.Add(shadowSource_0, shadowGroup_0);
            directionalLight_0.ShadowType = ShadowType.None;
            shadowGroup_1.Shadow = null;
            shadowGroup_1.Lights.Clear();
            dictionary_0.Add(directionalLight_0, shadowGroup_1);
            foreach (ILight light in lights)
            {
                IShadowSource shadowSource = light.ShadowSource;
                if (usedefaultgrouping && (shadowSource == null || light == shadowSource && shadowSource.ShadowType == ShadowType.None))
                {
                    if (light is IPointSource)
                        shadowGroup_0.Lights.Add(light);
                    else
                        shadowGroup_1.Lights.Add(light);
                }
                else
                {
                    if (!dictionary_0.TryGetValue(shadowSource, out ShadowGroup shadowGroup))
                    {
                        shadowGroup = TrackingPool0.New();
                        shadowGroup.Shadow = null;
                        shadowGroup.Lights.Clear();
                        dictionary_0.Add(shadowSource, shadowGroup);
                    }
                    shadowGroup.Lights.Add(light);
                }
            }
            if (shadowGroup_1.Lights.Count <= 0)
                dictionary_0.Remove(directionalLight_0);
            if (shadowGroup_0.Lights.Count <= 0)
                dictionary_0.Remove(shadowSource_0);
            else
                shadowSource_0.Position = (shadowGroup_0.Lights[0] as IPointSource).Position;
            foreach (KeyValuePair<IShadowSource, ShadowGroup> keyValuePair in dictionary_0)
            {
                keyValuePair.Value.Build(keyValuePair.Key, SceneState);
                shadowgroups.Add(keyValuePair.Value);
            }
        }

        /// <summary>
        /// Removes resources managed by this object. Commonly used while clearing the scene.
        /// </summary>
        public abstract void Clear();

        /// <summary>
        /// Disposes any graphics resource used internally by this object, and removes
        /// scene resources managed by this object. Commonly used during Game.UnloadContent.
        /// </summary>
        public abstract void Unload();
    }
}
