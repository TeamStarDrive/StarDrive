using System;
using NAudio.Wave;
using SDUtils;

namespace Ship_Game.Audio.NAudio;

public class CachedSoundEffect
{
    public float[] AudioData { get; }
    public WaveFormat WaveFormat { get; }
    public readonly string Name;
    public override string ToString() => $"Cached {Name}";

    // TODO: optimization needed here
    public CachedSoundEffect(WaveFormat outFormat, string audioFile)
    {
        Name = FileSystemExtensions.GetAppRootRelPath(audioFile);

        using NAudioFileReader reader = new(outFormat, audioFile);
        WaveFormat = reader.WaveFormat;

        // TODO: maybe there's a more efficient way than using a resizable array?
        Array<float> data = new((int)reader.Length / 4);

        float[] buffer = new float[WaveFormat.SampleRate * WaveFormat.Channels];
        int samplesRead;
        while ((samplesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
        {
            data.AddRange(buffer, samplesRead);
        }

        AudioData = data.ToArr();
    }

    public ISampleProvider CreateReader() => new CachedSoundSampleProvider(this);

    class CachedSoundSampleProvider : ISampleProvider
    {
        readonly CachedSoundEffect Sound;
        int Position;
        public WaveFormat WaveFormat => Sound.WaveFormat;
        public override string ToString() => $"CachedSampler {Sound.Name}";

        public CachedSoundSampleProvider(CachedSoundEffect cachedSound)
        {
            Sound = cachedSound;
        }
        public int Read(float[] buffer, int offset, int count)
        {
            int availableSamples = Sound.AudioData.Length - Position;
            if (availableSamples <= 0)
                return 0;

            int samplesToCopy = Math.Min(availableSamples, count);
            Array.Copy(Sound.AudioData, Position, buffer, offset, samplesToCopy);
            Position += samplesToCopy;
            return samplesToCopy;
        }
    }
}
