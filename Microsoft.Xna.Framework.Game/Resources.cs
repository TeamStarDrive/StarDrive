// Decompiled with JetBrains decompiler
// Type: Microsoft.Xna.Framework.Resources
// Assembly: Microsoft.Xna.Framework.Game, Version=3.1.0.0, Culture=neutral, PublicKeyToken=6d5c3888ef60e27d
// MVID: E4BD910E-73ED-465E-A91E-14AAAB0CE109
// Assembly location: C:\WINDOWS\assembly\GAC_32\Microsoft.Xna.Framework.Game\3.1.0.0__6d5c3888ef60e27d\Microsoft.Xna.Framework.Game.dll

using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace Microsoft.Xna.Framework
{
  [DebuggerNonUserCode]
  [CompilerGenerated]
  [GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "2.0.0.0")]
  internal class Resources
  {
    private static ResourceManager resourceMan;
    private static CultureInfo resourceCulture;

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    internal static ResourceManager ResourceManager
    {
      get
      {
        if (object.ReferenceEquals((object) Microsoft.Xna.Framework.Resources.resourceMan, (object) null))
          Microsoft.Xna.Framework.Resources.resourceMan = new ResourceManager("Microsoft.Xna.Framework.Resources", typeof (Microsoft.Xna.Framework.Resources).Assembly);
        return Microsoft.Xna.Framework.Resources.resourceMan;
      }
    }

    [EditorBrowsable(EditorBrowsableState.Advanced)]
    internal static CultureInfo Culture
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.resourceCulture;
      }
      set
      {
        Microsoft.Xna.Framework.Resources.resourceCulture = value;
      }
    }

    internal static string BackBufferDimMustBePositive
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("BackBufferDimMustBePositive", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string CannotAddSameComponentMultipleTimes
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("CannotAddSameComponentMultipleTimes", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string CannotSetItemsIntoGameComponentCollection
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("CannotSetItemsIntoGameComponentCollection", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string DefaultTitleName
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("DefaultTitleName", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string Direct3DCreateError
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("Direct3DCreateError", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string Direct3DInternalDriverError
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("Direct3DInternalDriverError", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string Direct3DInvalidCreateParameters
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("Direct3DInvalidCreateParameters", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string Direct3DNotAvailable
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("Direct3DNotAvailable", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string GameCannotBeNull
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("GameCannotBeNull", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string GraphicsComponentNotAttachedToGame
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("GraphicsComponentNotAttachedToGame", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string GraphicsDeviceManagerAlreadyPresent
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("GraphicsDeviceManagerAlreadyPresent", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string InactiveSleepTimeCannotBeZero
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("InactiveSleepTimeCannotBeZero", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string InvalidPixelShaderProfile
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("InvalidPixelShaderProfile", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string InvalidScreenAdapter
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("InvalidScreenAdapter", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string InvalidScreenDeviceName
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("InvalidScreenDeviceName", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string InvalidVertexShaderProfile
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("InvalidVertexShaderProfile", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string MissingGraphicsDeviceService
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("MissingGraphicsDeviceService", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string MustCallBeginDeviceChange
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("MustCallBeginDeviceChange", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string NoAudioHardware
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("NoAudioHardware", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string NoCompatibleDevices
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("NoCompatibleDevices", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string NoCompatibleDevicesAfterRanking
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("NoCompatibleDevicesAfterRanking", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string NoDirect3DAcceleration
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("NoDirect3DAcceleration", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string NoDirect3DAccelerationRemoteDesktop
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("NoDirect3DAccelerationRemoteDesktop", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string NoGraphicsDeviceService
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("NoGraphicsDeviceService", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string NoHighResolutionTimer
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("NoHighResolutionTimer", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string NoMultipleRuns
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("NoMultipleRuns", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string NoNullUseDefaultAdapter
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("NoNullUseDefaultAdapter", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string NoPixelShader11OrDDI9Support
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("NoPixelShader11OrDDI9Support", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string NoSuitableGraphicsDevice
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("NoSuitableGraphicsDevice", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string NoSuitableGraphicsDeviceDetails
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("NoSuitableGraphicsDeviceDetails", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string NullOrEmptyScreenDeviceName
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("NullOrEmptyScreenDeviceName", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string PreviousDrawThrew
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("PreviousDrawThrew", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string PropertyCannotBeCalledBeforeInitialize
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("PropertyCannotBeCalledBeforeInitialize", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string ServiceAlreadyPresent
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("ServiceAlreadyPresent", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string ServiceMustBeAssignable
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("ServiceMustBeAssignable", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string ServiceProviderCannotBeNull
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("ServiceProviderCannotBeNull", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string ServiceTypeCannotBeNull
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("ServiceTypeCannotBeNull", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string TargetElaspedCannotBeZero
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("TargetElaspedCannotBeZero", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string TitleCannotBeNull
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("TitleCannotBeNull", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string ValidateAutoDepthStencilAdapterGroup
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("ValidateAutoDepthStencilAdapterGroup", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string ValidateAutoDepthStencilFormatIncompatible
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("ValidateAutoDepthStencilFormatIncompatible", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string ValidateAutoDepthStencilFormatInvalid
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("ValidateAutoDepthStencilFormatInvalid", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string ValidateAutoDepthStencilMismatch
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("ValidateAutoDepthStencilMismatch", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string ValidateBackBufferCount
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("ValidateBackBufferCount", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string ValidateBackBufferCountSwapCopy
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("ValidateBackBufferCountSwapCopy", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string ValidateBackBufferDimsFullScreen
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("ValidateBackBufferDimsFullScreen", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string ValidateBackBufferDimsModeFullScreen
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("ValidateBackBufferDimsModeFullScreen", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string ValidateBackBufferFormatIsInvalid
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("ValidateBackBufferFormatIsInvalid", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string ValidateBackBufferHzModeFullScreen
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("ValidateBackBufferHzModeFullScreen", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string ValidateDepthStencilFormatIsInvalid
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("ValidateDepthStencilFormatIsInvalid", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string ValidateDeviceType
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("ValidateDeviceType", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string ValidateMultiSampleQualityInvalid
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("ValidateMultiSampleQualityInvalid", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string ValidateMultiSampleSwapEffect
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("ValidateMultiSampleSwapEffect", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string ValidateMultiSampleTypeInvalid
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("ValidateMultiSampleTypeInvalid", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string ValidatePresentationIntervalIncompatibleInFullScreen
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("ValidatePresentationIntervalIncompatibleInFullScreen", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string ValidatePresentationIntervalInFullScreen
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("ValidatePresentationIntervalInFullScreen", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string ValidatePresentationIntervalInWindow
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("ValidatePresentationIntervalInWindow", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string ValidatePresentationIntervalOnXbox
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("ValidatePresentationIntervalOnXbox", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string ValidateRefreshRateInFullScreen
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("ValidateRefreshRateInFullScreen", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string ValidateRefreshRateInWindow
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("ValidateRefreshRateInWindow", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal static string ValidateSwapEffectInvalid
    {
      get
      {
        return Microsoft.Xna.Framework.Resources.ResourceManager.GetString("ValidateSwapEffectInvalid", Microsoft.Xna.Framework.Resources.resourceCulture);
      }
    }

    internal Resources()
    {
    }
  }
}
