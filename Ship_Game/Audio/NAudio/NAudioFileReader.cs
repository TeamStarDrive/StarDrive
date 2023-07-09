using System;
using NAudio.Wave;

namespace Ship_Game.Audio.NAudio;

internal class NAudioFileReader : ISampleProvider, IDisposable
{
    readonly AudioFileReader Reader;
    readonly ISampleProvider Provider;
    public WaveFormat WaveFormat { get; }

    public NAudioFileReader(NAudioPlaybackEngine engine, string audioFile)
    {
        Reader = new(audioFile);
        Provider = engine.GetCompatibleSampleProvider(Reader);
        WaveFormat = Provider.WaveFormat;
    }

    public int Length => (int)Reader.Length;

    public void Dispose()
    {
        Reader.Dispose();
        if (Provider != Reader && Provider is IDisposable provider)
            provider.Dispose();
    }

    public int Read(float[] buffer, int offset, int count)
    {
        return Provider.Read(buffer, offset, count);
    }
}
