using System;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Ship_Game.Audio.NAudio;

internal class NAudioFileReader : WaveStream, ISampleProvider, IDisposable
{
    public readonly string Name;

    WaveStream Reader; // the waveStream which we will use for all positioning
    readonly ISampleProvider Provider;
    readonly int DstBytesPerSample;
    readonly int SrcBytesPerSample;
    readonly object Sync = new();
    
    public NAudioFileReader(NAudioPlaybackEngine engine, string audioFile)
    {
        Name = audioFile;

        Reader = CreateReaderStream(audioFile);
        Provider = engine.GetCompatibleSampleProvider(ConvertWaveProviderIntoSampleProvider(Reader));

        SrcBytesPerSample = (Reader.WaveFormat.BitsPerSample / 8) * Reader.WaveFormat.Channels;
        DstBytesPerSample = 4*Provider.WaveFormat.Channels;

        WaveFormat = Provider.WaveFormat;
        Length = SourceToDest(Reader.Length);
    }

    public override string ToString() => $"NAudioFileReader {Name}";
    
    /// <summary>
    /// WaveFormat of this stream
    /// </summary>
    public override WaveFormat WaveFormat { get; }

    /// <summary>
    /// Length of this stream (in bytes)
    /// </summary>
    public override long Length { get; }

    /// <summary>
    /// Position of this stream (in bytes)
    /// </summary>
    public override long Position
    {
        get => SourceToDest(Reader.Position);
        set { lock (Sync) { Reader.Position = DestToSource(value); }  }
    }

    /// <summary>
    /// Creates the reader stream, supporting all file types in the core NAudio library,
    /// and ensuring we are in PCM format
    /// </summary>
    static WaveStream CreateReaderStream(string fileName)
    {
        if (fileName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
        {
            WaveStream stream = new WaveFileReader(fileName);
            if (stream.WaveFormat.Encoding != WaveFormatEncoding.Pcm && stream.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
            {
                stream = WaveFormatConversionStream.CreatePcmStream(stream);
                stream = new BlockAlignReductionStream(stream);
            }
            return stream;
        }
        else if (fileName.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
        {
            if (Environment.OSVersion.Version.Major < 6)
                return new Mp3FileReader(fileName);
            else // make MediaFoundationReader the default for MP3 going forwards
                return new MediaFoundationReader(fileName);
        }
        else if (fileName.EndsWith(".aiff", StringComparison.OrdinalIgnoreCase) || 
                 fileName.EndsWith(".aif", StringComparison.OrdinalIgnoreCase))
        {
            return new AiffFileReader(fileName);
        }
        else
        {
            // fall back to media foundation reader, see if that can play it
            return new MediaFoundationReader(fileName);
        }
    }


    static ISampleProvider ConvertWaveProviderIntoSampleProvider(IWaveProvider provider)
    {
        if (provider.WaveFormat.Encoding == WaveFormatEncoding.Pcm)
        {
            if (provider.WaveFormat.BitsPerSample == 8) return new Pcm8BitToSampleProvider(provider);
            if (provider.WaveFormat.BitsPerSample == 16) return new Pcm16BitToSampleProvider(provider);
            if (provider.WaveFormat.BitsPerSample == 24) return new Pcm24BitToSampleProvider(provider);
            if (provider.WaveFormat.BitsPerSample == 32) return new Pcm32BitToSampleProvider(provider);
            throw new InvalidOperationException("Unsupported bit depth");
        }
        if (provider.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
            throw new ArgumentException("Unsupported source encoding");

        if (provider.WaveFormat.BitsPerSample == 64) return new WaveToSampleProvider64(provider);
        return new WaveToSampleProvider(provider);
    }

    /// <summary>
    /// Reads from this wave stream
    /// </summary>
    /// <param name="buffer">Audio buffer</param>
    /// <param name="offset">Offset into buffer</param>
    /// <param name="count">Number of bytes required</param>
    /// <returns>Number of bytes read</returns>
    public override int Read(byte[] buffer, int offset, int count)
    {
        WaveBuffer waveBuffer = new(buffer);
        int samplesRequired = count / 4;
        int samplesRead = Read(waveBuffer.FloatBuffer, offset / 4, samplesRequired);
        return samplesRead * 4;
    }

    /// <summary>
    /// Reads audio from this sample provider
    /// </summary>
    /// <param name="buffer">Sample buffer</param>
    /// <param name="offset">Offset into sample buffer</param>
    /// <param name="count">Number of samples required</param>
    /// <returns>Number of samples read</returns>
    public int Read(float[] buffer, int offset, int count)
    {
        lock (Sync)
        {
            return Provider.Read(buffer, offset, count);
        }
    }

    /// <summary>
    /// Helper to convert source to dest bytes
    /// </summary>
    long SourceToDest(long sourceBytes)
    {
        return DstBytesPerSample * (sourceBytes / SrcBytesPerSample);
    }

    /// <summary>
    /// Helper to convert dest to source bytes
    /// </summary>
    long DestToSource(long destBytes)
    {
        return SrcBytesPerSample * (destBytes / DstBytesPerSample);
    }

    /// <summary>
    /// Disposes this AudioFileReader
    /// </summary>
    /// <param name="disposing">True if called from Dispose</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (Reader != null)
            {
                Reader.Dispose();
                if (Provider != Reader && Provider is IDisposable provider)
                    provider.Dispose();
                Reader = null;
            }
        }
        base.Dispose(disposing);
    }
}
