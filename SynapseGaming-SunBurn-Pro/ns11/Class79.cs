// Decompiled with JetBrains decompiler
// Type: ns11.Class79
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Runtime.InteropServices;

namespace ns11
{
  internal class Class79
  {
    public const int int_0 = -4;
    public const int int_1 = -6;
    public const int int_2 = -8;
    public const int int_3 = -16;
    public const int int_4 = -20;
    public const int int_5 = -21;
    public const int int_6 = -12;
    public const int int_7 = 0;
    public const int int_8 = 1073741824;
    public const int int_9 = 536870912;
    public const int int_10 = 268435456;
    public const int int_11 = 134217728;
    public const int int_12 = 67108864;
    public const int int_13 = 33554432;
    public const int int_14 = 16777216;
    public const int int_15 = 12582912;
    public const int int_16 = 8388608;
    public const int int_17 = 4194304;
    public const int int_18 = 2097152;
    public const int int_19 = 1048576;
    public const int int_20 = 524288;
    public const int int_21 = 262144;
    public const int int_22 = 131072;
    public const int int_23 = 65536;
    public const int int_24 = 131072;
    public const int int_25 = 65536;
    public const int int_26 = 0;
    public const int int_27 = 536870912;
    public const int int_28 = 262144;
    public const int int_29 = 13565952;
    public const int int_30 = 13565952;
    public const int int_31 = 1073741824;
    public const int int_32 = 1;
    public const int int_33 = 4;
    public const int int_34 = 8;
    public const int int_35 = 16;
    public const int int_36 = 32;
    public const int int_37 = 64;
    public const int int_38 = 128;
    public const int int_39 = 256;
    public const int int_40 = 512;
    public const int int_41 = 1024;
    public const int int_42 = 4096;
    public const int int_43 = 0;
    public const int int_44 = 8192;
    public const int int_45 = 0;
    public const int int_46 = 16384;
    public const int int_47 = 0;
    public const int int_48 = 65536;
    public const int int_49 = 131072;
    public const int int_50 = 262144;
    public const int int_51 = 768;
    public const int int_52 = 392;
    public const int int_53 = 524288;
    public const int int_54 = 1;
    public const int int_55 = 2;
    public const int int_56 = 4;
    public const int int_57 = 8;
    public const int int_58 = 16;
    public const int int_59 = 32;
    public const int int_60 = 64;
    public const int int_61 = 128;
    public const int int_62 = 256;
    public const int int_63 = 512;
    public const int int_64 = 1024;
    public const int int_65 = 32;
    public const int int_66 = 512;
    public const int int_67 = 8192;
    public const int int_68 = 16384;
    public const int int_69 = 0;
    public const int int_70 = 1;
    public const int int_71 = 1;
    public const int int_72 = 2;
    public const int int_73 = 3;
    public const int int_74 = 3;
    public const int int_75 = 4;
    public const int int_76 = 5;
    public const int int_77 = 6;
    public const int int_78 = 7;
    public const int int_79 = 8;
    public const int int_80 = 9;
    public const int int_81 = 10;
    public const int int_82 = 11;
    public const int int_83 = 11;

    [DllImport("user32.dll")]
    public static extern IntPtr GetWindowRect(IntPtr intptr_0, ref Class79.Struct4 struct4_0);

    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(IntPtr intptr_0, IntPtr intptr_1, int int_84, int int_85, int int_86, int int_87, uint uint_0);

    [DllImport("user32.dll")]
    public static extern IntPtr SetParent(IntPtr intptr_0, IntPtr intptr_1);

    [DllImport("user32.dll")]
    public static extern IntPtr SetWindowLong(IntPtr intptr_0, int int_84, IntPtr intptr_1);

    [DllImport("user32.dll")]
    public static extern IntPtr GetWindowLong(IntPtr intptr_0, int int_84);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern IntPtr ShowWindow(IntPtr intptr_0, int int_84);

    public struct Struct4
    {
      public int left;
      public int top;
      public int right;
      public int bottom;
    }
  }
}
