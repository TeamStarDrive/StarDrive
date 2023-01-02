using System;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using SDUtils;
using Ship_Game.Utils;

namespace Ship_Game.Audio;

public class AudioDevices : IDisposable
{
    /// <summary>
    /// We ALWAYS enumerate ALL audio devices, just in case User changes
    /// Audio devices while the game is running
    /// </summary>
    public Array<MMDevice> Devices
    {
        get
        {
            try
            {
                return Enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).ToArrayList();
            }
            catch (Exception e)
            {
                Log.Warning($"AudioDevices: No available devices ({e.Message})");
                return new();
            }
        }
    }

    /// <summary>
    /// Query for the Default Audio device
    /// </summary>
    public MMDevice DefaultDevice
    {
        get
        {
            try
            {
                return Enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
            }
            catch (Exception e)
            {
                Log.Warning($"AudioDevices: No default device ({e.Message})");
                return null;
            }
        }
    }

    static string Id(MMDevice device)
    {
        return device.ID;
    }

    public MMDevice CurrentDevice { get; internal set; }

    public void SetUserPreference(MMDevice newDevice)
    {
        GlobalStats.SoundDevice = Id(newDevice);
    }

    public bool UserPrefersDefaultDevice => "Default".Equals(GlobalStats.SoundDevice, StringComparison.OrdinalIgnoreCase);

    /**
         * Attempts to select an audio device
         * First it checks player preference
         * Then default device
         * Then any available device
         */
    public bool PickAudioDevice(out MMDevice selected)
    {
        selected = PickDevice();
        return selected != null;
    }
        
    MMDevice PickDevice()
    {
        MMDevice defaultDevice = DefaultDevice;
        Array<MMDevice> devices = Devices;

        string userDevice = GlobalStats.SoundDevice;
        if (UserPrefersDefaultDevice)
        {
            if (defaultDevice != null)
                return defaultDevice;
        }
        else if (userDevice.NotEmpty())
        {
            foreach (MMDevice device in devices)
            {
                if (Id(device).Equals(userDevice, StringComparison.OrdinalIgnoreCase))
                    return device;
            }
        }

        // fallback, select default device, because someone messed up userDevice
        GlobalStats.SoundDevice = "Default";
        if (defaultDevice != null)
            return defaultDevice;

        // we are almost screwed, there is no default device, so pick whatever works:
        if (devices.Count > 0)
        {
            foreach (MMDevice device in devices)
            {
                if (!device.AudioEndpointVolume.Mute)
                    return device;
            }
            return devices[0];
        }
        return null;
    }

    MMDeviceEnumerator Enumerator;
    readonly AudioNotificationClient Notifications;
    readonly SafeQueue<DeviceEvent> Events = new();

    public AudioDevices()
    {
        Notifications = new(this);
        Enumerator = new();
        Enumerator.RegisterEndpointNotificationCallback(Notifications);
    }

    public void Dispose()
    {
        Enumerator?.UnregisterEndpointNotificationCallback(Notifications);
        Enumerator = null;
    }

    enum EventType
    {
        DefaultChanged,
        Added,
        Removed
    }

    struct DeviceEvent
    {
        public EventType Type;
        public string DeviceId;
    }

    internal void HandleEvents()
    {
        while (Events.TryDequeue(out DeviceEvent evt))
        {
            switch (evt.Type)
            {
                case EventType.DefaultChanged:
                    // if user has chosen default device
                    if (UserPrefersDefaultDevice && Id(CurrentDevice) != evt.DeviceId)
                    {
                        GameAudio.ReloadAfterDeviceChange(null);
                    }
                    break;
                case EventType.Added:
                    // we didn't have any device (all speakers unplugged?)
                    if (CurrentDevice == null)
                    {
                        GameAudio.ReloadAfterDeviceChange(null);
                    }
                    break;
                case EventType.Removed:
                    // if current device was removed, auto-pick new
                    if (Id(CurrentDevice) == evt.DeviceId)
                    {
                        GameAudio.ReloadAfterDeviceChange(null);
                    }
                    break;
            }
        }
    }

    class AudioNotificationClient : IMMNotificationClient
    {
        readonly AudioDevices Devices;
        public AudioNotificationClient(AudioDevices devices)
        {
            Devices = devices;
            if (Environment.OSVersion.Version.Major < 6)
                throw new NotSupportedException("This functionality is only supported on Windows Vista or newer.");
        }

        public void OnDefaultDeviceChanged(DataFlow dataFlow, Role deviceRole, string defaultDeviceId)
        {
            Devices.Events.Enqueue(new(){ Type = EventType.DefaultChanged, DeviceId = defaultDeviceId});
        }

        public void OnDeviceAdded(string deviceId)
        {
            Devices.Events.Enqueue(new(){ Type = EventType.Added, DeviceId = deviceId});
        }

        public void OnDeviceRemoved(string deviceId)
        {
            Devices.Events.Enqueue(new(){ Type = EventType.Removed, DeviceId = deviceId});
        }

        public void OnDeviceStateChanged(string deviceId, DeviceState newState)
        {
        }

        public void OnPropertyValueChanged(string deviceId, PropertyKey propertyKey)
        {
        }
    }
}