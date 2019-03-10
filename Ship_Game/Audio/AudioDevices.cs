using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;

namespace Ship_Game.Audio
{
    public static class AudioDevices
    {
        /**
         * @note We ALWAYS enumerate ALL audio devices, just in case User changes Audio devices
         *       while the game is running
         */
        public static Array<MMDevice> Devices
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
                    return new Array<MMDevice>();
                }
            }
        }

        public static MMDevice DefaultDevice
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

        public static string Id(MMDevice device)
        {
            return device.ID;
        }

        public static MMDevice CurrentDevice { get; internal set; }

        public static void SetUserPreference(MMDevice newDevice)
        {
            GlobalStats.SoundDevice = Id(newDevice);
        }

        public static bool UserPrefersDefaultDevice => "Default".Equals(GlobalStats.SoundDevice, StringComparison.OrdinalIgnoreCase);

        /**
         * Attempts to select an audio device
         * First it checks player preference
         * Then default device
         * Then any available device
         */
        public static bool PickAudioDevice(out MMDevice selected)
        {
            selected = PickDevice();
            return selected != null;
        }
        
        static MMDevice PickDevice()
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

        static readonly MMDeviceEnumerator Enumerator;
        static AudioNotificationClient EventHandler;
        static readonly SafeQueue<DeviceEvent> Events = new SafeQueue<DeviceEvent>();

        static AudioDevices()
        {
            Enumerator = new MMDeviceEnumerator();
            EventHandler = new AudioNotificationClient();
            Enumerator.RegisterEndpointNotificationCallback(EventHandler);
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

        internal static void HandleEvents()
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
            public AudioNotificationClient()
            {
                if (Environment.OSVersion.Version.Major < 6)
                    throw new NotSupportedException("This functionality is only supported on Windows Vista or newer.");
            }

            public void OnDefaultDeviceChanged(DataFlow dataFlow, Role deviceRole, string defaultDeviceId)
            {
                Events.Enqueue(new DeviceEvent{ Type = EventType.DefaultChanged, DeviceId = defaultDeviceId});
            }

            public void OnDeviceAdded(string deviceId)
            {
                Events.Enqueue(new DeviceEvent{ Type = EventType.Added, DeviceId = deviceId});
            }

            public void OnDeviceRemoved(string deviceId)
            {
                Events.Enqueue(new DeviceEvent{ Type = EventType.Removed, DeviceId = deviceId});
            }

            public void OnDeviceStateChanged(string deviceId, DeviceState newState)
            {
            }

            public void OnPropertyValueChanged(string deviceId, PropertyKey propertyKey)
            {
            }
        }
    }
}
