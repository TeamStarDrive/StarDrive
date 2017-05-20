// Decompiled with JetBrains decompiler
// Type: Class55
// Assembly: SynapseGaming-SunBurn-Pro, Version=1.3.2.8, Culture=neutral, PublicKeyToken=c23c60523565dbfd
// MVID: A5F03349-72AC-4BAA-AEEE-9AB9B77E0A39
// Assembly location: C:\Projects\BlackBox\StarDrive\SynapseGaming-SunBurn-Pro.dll

using Microsoft.Xna.Framework.Content;

internal class BlockUtil
{
  internal static void SkipBlock(ContentReader reader)
  {
    try
    {
      int count = reader.ReadInt32();
      reader.ReadBytes(count);
    }
    catch
    {
    }
  }
}
