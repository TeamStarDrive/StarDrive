using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;

namespace Ship_Game.Audio
{
    class NAudioNotificationClient : NAudio.CoreAudioApi.Interfaces.IMMNotificationClient
    {
        public void OnDefaultDeviceChanged(DataFlow dataFlow, Role deviceRole, string defaultDeviceId)
        {
            // Do some Work
            Console.WriteLine($"OnDefaultDeviceChanged --> {dataFlow}");
        }

        public void OnDeviceAdded(string deviceId)
        {
            // Do some Work
            Console.WriteLine("OnDeviceAdded -->");
        }

        public void OnDeviceRemoved(string deviceId)
        {
            Console.WriteLine("OnDeviceRemoved -->");
            // Do some Work
        }

        public void OnDeviceStateChanged(string deviceId, DeviceState newState)
        {
            Console.WriteLine($"OnDeviceStateChanged\n Device Id -->{deviceId} : Device State {newState}");
            // Do some Work
        }

        public NAudioNotificationClient()
        {
            //_realEnumerator.RegisterEndpointNotificationCallback();
            if (Environment.OSVersion.Version.Major < 6)
            {
                throw new NotSupportedException("This functionality is only supported on Windows Vista or newer.");
            }
        }

        public void OnPropertyValueChanged(string deviceId, PropertyKey propertyKey)
        {
            Console.WriteLine($"OnPropertyValueChanged: formatId: {propertyKey.formatId}  propertyId: {propertyKey.propertyId}");
        }
    }
}
