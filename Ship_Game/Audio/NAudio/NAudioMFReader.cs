﻿using System;
using System.IO;
using System.Runtime.InteropServices;
using NAudio.CoreAudioApi.Interfaces;
using NAudio.MediaFoundation;
using NAudio.Utils;
using NAudio.Wave;

// ReSharper disable once CheckNamespace
namespace Ship_Game.Audio.NAudio;

/// <summary>
/// Class for reading any file that Media Foundation can play
/// Will only work in Windows Vista and above
/// Automatically converts to PCM
/// If it is a video file with multiple audio streams, it will pick out the first audio stream
/// </summary>
public class NAudioMFReader : WaveStream
{
    WaveFormat waveFormat;
    long length;
    MediaFoundationReaderSettings settings;
    readonly string file;
    IMFSourceReader pReader;

    long position;

    /// <summary>
    /// Allows customization of this reader class
    /// </summary>
    public class MediaFoundationReaderSettings
    {
        /// <summary>
        /// Sets up the default settings for MediaFoundationReader
        /// </summary>
        public MediaFoundationReaderSettings()
        {
            RepositionInRead = true;
        }

        /// <summary>
        /// Allows us to request IEEE float output (n.b. no guarantee this will be accepted)
        /// </summary>
        public bool RequestFloatOutput { get; set; }
        /// <summary>
        /// If true, the reader object created in the constructor is used in Read
        /// Should only be set to true if you are working entirely on an STA thread, or 
        /// entirely with MTA threads.
        /// </summary>
        public bool SingleReaderObject { get; set; }
        /// <summary>
        /// If true, the reposition does not happen immediately, but waits until the
        /// next call to read to be processed.
        /// </summary>
        public bool RepositionInRead { get; set; }
    }

    /// <summary>
    /// Default constructor
    /// </summary>
    protected NAudioMFReader()
    {
    }
        
    /// <summary>
    /// Creates a new MediaFoundationReader based on the supplied file
    /// </summary>
    /// <param name="file">Filename (can also be a URL  e.g. http:// mms:// file://)</param>
    public NAudioMFReader(string file) : this(file, null)
    {
    }

    /// <summary>
    /// Creates a new MediaFoundationReader based on the supplied file
    /// </summary>
    /// <param name="file">Filename</param>
    /// <param name="settings">Advanced settings</param>
    public NAudioMFReader(string file, MediaFoundationReaderSettings settings)
    {
        this.file = file;
        Init(settings);
    }

    /// <summary>
    /// Initializes 
    /// </summary>
    protected void Init(MediaFoundationReaderSettings initialSettings)
    {
        MediaFoundationApi.Startup();
        settings = initialSettings ?? new MediaFoundationReaderSettings();
        var reader = CreateReader(settings);

        waveFormat = GetCurrentWaveFormat(reader);

        reader.SetStreamSelection(MediaFoundationInterop.MF_SOURCE_READER_FIRST_AUDIO_STREAM, true);
        length = GetLength(reader);

        if (settings.SingleReaderObject)
        {
            pReader = reader;
        }
        else
        {
            Marshal.ReleaseComObject(reader);
        }
    }

    static WaveFormat GetCurrentWaveFormat(IMFSourceReader reader)
    {
        reader.GetCurrentMediaType(MediaFoundationInterop.MF_SOURCE_READER_FIRST_AUDIO_STREAM, out IMFMediaType uncompressedMediaType);

        // Two ways to query it, first is to ask for properties (second is to convert into WaveFormatEx using MFCreateWaveFormatExFromMFMediaType)
        var outputMediaType = new MediaType(uncompressedMediaType);
        Guid actualMajorType = outputMediaType.MajorType;
        if (actualMajorType != MediaTypes.MFMediaType_Audio)
        {
            Log.Error($"Unexpected media type: {actualMajorType}");
        }
        Guid audioSubType = outputMediaType.SubType;
        int channels = outputMediaType.ChannelCount;
        int bits = outputMediaType.BitsPerSample;
        int sampleRate = outputMediaType.SampleRate;

        if (audioSubType == AudioSubtypes.MFAudioFormat_PCM)
            return new(sampleRate, bits, channels);
        if (audioSubType == AudioSubtypes.MFAudioFormat_Float)
            return WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
        var subTypeDescription = FieldDescriptionHelper.Describe(typeof (AudioSubtypes), audioSubType);
        throw new InvalidDataException($"Unsupported audio sub Type {subTypeDescription}");
    }

    static MediaType GetCurrentMediaType(IMFSourceReader reader)
    {
        reader.GetCurrentMediaType(MediaFoundationInterop.MF_SOURCE_READER_FIRST_AUDIO_STREAM, out IMFMediaType mediaType);
        return new(mediaType);
    }

    /// <summary>
    /// Creates the reader (overridable by )
    /// </summary>
    protected virtual IMFSourceReader CreateReader(MediaFoundationReaderSettings settings)
    {
        MediaFoundationInterop.MFCreateSourceReaderFromURL(file, null, out IMFSourceReader reader);
        reader.SetStreamSelection(MediaFoundationInterop.MF_SOURCE_READER_ALL_STREAMS, false);
        reader.SetStreamSelection(MediaFoundationInterop.MF_SOURCE_READER_FIRST_AUDIO_STREAM, true);

        // Create a partial media type indicating that we want uncompressed PCM audio

        MediaType partialMediaType = new();
        partialMediaType.MajorType = MediaTypes.MFMediaType_Audio;
        partialMediaType.SubType = settings.RequestFloatOutput ? AudioSubtypes.MFAudioFormat_Float : AudioSubtypes.MFAudioFormat_PCM;

        MediaType currentMediaType = GetCurrentMediaType(reader);

        // mono, low sample rate files can go wrong on Windows 10 unless we specify here
        partialMediaType.ChannelCount = currentMediaType.ChannelCount;
        partialMediaType.SampleRate = currentMediaType.SampleRate;

        try
        {
            // set the media type
            // can return MF_E_INVALIDMEDIATYPE if not supported
            reader.SetCurrentMediaType(MediaFoundationInterop.MF_SOURCE_READER_FIRST_AUDIO_STREAM, IntPtr.Zero, partialMediaType.MediaFoundationObject);
        }
        catch (COMException ex) when (ex.GetHResult() == MediaFoundationErrors.MF_E_INVALIDMEDIATYPE)
        {               
            // HE-AAC (and v2) seems to halve the samplerate
            if (currentMediaType.SubType == AudioSubtypes.MFAudioFormat_AAC && currentMediaType.ChannelCount == 1)
            {
                partialMediaType.SampleRate = currentMediaType.SampleRate *= 2;
                partialMediaType.ChannelCount = currentMediaType.ChannelCount *= 2;
                reader.SetCurrentMediaType(MediaFoundationInterop.MF_SOURCE_READER_FIRST_AUDIO_STREAM, IntPtr.Zero, partialMediaType.MediaFoundationObject);
            }
            else { throw; }
        }

        Marshal.ReleaseComObject(currentMediaType.MediaFoundationObject);
        return reader;
    }

    long GetLength(IMFSourceReader reader)
    {
        var variantPtr = Marshal.AllocHGlobal(Marshal.SizeOf<PropVariant>());
        try
        {

            // http://msdn.microsoft.com/en-gb/library/windows/desktop/dd389281%28v=vs.85%29.aspx#getting_file_duration
            int hResult = reader.GetPresentationAttribute(MediaFoundationInterop.MF_SOURCE_READER_MEDIASOURCE,
                MediaFoundationAttributes.MF_PD_DURATION, variantPtr);
            if (hResult == MediaFoundationErrors.MF_E_ATTRIBUTENOTFOUND)
            {
                // this doesn't support telling us its duration (might be streaming)
                return 0;
            }
            if (hResult != 0)
            {
                Marshal.ThrowExceptionForHR(hResult);
            }
            var variant = Marshal.PtrToStructure<PropVariant>(variantPtr);

            var lengthInBytes = (((long)variant.Value) * waveFormat.AverageBytesPerSecond) / 10000000L;
            return lengthInBytes;
        }
        finally 
        {
            PropVariant.Clear(variantPtr);
            Marshal.FreeHGlobal(variantPtr);
        }
    }

    byte[] decoderOutputBuffer;
    int decoderOutputOffset;
    int decoderOutputCount;

    void EnsureBuffer(int bytesRequired)
    {
        if (decoderOutputBuffer == null || decoderOutputBuffer.Length < bytesRequired)
        {
            decoderOutputBuffer = new byte[bytesRequired];
        }
    }

    /// <summary>
    /// Reads from this wave stream
    /// </summary>
    /// <param name="buffer">Buffer to read into</param>
    /// <param name="offset">Offset in buffer</param>
    /// <param name="count">Bytes required</param>
    /// <returns>Number of bytes read; 0 indicates end of stream</returns>
    public override int Read(byte[] buffer, int offset, int count)
    {
        pReader ??= CreateReader(settings);
        if (repositionTo != -1)
        {
            Reposition(repositionTo);
        }

        int bytesWritten = 0;
        // read in any leftovers from last time
        if (decoderOutputCount > 0)
        {
            bytesWritten += ReadFromDecoderBuffer(buffer, offset, count);
        }

        while (bytesWritten < count)
        {
            pReader.ReadSample(MediaFoundationInterop.MF_SOURCE_READER_FIRST_AUDIO_STREAM, 0, 
                out int actualStreamIndex, out MF_SOURCE_READER_FLAG dwFlags, out ulong timestamp, out IMFSample pSample);
            if ((dwFlags & MF_SOURCE_READER_FLAG.MF_SOURCE_READERF_ENDOFSTREAM) != 0)
            {
                // reached the end of the stream
                break;
            }
            else if ((dwFlags & MF_SOURCE_READER_FLAG.MF_SOURCE_READERF_CURRENTMEDIATYPECHANGED) != 0)
            {
                waveFormat = GetCurrentWaveFormat(pReader);
                OnWaveFormatChanged();
                // carry on, but user must handle the change of format
            }
            else if (dwFlags != 0)
            {
                throw new InvalidOperationException($"MediaFoundationReadError {dwFlags}");
            }

            pSample.ConvertToContiguousBuffer(out IMFMediaBuffer pBuffer);
            pBuffer.Lock(out IntPtr pAudioData, out int pcbMaxLength, out int cbBuffer);
            EnsureBuffer(cbBuffer);
            Marshal.Copy(pAudioData, decoderOutputBuffer, 0, cbBuffer);
            decoderOutputOffset = 0;
            decoderOutputCount = cbBuffer;

            bytesWritten += ReadFromDecoderBuffer(buffer, offset + bytesWritten, count - bytesWritten);

            pBuffer.Unlock();
            Marshal.ReleaseComObject(pBuffer);
            Marshal.ReleaseComObject(pSample);
        }
        position += bytesWritten;
        return bytesWritten;
    }

    int ReadFromDecoderBuffer(byte[] buffer, int offset, int needed)
    {
        int bytesFromDecoderOutput = Math.Min(needed, decoderOutputCount);
        Array.Copy(decoderOutputBuffer, decoderOutputOffset, buffer, offset, bytesFromDecoderOutput);
        decoderOutputOffset += bytesFromDecoderOutput;
        decoderOutputCount -= bytesFromDecoderOutput;
        if (decoderOutputCount == 0)
        {
            decoderOutputOffset = 0;
        }
        return bytesFromDecoderOutput;
    }

    /// <summary>
    /// WaveFormat of this stream (n.b. this is after converting to PCM)
    /// </summary>
    public override WaveFormat WaveFormat => waveFormat;

    /// <summary>
    /// The bytesRequired of this stream in bytes (n.b may not be accurate)
    /// </summary>
    public override long Length => length;

    /// <summary>
    /// Current position within this stream
    /// </summary>
    public override long Position
    {
        get => position;
        set
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException("value", "Position cannot be less than 0");
            if (settings.RepositionInRead)
            {
                repositionTo = value;
                position = value; // for gui apps, make it look like we have alread processed the reposition
            }
            else
            {
                Reposition(value);
            }
        }
    }

    long repositionTo = -1;

    void Reposition(long desiredPosition)
    {
        long nsPosition = (10000000L * repositionTo) / waveFormat.AverageBytesPerSecond;
        var pv = PropVariant.FromLong(nsPosition);
        var ptr = Marshal.AllocHGlobal(Marshal.SizeOf(pv));
        try
        {
            Marshal.StructureToPtr(pv, ptr, false);

            // should pass in a variant of type VT_I8 which is a long containing time in 100nanosecond units
            pReader.SetCurrentPosition(Guid.Empty, ptr);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
        decoderOutputCount = 0;
        decoderOutputOffset = 0;
        position = desiredPosition;
        repositionTo = -1;// clear the flag
    }

    /// <summary>
    /// Cleans up after finishing with this reader
    /// </summary>
    /// <param name="disposing">true if called from Dispose</param>
    protected override void Dispose(bool disposing)
    {
        if (pReader != null)
        {
            Marshal.ReleaseComObject(pReader);
            pReader = null;
        }
        base.Dispose(disposing);
    }

    /// <summary>
    /// WaveFormat has changed
    /// </summary>
    public event EventHandler WaveFormatChanged;

    void OnWaveFormatChanged()
    {
        WaveFormatChanged?.Invoke(this, EventArgs.Empty);
    }
}
