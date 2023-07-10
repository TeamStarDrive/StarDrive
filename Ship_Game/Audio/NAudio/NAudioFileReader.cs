using System;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using SDUtils;

namespace Ship_Game.Audio.NAudio;

internal class NAudioFileReader : WaveStream, ISampleProvider, IDisposable
{
    public readonly string Name;

    WaveStream Reader; // the waveStream which we will use for all positioning
    readonly ISampleProvider Provider;
    readonly int DstBytesPerSample;
    readonly int SrcBytesPerSample;
    readonly object Sync = new();
    
    /// <summary>
    /// Creates a new NAudioFileReader, converting the input file into the desired outFormat
    /// </summary>
    public NAudioFileReader(WaveFormat outFormat, string audioFile)
    {
        Name = FileSystemExtensions.GetAppRootRelPath(audioFile);

        Reader = CreateReaderStream(outFormat, audioFile);
        Provider = GetCompatibleSampleProvider(outFormat, Reader);

        SrcBytesPerSample = (Reader.WaveFormat.BitsPerSample / 8) * Reader.WaveFormat.Channels;
        DstBytesPerSample = (Provider.WaveFormat.BitsPerSample / 8) * Provider.WaveFormat.Channels;

        WaveFormat = Provider.WaveFormat;
        Length = SourceToDest(Reader.Length);
    }

    static WaveStream CreateMFReader(WaveFormat outFormat, string fileName)
    {
        return new NAudioMFReader(fileName, new()
        {
            RequestFloatOutput = outFormat.Encoding == WaveFormatEncoding.IeeeFloat,
        });
    }

    static WaveStream CreateReaderStream(WaveFormat outFormat, string fileName)
    {
        if (fileName.EndsWith(".m4a", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".aac", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
        {
            return CreateMFReader(outFormat, fileName);
        }
        if (fileName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
        {
            WaveStream stream = new WaveFileReader(fileName);
            if (stream.WaveFormat.Encoding != WaveFormatEncoding.Pcm && 
                stream.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
            {
                stream = WaveFormatConversionStream.CreatePcmStream(stream);
                stream = new BlockAlignReductionStream(stream);
            }
            return stream;
        }
        if (fileName.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
        {
            if (Environment.OSVersion.Version.Major < 6)
                return new Mp3FileReader(fileName);
            else // make MediaFoundationReader the default for MP3 going forwards
                return CreateMFReader(outFormat, fileName);
        }
        if (fileName.EndsWith(".aiff", StringComparison.OrdinalIgnoreCase) || 
            fileName.EndsWith(".aif", StringComparison.OrdinalIgnoreCase))
        {
            return new AiffFileReader(fileName);
        }

        // fall back to media foundation reader, see if that can play it
        return CreateMFReader(outFormat, fileName);
    }

    ISampleProvider GetCompatibleSampleProvider(WaveFormat desired, IWaveProvider reader)
    {
        WaveFormat readerFmt = reader.WaveFormat;
        ISampleProvider output;

        if (reader.WaveFormat.Encoding == WaveFormatEncoding.Pcm)
        {
            if      (readerFmt.BitsPerSample == 8)  output = new Pcm8BitToSampleProvider(reader);
            else if (readerFmt.BitsPerSample == 16) output = new Pcm16BitToSampleProvider(reader);
            else if (readerFmt.BitsPerSample == 24) output = new Pcm24BitToSampleProvider(reader);
            else if (readerFmt.BitsPerSample == 32) output = new Pcm32BitToSampleProvider(reader);
            else throw new InvalidOperationException("Unsupported bit depth");
        }
        else if (readerFmt.Encoding == WaveFormatEncoding.IeeeFloat)
        {
            if (readerFmt.BitsPerSample == 64) output = new WaveToSampleProvider64(reader);
            else                               output = new WaveToSampleProvider(reader);
        }
        else
        {
            throw new ArgumentException("Unsupported source encoding");
        }

        if (output.WaveFormat.SampleRate != desired.SampleRate)
        {
            Log.Write(ConsoleColor.DarkYellow, $"Resampling {Name} {output.WaveFormat.SampleRate}Hz => {desired.SampleRate}Hz");
            output = new WdlResamplingSampleProvider(output, desired.SampleRate);
        }

        // this is super rare, so no need to worry about it
        if (output.WaveFormat.Channels == 1 && desired.Channels == 2)
        {
            Log.Write(ConsoleColor.DarkYellow, $"MonoToStereo {Name}");
            output = new MonoToStereoSampleProvider(output);
        }

        return output;
    }

    /// <summary>
    /// Helper class turning an already 32 bit floating point IWaveProvider
    /// into an ISampleProvider - hopefully not needed for most applications
    /// </summary>
    public class WaveToSampleProvider : SampleProviderConverterBase
    {
        /// <summary>
        /// Initializes a new instance of the WaveToSampleProvider class
        /// </summary>
        /// <param name="source">Source wave provider, must be IEEE float</param>
        public WaveToSampleProvider(IWaveProvider source) : base(source)
        {
            if (source.WaveFormat.Encoding != WaveFormatEncoding.IeeeFloat)
                throw new ArgumentException("Must be already floating point");
        }

        /// <summary>
        /// Reads from this provider
        /// </summary>
        public override int Read(float[] buffer, int offset, int count)
        {
            int bytesNeeded = count * 4;
            EnsureSourceBuffer(bytesNeeded);
            int bytesRead = source.Read(sourceBuffer, 0, bytesNeeded);
            int samplesRead = bytesRead / 4;
            int outputIndex = offset;
            unsafe
            {
                fixed(byte* pBytes = &sourceBuffer[0])
                {
                    float* pFloat = (float*)pBytes;
                    for (int n = 0, i = 0; n < bytesRead; n += 4, i++)
                    {
                        buffer[outputIndex++] = *(pFloat + i);
                    }
                }
            }
            return samplesRead;
        }
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
