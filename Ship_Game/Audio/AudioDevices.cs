using System;
using System.Collections.Generic;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using SDUtils;
using Ship_Game.Utils;

namespace Ship_Game.Audio;

public class AudioDevices : IDisposable
{
    /// <summary>
    /// We ALWAYS enumerate ALL audio devices, just in case User changes
    /// Audio devices while the game is running.
    /// </summary>
    public Array<MMDevice> Devices => EnumerateActiveDevices().ToArrayList();

    IEnumerable<MMDevice> EnumerateActiveDevices()
    {
        MMDeviceCollection devices;
        try
        {
            devices = Enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
        }
        catch (Exception e)
        {
            Log.Warning($"AudioDevices: No available devices ({e.Message})");
            yield break;
        }

        foreach (MMDevice device in devices)
            yield return device;
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

    /// <summary>
    /// The currently active audio device
    /// </summary>
    public MMDevice CurrentDevice { get; internal set; }

    /// <summary>
    /// Sets the user's audio device preference.
    /// </summary>
    public void SetUserPreference(MMDevice newDevice)
    {
        GlobalStats.SoundDevice = newDevice.ID;
    }

    /// <summary>
    /// If TRUE, user prefers whatever is the system default device currently.
    /// That means the game must automatically switch devices when
    /// Windows Default Device changes while game is running.
    /// </summary>
    public bool UserPrefersDefaultDevice => GlobalStats.SoundDevice.Equals("Default", StringComparison.OrdinalIgnoreCase);

    // slow; finds the FriendlyName of a device, or returns null
    string FindDeviceFriendlyName(string id)
    {
        foreach (MMDevice device in EnumerateActiveDevices())
            if (device.ID == id)
                return device.FriendlyName;
        return null;
    }

    /// <summary>
    /// Finds a device by MMDevice.ID
    /// </summary>
    public MMDevice FindDevice(string id, IEnumerable<MMDevice> devices = null)
    {
        devices ??= EnumerateActiveDevices();
        foreach (MMDevice device in devices)
            if (device.ID.Equals(id, StringComparison.OrdinalIgnoreCase))
                return device;
        return null;
    }

    /// <summary>
    /// Attempts to select an audio device
    /// First it checks player preference
    /// Then default device
    /// Then any available device
    /// </summary>
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
            var device = FindDevice(userDevice, devices);
            if (device != null) return device;
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

    /// <summary>
    /// If this is set `true`, the game should reload audio devices at the next opportunity
    /// </summary>
    public bool ShouldReloadAudioDevice { get; set; }

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

    // This will set `ShouldReloadAudioDevice=true`
    public void HandleEvents()
    {
        // Group the events by device ID, because IMMNotificationClient can send multiple events
        // when the device is flagged as Console(Games) + Multimedia(Music) + Communications(VoiceChat).
        // If it exists, it should be able to play audio and the user can switch to it.
        // We don't want to exclude devices if they never report Console(Games) role
        Array<string> defaultChanged = new();
        Array<string> added = new();
        Array<string> removed = new();

        while (Events.TryDequeue(out DeviceEvent evt))
        {
            switch (evt.Type)
            {
                case EventType.DefaultChanged: defaultChanged.AddUnique(evt.DeviceId); break;
                case EventType.Added: added.AddUnique(evt.DeviceId); break;
                case EventType.Removed: removed.AddUnique(evt.DeviceId); break;
            }
        }

        foreach (string deviceId in added)
        {
            //Log.Info($"AudioDevices.Added: {FindDeviceFriendlyName(deviceId)}  ({deviceId})");
            if (CurrentDevice == null) // we didn't have any device (all speakers unplugged?)
            {
                ShouldReloadAudioDevice = true;
            }
        }

        foreach (string deviceId in removed)
        {
            // if current device was removed, auto-pick new
            if (CurrentDevice != null && CurrentDevice.ID == deviceId)
            {
                ShouldReloadAudioDevice = true;
            }
        }

        foreach (string deviceId in defaultChanged)
        {
            // if preferring default device AND user has chosen a NEW default device
            if (UserPrefersDefaultDevice && CurrentDevice != null && CurrentDevice.ID != deviceId)
            {
                //Log.Info($"AudioDevices.DefaultChanged: {FindDeviceFriendlyName(deviceId)}  ({deviceId})");
                ShouldReloadAudioDevice = true;
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
            // we only care about audio render devices (output devices)
            if (dataFlow == DataFlow.Render)
            {
                Devices.Events.Enqueue(new(){ Type = EventType.DefaultChanged, DeviceId = defaultDeviceId});
            }
        }

        // completely new device
        public void OnDeviceAdded(string deviceId)
        {
            Devices.Events.Enqueue(new(){ Type = EventType.Added, DeviceId = deviceId});
        }

        // the device was removed from system devices list (uninstalled?)
        public void OnDeviceRemoved(string deviceId)
        {
            Devices.Events.Enqueue(new(){ Type = EventType.Removed, DeviceId = deviceId});
        }

        public void OnDeviceStateChanged(string deviceId, DeviceState newState)
        {
            // device unplugged or disabled temporarily
            if (newState == DeviceState.Unplugged || newState == DeviceState.Disabled)
                Devices.Events.Enqueue(new(){ Type = EventType.Removed, DeviceId = deviceId});
            else if (newState == DeviceState.Active)
                Devices.Events.Enqueue(new(){ Type = EventType.Added, DeviceId = deviceId});
        }

        public void OnPropertyValueChanged(string deviceId, PropertyKey propertyKey)
        {
        }
    }
}