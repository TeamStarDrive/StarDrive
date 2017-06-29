// Decompiled with JetBrains decompiler
// Type: ns3.Class26
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework;
using SynapseGaming.LightingSystem.Lights;

namespace ns3
{
  internal class Class26
  {
    private float float_4 = 3f;
    private float float_5 = 1f;
    private float float_6 = 0.25f;
    private float float_7 = 1f;
      private const float float_0 = 0.28209f;
    private const float float_1 = 0.4886f;
    private const float float_2 = 0.31539f;
    private const float float_3 = 1.092548f;

    public Vector4[] Coefficients { get; } = new Vector4[7];

      public Class26()
    {
    }

    public Class26(float constantamount, float linearamount, float quadraticamount)
    {
      this.float_4 = constantamount;
      this.float_5 = linearamount;
      this.float_6 = quadraticamount;
    }

    public void method_0()
    {
      this.float_7 = 1f;
      for (int index = 0; index < this.Coefficients.Length; ++index)
        this.Coefficients[index] = Vector4.Zero;
    }

    public void method_1()
    {
      if (this.float_7 == 1.0)
        return;
      float num = 1f / this.float_7;
      for (int index = 0; index < this.Coefficients.Length; ++index)
        this.Coefficients[index] *= num;
      this.float_7 = 1f;
    }

    public void method_2(IDirectionalSource idirectionalSource_0, float float_8)
    {
      if (!(idirectionalSource_0 is ILight))
        return;
      this.method_3((ILight) idirectionalSource_0, idirectionalSource_0.Direction, float_8);
    }

    public void method_3(ILight ilight_0, Vector3 vector3_0, float float_8)
    {
      this.method_5(ilight_0.CompositeColorAndIntensity, vector3_0, float_8);
    }

    public void method_4(ILight ilight_0, Vector3 vector3_0, float float_8, float float_9)
    {
      Vector3 vector3_2;
      Vector3 vector3_3;
      CoreUtils.smethod_1(ilight_0.CompositeColorAndIntensity, float_9, 0.75f, out vector3_2, out vector3_3);
      this.method_5(vector3_2, vector3_0, float_8);
      this.method_5(vector3_3, -vector3_0, float_8);
    }

    public void method_5(Vector3 vector3_0, Vector3 vector3_1, float float_8)
    {
      float w1 = 0.28209f * this.float_4;
      float num1 = 0.4886f * this.float_5;
      float num2 = 0.31539f * this.float_6;
      float num3 = 1.092548f * this.float_6;
      vector3_1 = -vector3_1;
      vector3_0 *= float_8;
      this.float_7 += float_8;
      float num4 = vector3_1.X * vector3_1.X;
      float num5 = vector3_1.Y * vector3_1.Y;
      float num6 = vector3_1.Z * vector3_1.Z;
      float y1 = vector3_1.Y * num1;
      float z1 = vector3_1.Z * num1;
      float x1 = vector3_1.X * num1;
      float x2 = vector3_1.Y * vector3_1.X * num3;
      float y2 = vector3_1.Y * vector3_1.Z * num3;
      float w2 = (2f * num6 - num4 - num5) * num2;
      float z2 = vector3_1.X * vector3_1.Z * num3;
      float num7 = (num4 - num5) * num3;
      Vector4 vector4_1 = new Vector4(x1, y1, z1, w1);
      Vector4 vector4_2 = new Vector4(x2, y2, z2, w2);
      this.Coefficients[0] += vector4_1 * vector3_0.X;
      this.Coefficients[1] += vector4_1 * vector3_0.Y;
      this.Coefficients[2] += vector4_1 * vector3_0.Z;
      this.Coefficients[3] += vector4_2 * vector3_0.X;
      this.Coefficients[4] += vector4_2 * vector3_0.Y;
      this.Coefficients[5] += vector4_2 * vector3_0.Z;
      this.Coefficients[6] += new Vector4(vector3_0, 1f) * num7;
    }
  }
}
