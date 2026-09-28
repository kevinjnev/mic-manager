using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;

namespace MicManager
{
    public class AudioEngine
    {
        private WaveInEvent _micIn;
        private WaveOutEvent _virtualCableOut;
        private BufferedWaveProvider _micBuffer;
        private MixingSampleProvider _mixer;

        public void StartRouting(int micDeviceIndex, int virtualCableOutputIndex)
        {
            // 1. Setup Microphone Input
            _micIn = new WaveInEvent();
            _micIn.DeviceNumber = micDeviceIndex;
            _micIn.WaveFormat = new WaveFormat(44100, 2); // Standard format

            _micBuffer = new BufferedWaveProvider(_micIn.WaveFormat);
            _micBuffer.DiscardOnBufferOverflow = true;

            _micIn.DataAvailable += (s, a) =>
            {
                _micBuffer.AddSamples(a.Buffer, 0, a.BytesRecorded);
            };

            // 2. Setup the Mixer (Combines Mic + Soundboard)
            _mixer = new MixingSampleProvider(WaveFormat.CreateIeeeFloatWaveFormat(44100, 2));
            _mixer.ReadFully = true;

            // Convert Mic byte buffer to ISampleProvider and add to mixer
            var micSampleProvider = new WaveToSampleProvider(_micBuffer);
            _mixer.AddMixerInput(micSampleProvider);

            // 3. Setup Output to Virtual Cable
            _virtualCableOut = new WaveOutEvent();
            _virtualCableOut.DeviceNumber = virtualCableOutputIndex;
            _virtualCableOut.Init(_mixer);

            _micIn.StartRecording();
            _virtualCableOut.Play();
        }

        public void PlaySoundboardFile(string filePath)
        {
            if (_mixer == null) return;

            try
            {
                var audioFile = new AudioFileReader(filePath);

                // Ensure the soundboard file matches the mixer sample rate
                var resampler = new WdlResamplingSampleProvider(audioFile, _mixer.WaveFormat.SampleRate);
                var channelConverter = new MultiplexingSampleProvider(new ISampleProvider[] { resampler }, 2);

                _mixer.AddMixerInput(channelConverter);
            }
            catch (Exception ex)
            {
                // Handle file read errors
                Console.WriteLine($"Error playing file: {ex.Message}");
            }
        }

        public void Stop()
        {
            _micIn?.StopRecording();
            _virtualCableOut?.Stop();
            _micIn?.Dispose();
            _virtualCableOut?.Dispose();
        }
    }
}