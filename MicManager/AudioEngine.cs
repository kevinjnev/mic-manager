using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;

namespace MicManager
{
    public class AudioRoute
    {
        public string CableName { get; set; }
        public BufferedWaveProvider MicBuffer { get; set; }
        public MixingSampleProvider Mixer { get; set; }
        public VolumeSampleProvider VolumeControl { get; set; }
        public WaveOutEvent Output { get; set; }

        public bool IsMuted
        {
            get => VolumeControl.Volume == 0f;
            set => VolumeControl.Volume = value ? 0f : 1f;
        }
    }

    public class AudioEngine
    {
        private WaveInEvent _micIn;
        public List<AudioRoute> Routes { get; private set; } = new List<AudioRoute>();

        public void StartRouting(int micDeviceIndex, Dictionary<string, int> virtualCables)
        {
            _micIn = new WaveInEvent();
            _micIn.DeviceNumber = micDeviceIndex;
            _micIn.WaveFormat = new WaveFormat(44100, 16, 2); // Force 16-bit PCM

            foreach (var cable in virtualCables)
            {
                var micBuffer = new BufferedWaveProvider(_micIn.WaveFormat) { DiscardOnBufferOverflow = true };

                var mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
                mixer.ReadFully = true;

                // Safely convert the 16-bit PCM buffer to Float for the mixer
                mixer.AddMixerInput(micBuffer.ToSampleProvider());

                var volumeControl = new VolumeSampleProvider(mixer) { Volume = 1.0f };

                var output = new WaveOutEvent { DeviceNumber = cable.Value };

                // Safely convert the Float mixer back down to 16-bit PCM for the Virtual Cable
                output.Init(new SampleToWaveProvider16(volumeControl));
                output.Play();

                Routes.Add(new AudioRoute
                {
                    CableName = cable.Key,
                    MicBuffer = micBuffer,
                    Mixer = mixer,
                    VolumeControl = volumeControl,
                    Output = output
                });
            }

            _micIn.DataAvailable += (s, a) =>
            {
                foreach (var route in Routes)
                {
                    route.MicBuffer.AddSamples(a.Buffer, 0, a.BytesRecorded);
                }
            };

            _micIn.StartRecording();
        }

        public void PlaySoundboardFile(string filePath)
        {
            foreach (var route in Routes)
            {
                try
                {
                    var audioFile = new AudioFileReader(filePath);
                    var resampler = new WdlResamplingSampleProvider(audioFile, route.Mixer.WaveFormat.SampleRate);

                    // Ensure the audio file is forced to stereo to match the mixer
                    ISampleProvider channelProvider = resampler;
                    if (resampler.WaveFormat.Channels == 1)
                    {
                        channelProvider = new MonoToStereoSampleProvider(resampler);
                    }

                    route.Mixer.AddMixerInput(channelProvider);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error playing file on {route.CableName}: {ex.Message}");
                }
            }
        }

        public void Stop()
        {
            _micIn?.StopRecording();
            _micIn?.Dispose();
            foreach (var route in Routes)
            {
                route.Output?.Stop();
                route.Output?.Dispose();
            }
        }
    }
}