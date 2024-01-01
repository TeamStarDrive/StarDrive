using System;
using NAudio.Wave;
using SDUtils;

namespace Ship_Game.Audio.NAudio;

public class CachedSoundEffect
{
    public readonly string Name;
    public readonly WaveFormat WaveFormat;

    public int NumSamples { get; private set; }
    
    // The samples are stored in time based chunks
    const float ChunkLengthInSeconds = 0.5f;
    readonly int ChunkSize;
    readonly Array<float[]> Chunks = new();

    public override string ToString() => $"Cached {NumSamples*4/1024}KB {Name}";

    public CachedSoundEffect(WaveFormat outFormat, string audioFile)
    {
        Name = FileSystemExtensions.GetAppRootRelPath(audioFile);

        using NAudioFileReader reader = new(outFormat, audioFile);
        WaveFormat = reader.WaveFormat;
        ChunkSize = (int)(WaveFormat.SampleRate * WaveFormat.Channels * ChunkLengthInSeconds);
        FillChunks(reader);
    }

    void FillChunks(NAudioFileReader reader)
    {
        float[] chunk = new float[ChunkSize];
        int chunkPos = 0;
        int samplesRead;
        while ((samplesRead = reader.Read(chunk, chunkPos, ChunkSize - chunkPos)) > 0)
        {
            NumSamples += samplesRead;
            chunkPos += samplesRead;
            if (chunkPos == ChunkSize) // chunk is full, add it to the list
            {
                Chunks.Add(chunk);
                chunk = new float[ChunkSize];
                chunkPos = 0;
            }
        }

        // add the last chunk and trim it to reduce memory waste
        if (chunkPos > 0)
        {
            if (chunkPos < ChunkSize)
                Array.Resize(ref chunk, chunkPos);
            Chunks.Add(chunk);
        }
    }

    int ReadSamples(int position, float[] buffer, int offset, int maxCount)
    {
        int availableSamples = NumSamples - position;
        if (availableSamples <= 0)
            return 0;

        int samplesToRead = Math.Min(availableSamples, maxCount);
        int chunkIndex = position / ChunkSize;
        int chunkOffset = position % ChunkSize;

        int chunkRemaining = ChunkSize - chunkOffset;
        // more than enough in the current chunk
        if (chunkRemaining >= samplesToRead)
        {
            Array.Copy(Chunks[chunkIndex], chunkOffset, buffer, offset, samplesToRead);
            return samplesToRead;
        }

        // NOTE: in the real use case tests, we pretty much never hit this code path

        // partial read to finish off this chunk
        Array.Copy(Chunks[chunkIndex], chunkOffset, buffer, offset, chunkRemaining);
        int numSamples = chunkRemaining;
        samplesToRead -= chunkRemaining;

        // next chunk has more than enough to finish in one read (fast path)
        if (ChunkSize >= samplesToRead)
        {
            ++chunkIndex;
            Array.Copy(Chunks[chunkIndex], 0, buffer, offset + numSamples, samplesToRead);
            numSamples += samplesToRead;
            return numSamples;
        }
        
        // we need to loop through multiple chunks, this might take several iterations
        while (samplesToRead > 0)
        {
            ++chunkIndex;
            int toRead = Math.Min(ChunkSize, samplesToRead);
            Array.Copy(Chunks[chunkIndex], 0, buffer, offset + numSamples, toRead);
            numSamples += toRead;
            samplesToRead -= toRead;

            if (toRead < ChunkSize)
                break; // if we read less than the chunk size, we're done
        }
        return numSamples;
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
            int read = Sound.ReadSamples(Position, buffer, offset, count);
            Position += read;
            return read;
        }
    }
}
