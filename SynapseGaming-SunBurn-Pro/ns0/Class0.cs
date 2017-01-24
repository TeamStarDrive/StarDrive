// Decompiled with JetBrains decompiler
// Type: ns0.Class0
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using System;
using System.Diagnostics;

namespace ns0
{
    internal class Class0
    {

        //private static uint uint_1 = 1000000;

        //private static bool IsActivated = true;

        internal static string string_0 = "";

        private static uint uint_0;

        private static Class4 class4_0;


        private static void smethod_0()
        {
            if (Class0.class4_0 != null)
                return;
            Class0.uint_0 = Class1.smethod_2();
            Class0.class4_0 = new Class4();
        }


        internal static void CheckProductActivation1()
        {
            Class0.smethod_0();
            //if (Class0.uint_1 > 10000U)
            //{
            //  if (Debugger.IsAttached)
            //  {
            //      Class0.IsActivated = false;
            //      Class0.IsActivated = Class0.class4_0.method_1(Class2.class3_0[1].FileName, Class2.class3_0[1].Name, Class0.uint_0);
            //  }
            //  Class0.uint_1 = 0U;
            //}
            //++Class0.uint_1;
            //if (Debugger.IsAttached && !Class0.IsActivated)
            //  throw new Exception("Product not activated, please run activation tool.");
        }


        internal static void CheckProductActivation2()
        {
            Class0.smethod_0();
            //if (Class0.uint_1 > 1000U)
            //{
            //  Class0.IsActivated = false;
            //  Class0.uint_1 = 0U;
            //  Class0.IsActivated = Class0.class4_0.method_1(Class2.class3_0[1].FileName, Class2.class3_0[1].Name, Class0.uint_0);
            //}
            //++Class0.uint_1;
            //if (!Class0.IsActivated)
            //  throw new Exception("Product not activated, please run activation tool.");
        }


        internal static string smethod_3()
        {
            return Class2.GetActivationPath() + Class2.class3_0[1].FileName;
        }
    }
}
