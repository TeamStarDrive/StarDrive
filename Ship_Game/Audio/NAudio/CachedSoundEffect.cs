using System;
using NAudio.Wave;
using SDUtils;

namespace Ship_Game.Audio.NAudio;

internal class CachedSoundEffect
{
    public float[] AudioData { get; }
    public WaveFormat WaveFormat { get; }
    public readonly string Name;
    public override string ToString() => $"Cached {Name}";

    public CachedSoundEffect(NAudioPlaybackEngine engine, string audioFile)
    {
        Name = audioFile;
        using NAudioFileReader reader = new(engine, audioFile);
        WaveFormat = reader.WaveFormat;

        // TODO: maybe there's a more efficient way than using a resizable array?
        Array<float> data = new(reader.Length / 4);

        float[] buffer = new float[WaveFormat.SampleRate * WaveFormat.Channels];
        int samplesRead;
        while ((samplesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
        {
            data.AddSpan(buffer.AsSpan(0, samplesRead));
        }

        AudioData = data.ToArr();
    }

    public ISampleProvider CreateReader() => new CachedSoundSampleProvider(this);

    class CachedSoundSampleProvider : ISampleProvider
    {
        readonly CachedSoundEffect Sound;
        long Position;
        public WaveFormat WaveFormat => Sound.WaveFormat;
        public override string ToString() => $"CachedSampler {Sound.Name}";

        public CachedSoundSampleProvider(CachedSoundEffect cachedSound)
        {
            Sound = cachedSound;
        }
        public int Read(float[] buffer, int offset, int count)
        {
            long availableSamples = Sound.AudioData.Length - Position;
            long samplesToCopy = Math.Min(availableSamples, count);
            Array.Copy(Sound.AudioData, Position, buffer, offset, samplesToCopy);
            Position += samplesToCopy;
            return (int)samplesToCopy;
        }
    }
}
