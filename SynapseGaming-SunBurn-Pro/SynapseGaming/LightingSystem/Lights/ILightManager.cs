// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Lights.ILightManager
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;
using SynapseGaming.LightingSystem.Core;
using SynapseGaming.LightingSystem.Rendering.Deferred;
using SynapseGaming.LightingSystem.Shadows;

namespace SynapseGaming.LightingSystem.Lights
{
  /// <summary>
  /// Interface that provides access to the scene's light manager. The light manager
  /// provides methods for storing and querying scene lights.
  /// </summary>
  public interface ILightManager : ISubmit<ILight>, ISubmit<ILightRig>, IWorldRenderableManager, IManagerService, ILightQuery
  {
    /// <summary>
    /// Helper method that creates and submits a static light
    /// using a method layout similar to SunBurn 1.2.x.
    /// </summary>
    /// <param name="diffusecolor">Direct lighting color given off by the light.</param>
    /// <param name="direction">Direction the light is pointing.</param>
    /// <param name="intensity">Intensity of the light.</param>
    /// <param name="shadowtype">Defines the type of objects that cast shadows from the light.
    /// Does not affect an object's ability to receive shadows.</param>
    /// <param name="shadowquality">Visual quality of casts shadows.</param>
    /// <param name="shadowprimarybias">Main property used to eliminate shadow artifacts.</param>
    /// <param name="shadowsecondarybias">Additional fine-tuned property used to eliminate shadow artifacts.</param>
    void SubmitStaticDirectionalLight(Vector3 diffusecolor, Vector3 direction, float intensity, ShadowType shadowtype, float shadowquality, float shadowprimarybias, float shadowsecondarybias);

    /// <summary>
    /// Helper method that creates and submits a static light
    /// using a method layout similar to SunBurn 1.2.x.
    /// </summary>
    /// <param name="diffusecolor">Direct lighting color given off by the light.</param>
    /// <param name="position">Position in world space of the light.</param>
    /// <param name="intensity">Intensity of the light.</param>
    /// <param name="radius">Lighting radius in world space.</param>
    /// <param name="filllight">Provides softer indirect-like illumination without "hot-spots".</param>
    /// <param name="falloffstrength">Controls how quickly lighting falls off over distance (only available in deferred rendering).</param>
    /// <param name="shadowtype">Defines the type of objects that cast shadows from the light.
    /// Does not affect an object's ability to receive shadows.</param>
    /// <param name="shadowquality">Visual quality of casts shadows.</param>
    /// <param name="shadowprimarybias">Main property used to eliminate shadow artifacts.</param>
    /// <param name="shadowsecondarybias">Additional fine-tuned property used to eliminate shadow artifacts.</param>
    /// <param name="shadowsource">Shadow source the light's shadows are generated from.
    /// Allows sharing shadows between point light sources.  Setting the parameter
    /// to null makes the light its own unique shadow source.</param>
    void SubmitStaticPointLight(Vector3 diffusecolor, Vector3 position, float intensity, float radius, bool filllight, float falloffstrength, ShadowType shadowtype, float shadowquality, float shadowprimarybias, float shadowsecondarybias, IShadowSource shadowsource);

    /// <summary>
    /// Helper method that creates and submits a static light
    /// using a method layout similar to SunBurn 1.2.x.
    /// </summary>
    /// <param name="diffusecolor">Direct lighting color given off by the light.</param>
    /// <param name="position">Position in world space of the light.</param>
    /// <param name="intensity">Intensity of the light.</param>
    /// <param name="radius">Lighting radius in world space.</param>
    /// <param name="direction">Direction the light is pointing.</param>
    /// <param name="angle">Angle in degrees of the light's influence.</param>
    /// <param name="filllight">Provides softer indirect-like illumination without "hot-spots".</param>
    /// <param name="falloffstrength">Controls how quickly lighting falls off over distance (only available in deferred rendering).</param>
    /// <param name="shadowtype">Defines the type of objects that cast shadows from the light.
    /// Does not affect an object's ability to receive shadows.</param>
    /// <param name="shadowquality">Visual quality of casts shadows.</param>
    /// <param name="shadowprimarybias">Main property used to eliminate shadow artifacts.</param>
    /// <param name="shadowsecondarybias">Additional fine-tuned property used to eliminate shadow artifacts.</param>
    /// <param name="shadowsource">Shadow source the light's shadows are generated from.
    /// Allows sharing shadows between point light sources.  Setting the parameter
    /// to null makes the light its own unique shadow source.</param>
    void SubmitStaticSpotLight(Vector3 diffusecolor, Vector3 position, float intensity, float radius, Vector3 direction, float angle, bool filllight, float falloffstrength, ShadowType shadowtype, float shadowquality, float shadowprimarybias, float shadowsecondarybias, IShadowSource shadowsource);

    /// <summary>
    /// Helper method that creates and submits a static light
    /// using a method layout similar to SunBurn 1.2.x.
    /// </summary>
    /// <param name="diffusecolor">Direct lighting color given off by the light.</param>
    /// <param name="intensity">Intensity of the light.</param>
    void SubmitStaticAmbientLight(Vector3 diffusecolor, float intensity);

    /// <summary>Renders volume lighting for the contained lights.</summary>
    /// <param name="deferredbuffers">The deferred buffer used during the
    /// rendering pass, or null if forward rendering.</param>
    void RenderVolumeLights(DeferredBuffers deferredbuffers);

    /// <summary>
    /// Auto-detects moved dynamic objects and repositions them in the storage tree / scenegraph.
    /// This method is used when the container implements a tree or graph, and relocates all
    /// dynamic objects within that structure often due to a change in object world position.
    /// </summary>
    new void MoveDynamicObjects();

    /// <summary>
    /// Removes all objects from the container. Commonly used while clearing the scene.
    /// </summary>
    new void Clear();
  }
}
