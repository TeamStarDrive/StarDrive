// Decompiled with JetBrains decompiler
// Type: SynapseGaming.LightingSystem.Core.VideoHardwareHelper
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System.Management;

namespace SynapseGaming.LightingSystem.Core
{
  /// <summary>
  /// Helper class used to determine video card manufacturer, model, and memory size.
  /// </summary>
  public class VideoHardwareHelper
  {
    private VideoHardwareHelper.VideoManufacturer videoManufacturer_0 = VideoHardwareHelper.VideoManufacturer.Unknown;
    private string string_0 = "";
    private int int_0;
    private int int_1;

    /// <summary>Manufacturer of the active video card.</summary>
    public VideoHardwareHelper.VideoManufacturer Manufacturer
    {
      get
      {
        return this.videoManufacturer_0;
      }
    }

    /// <summary>Model number of the active video card.</summary>
    public int ModelNumber
    {
      get
      {
        return this.int_0;
      }
    }

    /// <summary>
    /// Total memory available to the active video card. This is not always accurate,
    /// video hardware that supports shared memory will report *all* available memory
    /// not just the faster on board memory.
    /// </summary>
    public int TotalMemory
    {
      get
      {
        return this.int_1;
      }
    }

    /// <summary>Description of the active video card.</summary>
    public string Description
    {
      get
      {
        return this.string_0;
      }
    }

    /// <summary>Creates a new VideoHardwareHelper instance.</summary>
    public VideoHardwareHelper()
    {
      ManagementClass managementClass = new ManagementClass("Win32_VideoController");
      ManagementObjectCollection instances = managementClass.GetInstances();
      ManagementObjectCollection.ManagementObjectEnumerator enumerator = instances.GetEnumerator();
      if (enumerator.MoveNext())
      {
        ManagementObject current = (ManagementObject) enumerator.Current;
        object obj1 = this.method_0(current, "AdapterRAM");
        if (obj1 is uint)
          this.int_1 = (int) (uint) obj1;
        object obj2 = this.method_0(current, "Description");
        if (obj2 is string)
        {
          this.string_0 = (string) obj2;
        }
        else
        {
          object obj3 = this.method_0(current, "Name");
          if (obj3 is string)
            this.string_0 = (string) obj3;
        }
        string lowerInvariant = this.string_0.ToLowerInvariant();
        this.videoManufacturer_0 = !lowerInvariant.Contains("nvidia") ? (!lowerInvariant.Contains("ati") ? VideoHardwareHelper.VideoManufacturer.Unknown : VideoHardwareHelper.VideoManufacturer.Ati) : VideoHardwareHelper.VideoManufacturer.Nvidia;
        int startIndex = 0;
        bool flag1 = false;
        for (int index = 0; index < this.string_0.Length; ++index)
        {
          if (!flag1 && char.IsDigit(this.string_0[index]))
          {
            startIndex = index;
            flag1 = true;
          }
          bool flag2 = index == this.string_0.Length - 1;
          if (flag1)
          {
            if (!char.IsDigit(this.string_0[index]))
              goto label_15;
          }
          if (!flag2)
            continue;
label_15:
          try
          {
            int length = index - startIndex;
            if (flag2)
              ++length;
            this.int_0 = int.Parse(this.string_0.Substring(startIndex, length));
            break;
          }
          catch
          {
            break;
          }
        }
      }
      instances.Dispose();
      managementClass.Dispose();
    }

    private object method_0(ManagementObject managementObject_0, string string_1)
    {
      try
      {
        return managementObject_0[string_1];
      }
      catch
      {
      }
      return (object) null;
    }

    /// <summary>Common video card manufacturers.</summary>
    public enum VideoManufacturer
    {
      Nvidia,
      Ati,
      Unknown,
    }
  }
}
