// Decompiled with JetBrains decompiler
// Type: Microsoft.Xna.Framework.GraphicsDeviceManager
// Assembly: Microsoft.Xna.Framework.Game, Version=3.1.0.0, Culture=neutral, PublicKeyToken=6d5c3888ef60e27d
// MVID: E4BD910E-73ED-465E-A91E-14AAAB0CE109
// Assembly location: C:\WINDOWS\assembly\GAC_32\Microsoft.Xna.Framework.Game\3.1.0.0__6d5c3888ef60e27d\Microsoft.Xna.Framework.Game.dll

using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.Xna.Framework
{
  public class GraphicsDeviceManager : IGraphicsDeviceService, IDisposable, IGraphicsDeviceManager
  {
    public static readonly int DefaultBackBufferWidth = 800;
    public static readonly int DefaultBackBufferHeight = 600;
    public static readonly DeviceType[] ValidDeviceTypes = new DeviceType[1]
    {
      DeviceType.Hardware
    };
    public static readonly SurfaceFormat[] ValidAdapterFormats = new SurfaceFormat[4]
    {
      SurfaceFormat.Bgr32,
      SurfaceFormat.Bgr555,
      SurfaceFormat.Bgr565,
      SurfaceFormat.Bgra1010102
    };
    public static readonly SurfaceFormat[] ValidBackBufferFormats = new SurfaceFormat[6]
    {
      SurfaceFormat.Bgr565,
      SurfaceFormat.Bgr555,
      SurfaceFormat.Bgra5551,
      SurfaceFormat.Bgr32,
      SurfaceFormat.Color,
      SurfaceFormat.Bgra1010102
    };
    private static readonly TimeSpan deviceLostSleepTime = TimeSpan.FromMilliseconds(50.0);
    private static MultiSampleType[] multiSampleTypes = new MultiSampleType[17]
    {
      MultiSampleType.NonMaskable,
      MultiSampleType.SixteenSamples,
      MultiSampleType.FifteenSamples,
      MultiSampleType.FourteenSamples,
      MultiSampleType.ThirteenSamples,
      MultiSampleType.TwelveSamples,
      MultiSampleType.ElevenSamples,
      MultiSampleType.TenSamples,
      MultiSampleType.NineSamples,
      MultiSampleType.EightSamples,
      MultiSampleType.SevenSamples,
      MultiSampleType.SixSamples,
      MultiSampleType.FiveSamples,
      MultiSampleType.FourSamples,
      MultiSampleType.ThreeSamples,
      MultiSampleType.TwoSamples,
      MultiSampleType.None
    };
    private static DepthFormat[] depthFormatsWithStencil = new DepthFormat[4]
    {
      DepthFormat.Depth24Stencil8,
      DepthFormat.Depth24Stencil4,
      DepthFormat.Depth24Stencil8Single,
      DepthFormat.Depth15Stencil1
    };
    private static DepthFormat[] depthFormatsWithoutStencil = new DepthFormat[3]
    {
      DepthFormat.Depth24,
      DepthFormat.Depth32,
      DepthFormat.Depth16
    };
    private bool synchronizeWithVerticalRetrace = true;
    private SurfaceFormat backBufferFormat = SurfaceFormat.Color;
    private DepthFormat depthStencilFormat = DepthFormat.Depth24;
    private int backBufferWidth = GraphicsDeviceManager.DefaultBackBufferWidth;
    private int backBufferHeight = GraphicsDeviceManager.DefaultBackBufferHeight;
    private ShaderProfile minimumVertexShaderProfile = ShaderProfile.VS_1_1;
    private Game game;
    private bool isReallyFullScreen;
    private bool isDeviceDirty;
    private bool inDeviceTransition;
    private GraphicsDevice device;
    private bool isFullScreen;
    private bool allowMultiSampling;
    private ShaderProfile minimumPixelShaderProfile;
    private int resizedBackBufferWidth;
    private int resizedBackBufferHeight;
    private bool useResizedBackBuffer;
    private EventHandler deviceCreated;
    private EventHandler deviceResetting;
    private EventHandler deviceReset;
    private EventHandler deviceDisposing;
    private bool beginDrawOk;

    public DepthFormat PreferredDepthStencilFormat
    {
      get
      {
        return this.depthStencilFormat;
      }
      set
      {
        switch (value)
        {
          case DepthFormat.Depth24Stencil8:
          case DepthFormat.Depth24Stencil8Single:
          case DepthFormat.Depth24Stencil4:
          case DepthFormat.Depth24:
          case DepthFormat.Depth32:
          case DepthFormat.Depth16:
          case DepthFormat.Depth15Stencil1:
            this.depthStencilFormat = value;
            this.isDeviceDirty = true;
            break;
          default:
            throw new ArgumentOutOfRangeException("value", Resources.ValidateDepthStencilFormatIsInvalid);
        }
      }
    }

    public SurfaceFormat PreferredBackBufferFormat
    {
      get
      {
        return this.backBufferFormat;
      }
      set
      {
        if (Array.IndexOf<SurfaceFormat>(GraphicsDeviceManager.ValidBackBufferFormats, value) == -1)
          throw new ArgumentOutOfRangeException("value", Resources.ValidateBackBufferFormatIsInvalid);
        this.backBufferFormat = value;
        this.isDeviceDirty = true;
      }
    }

    public int PreferredBackBufferWidth
    {
      get
      {
        return this.backBufferWidth;
      }
      set
      {
        if (value <= 0)
          throw new ArgumentOutOfRangeException("value", Resources.BackBufferDimMustBePositive);
        this.backBufferWidth = value;
        this.useResizedBackBuffer = false;
        this.isDeviceDirty = true;
      }
    }

    public int PreferredBackBufferHeight
    {
      get
      {
        return this.backBufferHeight;
      }
      set
      {
        if (value <= 0)
          throw new ArgumentOutOfRangeException("value", Resources.BackBufferDimMustBePositive);
        this.backBufferHeight = value;
        this.useResizedBackBuffer = false;
        this.isDeviceDirty = true;
      }
    }

    public bool IsFullScreen
    {
      get
      {
        return this.isFullScreen;
      }
      set
      {
        this.isFullScreen = value;
        this.isDeviceDirty = true;
      }
    }

    public bool SynchronizeWithVerticalRetrace
    {
      get
      {
        return this.synchronizeWithVerticalRetrace;
      }
      set
      {
        this.synchronizeWithVerticalRetrace = value;
        this.isDeviceDirty = true;
      }
    }

    public bool PreferMultiSampling
    {
      get
      {
        return this.allowMultiSampling;
      }
      set
      {
        this.allowMultiSampling = value;
        this.isDeviceDirty = true;
      }
    }

    public ShaderProfile MinimumPixelShaderProfile
    {
      get
      {
        return this.minimumPixelShaderProfile;
      }
      set
      {
        if (value < ShaderProfile.PS_1_1 || value > ShaderProfile.XPS_3_0)
          throw new ArgumentOutOfRangeException("value", Resources.InvalidPixelShaderProfile);
        this.minimumPixelShaderProfile = value;
        this.isDeviceDirty = true;
      }
    }

    public ShaderProfile MinimumVertexShaderProfile
    {
      get
      {
        return this.minimumVertexShaderProfile;
      }
      set
      {
        if (value < ShaderProfile.VS_1_1 || value > ShaderProfile.XVS_3_0)
          throw new ArgumentOutOfRangeException("value", Resources.InvalidVertexShaderProfile);
        this.minimumVertexShaderProfile = value;
        this.isDeviceDirty = true;
      }
    }

    public GraphicsDevice GraphicsDevice
    {
      get
      {
        return this.device;
      }
    }

    public event EventHandler DeviceCreated
    {
      add
      {
        this.deviceCreated += value;
      }
      remove
      {
        this.deviceCreated -= value;
      }
    }

    public event EventHandler DeviceResetting
    {
      add
      {
        this.deviceResetting += value;
      }
      remove
      {
        this.deviceResetting -= value;
      }
    }

    public event EventHandler DeviceReset
    {
      add
      {
        this.deviceReset += value;
      }
      remove
      {
        this.deviceReset -= value;
      }
    }

    public event EventHandler DeviceDisposing
    {
      add
      {
        this.deviceDisposing += value;
      }
      remove
      {
        this.deviceDisposing -= value;
      }
    }

    public event EventHandler<PreparingDeviceSettingsEventArgs> PreparingDeviceSettings;

    public event EventHandler Disposed;

    public GraphicsDeviceManager(Game game)
    {
      this.game = game ?? throw new ArgumentNullException(nameof(game), Resources.GameCannotBeNull);
      if (game.Services.GetService(typeof (IGraphicsDeviceManager)) != null)
        throw new ArgumentException(Resources.GraphicsDeviceManagerAlreadyPresent);
      game.Services.AddService(typeof (IGraphicsDeviceManager), this);
      game.Services.AddService(typeof (IGraphicsDeviceService), this);
      game.Window.ClientSizeChanged += GameWindowClientSizeChanged;
      game.Window.ScreenDeviceNameChanged += GameWindowScreenDeviceNameChanged;
    }

    public void ApplyChanges()
    {
      if (this.device != null && !this.isDeviceDirty)
        return;
      this.ChangeDevice(false);
    }

    public void ToggleFullScreen()
    {
      this.IsFullScreen = !this.IsFullScreen;
      this.ChangeDevice(false);
    }

    private void GameWindowScreenDeviceNameChanged(object sender, EventArgs e)
    {
      if (this.inDeviceTransition)
        return;
      this.ChangeDevice(false);
    }

    private void GameWindowClientSizeChanged(object sender, EventArgs e)
    {
      if (this.inDeviceTransition || this.game.Window.ClientBounds.Height == 0 && this.game.Window.ClientBounds.Width == 0)
        return;
      this.resizedBackBufferWidth = this.game.Window.ClientBounds.Width;
      this.resizedBackBufferHeight = this.game.Window.ClientBounds.Height;
      this.useResizedBackBuffer = true;
      this.ChangeDevice(false);
    }

    private bool EnsureDevice()
    {
      if (this.device == null)
        return false;
      return this.EnsureDevicePlatform();
    }

    private void CreateDevice(GraphicsDeviceInformation newInfo)
    {
      if (this.device != null)
      {
        this.device.Dispose();
        this.device = (GraphicsDevice) null;
      }
      this.OnPreparingDeviceSettings((object) this, new PreparingDeviceSettingsEventArgs(newInfo));
      this.MassagePresentParameters(newInfo.PresentationParameters);
      try
      {
        this.ValidateGraphicsDeviceInformation(newInfo);
        this.device = new GraphicsDevice(newInfo.Adapter, newInfo.DeviceType, this.game.Window.Handle, newInfo.PresentationParameters);
        this.device.DeviceResetting += new EventHandler(this.HandleDeviceResetting);
        this.device.DeviceReset += new EventHandler(this.HandleDeviceReset);
        this.device.DeviceLost += new EventHandler(this.HandleDeviceLost);
        this.device.Disposing += new EventHandler(this.HandleDisposing);
      }
      catch (DeviceNotSupportedException ex)
      {
        throw this.CreateNoSuitableGraphicsDeviceException(Resources.Direct3DNotAvailable, (Exception) ex);
      }
      catch (DriverInternalErrorException ex)
      {
        throw this.CreateNoSuitableGraphicsDeviceException(Resources.Direct3DInternalDriverError, (Exception) ex);
      }
      catch (ArgumentException ex)
      {
        throw this.CreateNoSuitableGraphicsDeviceException(Resources.Direct3DInvalidCreateParameters, (Exception) ex);
      }
      catch (Exception ex)
      {
        throw this.CreateNoSuitableGraphicsDeviceException(Resources.Direct3DCreateError, ex);
      }
      this.OnDeviceCreated((object) this, EventArgs.Empty);
    }

    private Exception CreateNoSuitableGraphicsDeviceException(string message, Exception innerException)
    {
      Exception exception = (Exception) new NoSuitableGraphicsDeviceException(message, innerException);
      exception.Data.Add((object) "MinimumPixelShaderProfile", (object) this.minimumPixelShaderProfile);
      exception.Data.Add((object) "MinimumVertexShaderProfile", (object) this.minimumVertexShaderProfile);
      return exception;
    }

    private void ChangeDevice(bool forceCreate)
    {
      if (this.game == null)
        throw new InvalidOperationException(Resources.GraphicsComponentNotAttachedToGame);
      this.CheckForAvailableSupportedHardware();
      this.inDeviceTransition = true;
      string screenDeviceName = this.game.Window.ScreenDeviceName;
      int clientWidth = this.game.Window.ClientBounds.Width;
      int clientHeight = this.game.Window.ClientBounds.Height;
      bool flag1 = false;
      try
      {
        GraphicsDeviceInformation bestDevice = this.FindBestDevice(forceCreate);
        this.game.Window.BeginScreenDeviceChange(bestDevice.PresentationParameters.IsFullScreen);
        flag1 = true;
        bool flag2 = true;
        if (!forceCreate && this.device != null)
        {
          this.OnPreparingDeviceSettings((object) this, new PreparingDeviceSettingsEventArgs(bestDevice));
          if (this.CanResetDevice(bestDevice))
          {
            try
            {
              GraphicsDeviceInformation deviceInformation = bestDevice.Clone();
              this.MassagePresentParameters(bestDevice.PresentationParameters);
              this.ValidateGraphicsDeviceInformation(bestDevice);
              this.device.Reset(deviceInformation.PresentationParameters, deviceInformation.Adapter);
              flag2 = false;
            }
            catch
            {
            }
          }
        }
        if (flag2)
          this.CreateDevice(bestDevice);
        PresentationParameters presentationParameters = this.device.PresentationParameters;
        screenDeviceName = this.device.CreationParameters.Adapter.DeviceName;
        this.isReallyFullScreen = presentationParameters.IsFullScreen;
        if (presentationParameters.BackBufferWidth != 0)
          clientWidth = presentationParameters.BackBufferWidth;
        if (presentationParameters.BackBufferHeight != 0)
          clientHeight = presentationParameters.BackBufferHeight;
        this.isDeviceDirty = false;
      }
      finally
      {
        if (flag1)
          this.game.Window.EndScreenDeviceChange(screenDeviceName, clientWidth, clientHeight);
        this.inDeviceTransition = false;
      }
    }

    private void MassagePresentParameters(PresentationParameters pp)
    {
      bool flag1 = pp.BackBufferWidth == 0;
      bool flag2 = pp.BackBufferHeight == 0;
      if (pp.IsFullScreen)
        return;
      IntPtr hWnd = pp.DeviceWindowHandle;
      if (hWnd == IntPtr.Zero)
      {
        if (this.game == null)
          throw new InvalidOperationException(Resources.GraphicsComponentNotAttachedToGame);
        hWnd = this.game.Window.Handle;
      }
      NativeMethods.RECT rect;
      NativeMethods.GetClientRect(hWnd, out rect);
      if (flag1 && rect.Right == 0)
        pp.BackBufferWidth = 1;
      if (!flag2 || rect.Bottom != 0)
        return;
      pp.BackBufferHeight = 1;
    }

    protected virtual GraphicsDeviceInformation FindBestDevice(bool anySuitableDevice)
    {
      return this.FindBestPlatformDevice(anySuitableDevice);
    }

    protected virtual bool CanResetDevice(GraphicsDeviceInformation newDeviceInfo)
    {
      return this.device.CreationParameters.DeviceType == newDeviceInfo.DeviceType;
    }

    protected virtual void RankDevices(List<GraphicsDeviceInformation> foundDevices)
    {
      this.RankDevicesPlatform(foundDevices);
    }

    private void HandleDisposing(object sender, EventArgs e)
    {
      this.OnDeviceDisposing((object) this, EventArgs.Empty);
    }

    private void HandleDeviceLost(object sender, EventArgs e)
    {
    }

    private void HandleDeviceReset(object sender, EventArgs e)
    {
      this.OnDeviceReset((object) this, EventArgs.Empty);
    }

    private void HandleDeviceResetting(object sender, EventArgs e)
    {
      this.OnDeviceResetting((object) this, EventArgs.Empty);
    }

    protected virtual void OnDeviceCreated(object sender, EventArgs args)
    {
        deviceCreated?.Invoke(sender, args);
    }

    protected virtual void OnDeviceDisposing(object sender, EventArgs args)
    {
        deviceDisposing?.Invoke(sender, args);
    }

    protected virtual void OnDeviceReset(object sender, EventArgs args)
    {
        deviceReset?.Invoke(sender, args);
    }

    protected virtual void OnDeviceResetting(object sender, EventArgs args)
    {
        deviceResetting?.Invoke(sender, args);
    }

    protected virtual void Dispose(bool disposing)
    {
      if (!disposing)
        return;
      if (this.game != null)
      {
        if (this.game.Services.GetService(typeof (IGraphicsDeviceService)) == this)
          this.game.Services.RemoveService(typeof (IGraphicsDeviceService));
        this.game.Window.ClientSizeChanged -= new EventHandler(this.GameWindowClientSizeChanged);
        this.game.Window.ScreenDeviceNameChanged -= new EventHandler(this.GameWindowScreenDeviceNameChanged);
      }
      if (this.device != null)
      {
        this.device.Dispose();
        this.device = (GraphicsDevice) null;
      }
      if (this.Disposed == null)
        return;
      this.Disposed((object) this, EventArgs.Empty);
    }

    protected virtual void OnPreparingDeviceSettings(object sender, PreparingDeviceSettingsEventArgs args)
    {
      if (this.PreparingDeviceSettings == null)
        return;
      this.PreparingDeviceSettings(sender, args);
    }

    void IDisposable.Dispose()
    {
      this.Dispose(true);
      GC.SuppressFinalize((object) this);
    }

    void IGraphicsDeviceManager.CreateDevice()
    {
      this.ChangeDevice(true);
    }

    bool IGraphicsDeviceManager.BeginDraw()
    {
      if (!this.EnsureDevice())
        return false;
      this.beginDrawOk = true;
      return true;
    }

    void IGraphicsDeviceManager.EndDraw()
    {
      if (!this.beginDrawOk)
        return;
      if (this.device == null)
        return;
      try
      {
        this.device.Present();
      }
      catch (InvalidOperationException)
      {
      }
      catch (DeviceLostException)
      {
      }
      catch (DeviceNotResetException)
      {
      }
      catch (DriverInternalErrorException)
      {
      }
    }

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(uint smIndex);

    private void CheckForAvailableSupportedHardware()
    {
      bool flag1 = false;
      bool flag2 = false;
      foreach (GraphicsAdapter adapter in GraphicsAdapter.Adapters)
      {
        if (adapter.IsDeviceTypeAvailable(DeviceType.Hardware))
        {
          flag1 = true;
          GraphicsDeviceCapabilities capabilities = adapter.GetCapabilities(DeviceType.Hardware);
          if (capabilities.MaxPixelShaderProfile != ShaderProfile.Unknown && capabilities.MaxPixelShaderProfile >= ShaderProfile.PS_1_1 && capabilities.DeviceCapabilities.IsDirect3D9Driver)
          {
            flag2 = true;
            break;
          }
        }
      }
      if (!flag1)
      {
        if (GraphicsDeviceManager.GetSystemMetrics(4096U) != 0)
          throw this.CreateNoSuitableGraphicsDeviceException(Resources.NoDirect3DAccelerationRemoteDesktop, (Exception) null);
        throw this.CreateNoSuitableGraphicsDeviceException(Resources.NoDirect3DAcceleration, (Exception) null);
      }
      if (!flag2)
        throw this.CreateNoSuitableGraphicsDeviceException(Resources.NoPixelShader11OrDDI9Support, (Exception) null);
    }

    private void RankDevicesPlatform(List<GraphicsDeviceInformation> foundDevices)
    {
      int index = 0;
      while (index < foundDevices.Count)
      {
        DeviceType deviceType = foundDevices[index].DeviceType;
        GraphicsAdapter adapter = foundDevices[index].Adapter;
        PresentationParameters presentationParameters = foundDevices[index].PresentationParameters;
        if (!adapter.CheckDeviceFormat(deviceType, adapter.CurrentDisplayMode.Format, TextureUsage.None, QueryUsages.PostPixelShaderBlending, ResourceType.Texture2D, presentationParameters.BackBufferFormat))
          foundDevices.RemoveAt(index);
        else
          ++index;
      }
      foundDevices.Sort((IComparer<GraphicsDeviceInformation>) new GraphicsDeviceInformationComparer(this));
    }

    private GraphicsDeviceInformation FindBestPlatformDevice(bool anySuitableDevice)
    {
      List<GraphicsDeviceInformation> foundDevices = new List<GraphicsDeviceInformation>();
      this.AddDevices(anySuitableDevice, foundDevices);
      if (foundDevices.Count == 0 && this.PreferMultiSampling)
      {
        this.PreferMultiSampling = false;
        this.AddDevices(anySuitableDevice, foundDevices);
      }
      if (foundDevices.Count == 0)
        throw this.CreateNoSuitableGraphicsDeviceException(Resources.NoCompatibleDevices, (Exception) null);
      this.RankDevices(foundDevices);
      if (foundDevices.Count == 0)
        throw this.CreateNoSuitableGraphicsDeviceException(Resources.NoCompatibleDevicesAfterRanking, (Exception) null);
      return foundDevices[0];
    }

    private void AddDevices(bool anySuitableDevice, List<GraphicsDeviceInformation> foundDevices)
    {
      IntPtr handle = game.Window.Handle;
      foreach (GraphicsAdapter adapter in GraphicsAdapter.Adapters)
      {
          if (!anySuitableDevice && !IsWindowOnAdapter(handle, adapter))
              continue;
          foreach (DeviceType validDeviceType in ValidDeviceTypes)
          {
              try
              {
                  if (!adapter.IsDeviceTypeAvailable(validDeviceType))
                      continue;
                  GraphicsDeviceCapabilities capabilities = adapter.GetCapabilities(validDeviceType);
                  if (!capabilities.DeviceCapabilities.IsDirect3D9Driver)
                      continue;
                  if (!IsValidShaderProfile(capabilities.MaxPixelShaderProfile, MinimumPixelShaderProfile))
                      continue;
                  if (!IsValidShaderProfile(capabilities.MaxVertexShaderProfile, MinimumVertexShaderProfile))
                      continue;
                  var baseDeviceInfo = new GraphicsDeviceInformation
                  {
                      Adapter    = adapter,
                      DeviceType = validDeviceType,
                      PresentationParameters =
                      {
                          DeviceWindowHandle        = IntPtr.Zero,
                          EnableAutoDepthStencil    = true,
                          BackBufferCount           = 1,
                          PresentOptions            = PresentOptions.None,
                          SwapEffect                = SwapEffect.Discard,
                          FullScreenRefreshRateInHz = 0,
                          MultiSampleQuality        = 0,
                          MultiSampleType           = MultiSampleType.None,
                          IsFullScreen              = IsFullScreen,
                          PresentationInterval = SynchronizeWithVerticalRetrace
                              ? PresentInterval.One
                              : PresentInterval.Immediate
                      }
                  };
                  for (int index = 0; index < ValidAdapterFormats.Length; ++index)
                  {
                      AddDevices(adapter, validDeviceType, adapter.CurrentDisplayMode, baseDeviceInfo, foundDevices);
                      if (!isFullScreen)
                          continue;
                      foreach (DisplayMode mode in adapter.SupportedDisplayModes[ValidAdapterFormats[index]])
                      {
                          if (mode.Width >= 640 && mode.Height >= 480)
                              AddDevices(adapter, validDeviceType, mode, baseDeviceInfo, foundDevices);
                      }
                  }
              }
              catch (DeviceNotSupportedException)
              {
              }
          }
      }
    }

    private static bool IsValidShaderProfile(ShaderProfile capsShaderProfile, ShaderProfile minimumShaderProfile)
    {
      if (capsShaderProfile == ShaderProfile.PS_2_B && minimumShaderProfile == ShaderProfile.PS_2_A)
        return false;
      return capsShaderProfile >= minimumShaderProfile;
    }

    private void AddDevices(GraphicsAdapter adapter, DeviceType deviceType, DisplayMode mode, GraphicsDeviceInformation baseDeviceInfo, List<GraphicsDeviceInformation> foundDevices)
    {
      for (int index1 = 0; index1 < GraphicsDeviceManager.ValidBackBufferFormats.Length; ++index1)
      {
        SurfaceFormat backBufferFormat = GraphicsDeviceManager.ValidBackBufferFormats[index1];
        if (adapter.CheckDeviceType(deviceType, mode.Format, backBufferFormat, this.IsFullScreen))
        {
          GraphicsDeviceInformation deviceInformation1 = baseDeviceInfo.Clone();
          if (this.IsFullScreen)
          {
            deviceInformation1.PresentationParameters.BackBufferWidth = mode.Width;
            deviceInformation1.PresentationParameters.BackBufferHeight = mode.Height;
            deviceInformation1.PresentationParameters.FullScreenRefreshRateInHz = mode.RefreshRate;
          }
          else if (this.useResizedBackBuffer)
          {
            deviceInformation1.PresentationParameters.BackBufferWidth = this.resizedBackBufferWidth;
            deviceInformation1.PresentationParameters.BackBufferHeight = this.resizedBackBufferHeight;
          }
          else
          {
            deviceInformation1.PresentationParameters.BackBufferWidth = this.PreferredBackBufferWidth;
            deviceInformation1.PresentationParameters.BackBufferHeight = this.PreferredBackBufferHeight;
          }
          deviceInformation1.PresentationParameters.BackBufferFormat = backBufferFormat;
          deviceInformation1.PresentationParameters.AutoDepthStencilFormat = this.ChooseDepthStencilFormat(adapter, deviceType, mode.Format);
          if (this.PreferMultiSampling)
          {
            for (int index2 = 0; index2 < GraphicsDeviceManager.multiSampleTypes.Length; ++index2)
            {
              int qualityLevels = 0;
              MultiSampleType multiSampleType = GraphicsDeviceManager.multiSampleTypes[index2];
              if (adapter.CheckDeviceMultiSampleType(deviceType, backBufferFormat, this.IsFullScreen, multiSampleType, out qualityLevels))
              {
                GraphicsDeviceInformation deviceInformation2 = deviceInformation1.Clone();
                deviceInformation2.PresentationParameters.MultiSampleType = multiSampleType;
                if (!foundDevices.Contains(deviceInformation2))
                {
                  foundDevices.Add(deviceInformation2);
                  break;
                }
                break;
              }
            }
          }
          else if (!foundDevices.Contains(deviceInformation1))
            foundDevices.Add(deviceInformation1);
        }
      }
    }

    private DepthFormat ChooseDepthStencilFormat(GraphicsAdapter adapter, DeviceType deviceType, SurfaceFormat adapterFormat)
    {
      if (adapter.CheckDeviceFormat(deviceType, adapterFormat, TextureUsage.None, QueryUsages.None, ResourceType.DepthStencilBuffer, this.PreferredDepthStencilFormat))
        return this.PreferredDepthStencilFormat;
      if (Array.IndexOf<DepthFormat>(GraphicsDeviceManager.depthFormatsWithStencil, this.PreferredDepthStencilFormat) >= 0)
      {
        DepthFormat depthFormat = this.ChooseDepthStencilFormatFromList(GraphicsDeviceManager.depthFormatsWithStencil, adapter, deviceType, adapterFormat);
        if (depthFormat != DepthFormat.Unknown)
          return depthFormat;
      }
      DepthFormat depthFormat1 = this.ChooseDepthStencilFormatFromList(GraphicsDeviceManager.depthFormatsWithoutStencil, adapter, deviceType, adapterFormat);
      if (depthFormat1 != DepthFormat.Unknown)
        return depthFormat1;
      return DepthFormat.Depth24;
    }

    private DepthFormat ChooseDepthStencilFormatFromList(DepthFormat[] availableFormats, GraphicsAdapter adapter, DeviceType deviceType, SurfaceFormat adapterFormat)
    {
      for (int index = 0; index < availableFormats.Length; ++index)
      {
        if (availableFormats[index] != this.PreferredDepthStencilFormat && adapter.CheckDeviceFormat(deviceType, adapterFormat, TextureUsage.None, QueryUsages.None, ResourceType.DepthStencilBuffer, availableFormats[index]))
          return availableFormats[index];
      }
      return DepthFormat.Unknown;
    }

    private bool IsWindowOnAdapter(IntPtr windowHandle, GraphicsAdapter adapter)
    {
      return WindowsGameWindow.ScreenFromAdapter(adapter) == WindowsGameWindow.ScreenFromHandle(windowHandle);
    }

    private bool EnsureDevicePlatform()
    {
      if (this.isReallyFullScreen && !this.game.IsActiveIgnoringGuide)
        return false;
      switch (this.device.GraphicsDeviceStatus)
      {
        case GraphicsDeviceStatus.Lost:
          Thread.Sleep((int) GraphicsDeviceManager.deviceLostSleepTime.TotalMilliseconds);
          return false;
        case GraphicsDeviceStatus.NotReset:
          Thread.Sleep((int) GraphicsDeviceManager.deviceLostSleepTime.TotalMilliseconds);
          try
          {
            this.ChangeDevice(false);
            break;
          }
          catch (DeviceLostException)
          {
            return false;
          }
          catch
          {
            this.ChangeDevice(true);
            break;
          }
      }
      return true;
    }

    private void ValidateGraphicsDeviceInformation(GraphicsDeviceInformation devInfo)
    {
      GraphicsAdapter adapter = devInfo.Adapter;
      DeviceType deviceType = devInfo.DeviceType;
      bool autoDepthStencil = devInfo.PresentationParameters.EnableAutoDepthStencil;
      DepthFormat depthStencilFormat = devInfo.PresentationParameters.AutoDepthStencilFormat;
      SurfaceFormat backBufferFormat = devInfo.PresentationParameters.BackBufferFormat;
      int backBufferWidth = devInfo.PresentationParameters.BackBufferWidth;
      int backBufferHeight = devInfo.PresentationParameters.BackBufferHeight;
      PresentationParameters presentationParameters = devInfo.PresentationParameters;
      SurfaceFormat surfaceFormat = presentationParameters.BackBufferFormat;
      SurfaceFormat index;
      if (!presentationParameters.IsFullScreen)
      {
        index = adapter.CurrentDisplayMode.Format;
        if (SurfaceFormat.Unknown == presentationParameters.BackBufferFormat)
          surfaceFormat = index;
      }
      else
      {
        switch (presentationParameters.BackBufferFormat)
        {
          case SurfaceFormat.Color:
            index = SurfaceFormat.Bgr32;
            break;
          case SurfaceFormat.Bgra5551:
            index = SurfaceFormat.Bgr555;
            break;
          default:
            index = presentationParameters.BackBufferFormat;
            break;
        }
      }
      if (-1 == Array.IndexOf<SurfaceFormat>(GraphicsDeviceManager.ValidBackBufferFormats, surfaceFormat))
        throw new ArgumentException(Resources.ValidateBackBufferFormatIsInvalid);
      if (!adapter.CheckDeviceType(deviceType, index, presentationParameters.BackBufferFormat, presentationParameters.IsFullScreen))
        throw new ArgumentException(Resources.ValidateDeviceType);
      if (presentationParameters.BackBufferCount < 0 || presentationParameters.BackBufferCount > 3)
        throw new ArgumentException(Resources.ValidateBackBufferCount);
      if (presentationParameters.BackBufferCount > 1 && presentationParameters.SwapEffect == SwapEffect.Copy)
        throw new ArgumentException(Resources.ValidateBackBufferCountSwapCopy);
      switch (presentationParameters.SwapEffect)
      {
        case SwapEffect.Discard:
        case SwapEffect.Flip:
        case SwapEffect.Copy:
          int qualityLevels;
          if (!adapter.CheckDeviceMultiSampleType(deviceType, surfaceFormat, presentationParameters.IsFullScreen, presentationParameters.MultiSampleType, out qualityLevels))
            throw new ArgumentException(Resources.ValidateMultiSampleTypeInvalid);
          if (presentationParameters.MultiSampleQuality >= qualityLevels)
            throw new ArgumentException(Resources.ValidateMultiSampleQualityInvalid);
          if (presentationParameters.MultiSampleType != MultiSampleType.None && presentationParameters.SwapEffect != SwapEffect.Discard)
            throw new ArgumentException(Resources.ValidateMultiSampleSwapEffect);
          if ((presentationParameters.PresentOptions & PresentOptions.DiscardDepthStencil) != PresentOptions.None && !presentationParameters.EnableAutoDepthStencil)
            throw new ArgumentException(Resources.ValidateAutoDepthStencilMismatch);
          if (presentationParameters.EnableAutoDepthStencil)
          {
            if (!adapter.CheckDeviceFormat(deviceType, index, TextureUsage.None, QueryUsages.None, ResourceType.DepthStencilBuffer, presentationParameters.AutoDepthStencilFormat))
              throw new ArgumentException(Resources.ValidateAutoDepthStencilFormatInvalid);
            if (!adapter.CheckDepthStencilMatch(deviceType, index, surfaceFormat, presentationParameters.AutoDepthStencilFormat))
              throw new ArgumentException(Resources.ValidateAutoDepthStencilFormatIncompatible);
          }
          if (!presentationParameters.IsFullScreen)
          {
            if (presentationParameters.FullScreenRefreshRateInHz != 0)
              throw new ArgumentException(Resources.ValidateRefreshRateInWindow);
            switch (presentationParameters.PresentationInterval)
            {
              case PresentInterval.Immediate:
                return;
              case PresentInterval.Default:
                return;
              case PresentInterval.One:
                return;
              default:
                throw new ArgumentException(Resources.ValidatePresentationIntervalInWindow);
            }
          }
          else
          {
            if (presentationParameters.FullScreenRefreshRateInHz == 0)
              throw new ArgumentException(Resources.ValidateRefreshRateInFullScreen);
            GraphicsDeviceCapabilities capabilities = adapter.GetCapabilities(deviceType);
            switch (presentationParameters.PresentationInterval)
            {
              case PresentInterval.Immediate:
              case PresentInterval.Default:
              case PresentInterval.One:
                if (presentationParameters.IsFullScreen)
                {
                  if (presentationParameters.BackBufferWidth == 0 || presentationParameters.BackBufferHeight == 0)
                    throw new ArgumentException(Resources.ValidateBackBufferDimsFullScreen);
                  bool flag1 = true;
                  bool flag2 = false;
                  DisplayMode currentDisplayMode = adapter.CurrentDisplayMode;
                  if (currentDisplayMode.Format != index && currentDisplayMode.Width != presentationParameters.BackBufferHeight && (currentDisplayMode.Height != presentationParameters.BackBufferHeight && currentDisplayMode.RefreshRate != presentationParameters.FullScreenRefreshRateInHz))
                  {
                    flag1 = false;
                    foreach (DisplayMode displayMode in adapter.SupportedDisplayModes[index])
                    {
                      if (displayMode.Width == presentationParameters.BackBufferWidth && displayMode.Height == presentationParameters.BackBufferHeight)
                      {
                        flag2 = true;
                        if (displayMode.RefreshRate == presentationParameters.FullScreenRefreshRateInHz)
                        {
                          flag1 = true;
                          break;
                        }
                      }
                    }
                  }
                  if (!flag1 && flag2)
                    throw new ArgumentException(Resources.ValidateBackBufferDimsModeFullScreen);
                  if (!flag1)
                    throw new ArgumentException(Resources.ValidateBackBufferHzModeFullScreen);
                }
                if (presentationParameters.EnableAutoDepthStencil != autoDepthStencil)
                  throw new ArgumentException(Resources.ValidateAutoDepthStencilAdapterGroup);
                if (!presentationParameters.EnableAutoDepthStencil)
                  return;
                if (presentationParameters.AutoDepthStencilFormat != depthStencilFormat)
                  throw new ArgumentException(Resources.ValidateAutoDepthStencilAdapterGroup);
                if (presentationParameters.BackBufferFormat != backBufferFormat)
                  throw new ArgumentException(Resources.ValidateAutoDepthStencilAdapterGroup);
                if (presentationParameters.BackBufferWidth != backBufferWidth)
                  throw new ArgumentException(Resources.ValidateAutoDepthStencilAdapterGroup);
                if (presentationParameters.BackBufferHeight == backBufferHeight)
                  return;
                throw new ArgumentException(Resources.ValidateAutoDepthStencilAdapterGroup);
              case PresentInterval.Two:
              case PresentInterval.Three:
              case PresentInterval.Four:
                if ((capabilities.PresentInterval & presentationParameters.PresentationInterval) == PresentInterval.Default)
                  throw new ArgumentException(Resources.ValidatePresentationIntervalIncompatibleInFullScreen);
                goto case PresentInterval.Immediate;
              default:
                throw new ArgumentException(Resources.ValidatePresentationIntervalInFullScreen);
            }
          }
        default:
          throw new ArgumentException(Resources.ValidateSwapEffectInvalid);
      }
    }
  }
}
