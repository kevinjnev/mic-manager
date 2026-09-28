using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NAudio.CoreAudioApi;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace MicManager
{
    public class AppAudioSession
    {
        public string AppName { get; set; }
        public AudioSessionControl SessionControl { get; set; }

        public bool IsMuted
        {
            get => SessionControl.SimpleAudioVolume.Mute;
            set => SessionControl.SimpleAudioVolume.Mute = value;
        }

        public float Volume
        {
            get => SessionControl.SimpleAudioVolume.Volume * 100;
            set => SessionControl.SimpleAudioVolume.Volume = value / 100f;
        }
    }

    public class AppController
    {
        public List<AppAudioSession> GetAppsListeningToCable()
        {
            var activeApps = new List<AppAudioSession>();
            var enumerator = new MMDeviceEnumerator();

            // Get all active microphones/capture devices
            var captureDevices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);

            // Find the Virtual Cable
            var cableDevice = captureDevices.FirstOrDefault(d => d.FriendlyName.Contains("CABLE Output"));

            if (cableDevice == null) return activeApps;

            var sessions = cableDevice.AudioSessionManager.Sessions;

            for (int i = 0; i < sessions.Count; i++)
            {
                var session = sessions[i];
                if (session.GetProcessID != 0) // Ignore system sounds (PID 0)
                {
                    try
                    {
                        var process = Process.GetProcessById((int)session.GetProcessID);
                        activeApps.Add(new AppAudioSession
                        {
                            AppName = process.ProcessName,
                            SessionControl = session
                        });
                    }
                    catch { /* Process might have just closed */ }
                }
            }
            return activeApps;
        }
    }
}