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
      /// <summary>Manufacturer of the active video card.</summary>
    public VideoManufacturer Manufacturer { get; } = VideoManufacturer.Unknown;

      /// <summary>Model number of the active video card.</summary>
    public int ModelNumber { get; }

      /// <summary>
    /// Total memory available to the active video card. This is not always accurate,
    /// video hardware that supports shared memory will report *all* available memory
    /// not just the faster on board memory.
    /// </summary>
    public int TotalMemory { get; }

      /// <summary>Description of the active video card.</summary>
    public string Description { get; } = "";

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
          this.TotalMemory = (int) (uint) obj1;
        object obj2 = this.method_0(current, "Description");
        if (obj2 is string)
        {
          this.Description = (string) obj2;
        }
        else
        {
          object obj3 = this.method_0(current, "Name");
          if (obj3 is string)
            this.Description = (string) obj3;
        }
        string lowerInvariant = this.Description.ToLowerInvariant();
        this.Manufacturer = !lowerInvariant.Contains("nvidia") ? (!lowerInvariant.Contains("ati") ? VideoManufacturer.Unknown : VideoManufacturer.Ati) : VideoManufacturer.Nvidia;
        int startIndex = 0;
        bool flag1 = false;
        for (int index = 0; index < this.Description.Length; ++index)
        {
          if (!flag1 && char.IsDigit(this.Description[index]))
          {
            startIndex = index;
            flag1 = true;
          }
          bool flag2 = index == this.Description.Length - 1;
          if (flag1)
          {
            if (!char.IsDigit(this.Description[index]))
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
            this.ModelNumber = int.Parse(this.Description.Substring(startIndex, length));
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
      return null;
    }

    /// <summary>Common video card manufacturers.</summary>
    public enum VideoManufacturer
    {
      Nvidia,
      Ati,
      Unknown
    }
  }
}
