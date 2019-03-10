using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;

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
                    var enumerator = new MMDeviceEnumerator();
                    return enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active).ToArrayList();
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
                    var enumerator = new MMDeviceEnumerator();
                    return enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
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
            if ("Default".Equals(userDevice, StringComparison.OrdinalIgnoreCase))
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
    }
}
