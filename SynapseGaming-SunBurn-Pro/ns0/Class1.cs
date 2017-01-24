// Decompiled with JetBrains decompiler
// Type: ns0.Class1
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System.Management;

namespace ns0
{
  internal class Class1
  {
    private static bool smethod_0(string string_0)
    {
      return string_0 != null && string_0 != "" && string_0 != "0";
    }

    private static string smethod_1(ManagementObject managementObject_0, string string_0)
    {
        return managementObject_0[string_0]?.ToString().Trim() ?? "";
    }

    public static uint smethod_2()
    {
      try
      {
        string string_0_1 = "";
        string string_0_2 = "";
        ManagementClass managementClass1 = new ManagementClass("Win32_Processor");
        ManagementObjectCollection instances1 = managementClass1.GetInstances();
        foreach (ManagementObject managementObject_0 in instances1)
        {
          string_0_1 += Class1.smethod_1(managementObject_0, "Caption");
          string_0_1 += Class1.smethod_1(managementObject_0, "Family");
          string_0_1 += Class1.smethod_1(managementObject_0, "L2CacheSize");
          string_0_1 += Class1.smethod_1(managementObject_0, "Level");
          string_0_1 += Class1.smethod_1(managementObject_0, "Manufacturer");
          string_0_1 += Class1.smethod_1(managementObject_0, "Name");
          string_0_1 += Class1.smethod_1(managementObject_0, "ProcessorId");
          string_0_1 += Class1.smethod_1(managementObject_0, "Revision");
          string_0_1 += Class1.smethod_1(managementObject_0, "Stepping");
          string_0_1 += Class1.smethod_1(managementObject_0, "UniqueId");
          string_0_1 += Class1.smethod_1(managementObject_0, "Version");
          string_0_2 += Class1.smethod_1(managementObject_0, "UniqueId");
        }
        instances1.Dispose();
        managementClass1.Dispose();
        ManagementClass managementClass2 = new ManagementClass("Win32_BIOS");
        ManagementObjectCollection instances2 = managementClass2.GetInstances();
        foreach (ManagementObject managementObject_0 in instances2)
        {
          string_0_1 += Class1.smethod_1(managementObject_0, "IdentificationCode");
          string_0_1 += Class1.smethod_1(managementObject_0, "Manufacturer");
          string_0_1 += Class1.smethod_1(managementObject_0, "SerialNumber");
          string_0_2 += Class1.smethod_1(managementObject_0, "SerialNumber");
        }
        instances2.Dispose();
        managementClass2.Dispose();
        ManagementClass managementClass3 = new ManagementClass("Win32_BaseBoard");
        ManagementObjectCollection instances3 = managementClass3.GetInstances();
        foreach (ManagementObject managementObject_0 in instances3)
        {
          string_0_1 += Class1.smethod_1(managementObject_0, "Manufacturer");
          string_0_1 += Class1.smethod_1(managementObject_0, "Model");
          string_0_1 += Class1.smethod_1(managementObject_0, "PartNumber");
          string_0_1 += Class1.smethod_1(managementObject_0, "Product");
          string_0_1 += Class1.smethod_1(managementObject_0, "SerialNumber");
          string_0_1 += Class1.smethod_1(managementObject_0, "SKU");
          string_0_1 += Class1.smethod_1(managementObject_0, "Version");
          string_0_2 += Class1.smethod_1(managementObject_0, "SerialNumber");
        }
        instances3.Dispose();
        managementClass3.Dispose();
        if (string_0_2.Length < 40 || !Class1.smethod_0(string_0_2) || !Class1.smethod_0(string_0_1))
        {
          bool flag = false;
          for (int index = 0; index < 2; ++index)
          {
            ManagementClass managementClass4 = new ManagementClass("Win32_DiskDrive");
            ManagementObjectCollection instances4 = managementClass4.GetInstances();
            foreach (ManagementObject managementObject_0 in instances4)
            {
              if (index != 0 || !(Class1.smethod_1(managementObject_0, "DeviceID") != "\\\\.\\PHYSICALDRIVE0"))
              {
                flag = true;
                string_0_1 += Class1.smethod_1(managementObject_0, "Caption");
                string_0_1 += Class1.smethod_1(managementObject_0, "InterfaceType");
                string_0_1 += Class1.smethod_1(managementObject_0, "MediaType");
                string_0_1 += Class1.smethod_1(managementObject_0, "Model");
                string_0_1 += Class1.smethod_1(managementObject_0, "Signature");
                string_0_1 += Class1.smethod_1(managementObject_0, "Size");
                string_0_1 += Class1.smethod_1(managementObject_0, "TotalCylinders");
                string_0_1 += Class1.smethod_1(managementObject_0, "TotalHeads");
                string_0_1 += Class1.smethod_1(managementObject_0, "TotalSectors");
                string_0_1 += Class1.smethod_1(managementObject_0, "TotalTracks");
                string_0_1 += Class1.smethod_1(managementObject_0, "TracksPerCylinder");
                break;
              }
            }
            instances4.Dispose();
            managementClass4.Dispose();
            if (flag)
              break;
          }
        }
        if (Class1.smethod_0(string_0_1))
          return (uint) Class4.smethod_0(string_0_1);
        return 0;
      }
      catch
      {
      }
      return 0;
    }
  }
}
