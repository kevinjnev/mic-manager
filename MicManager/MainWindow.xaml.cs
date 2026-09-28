using Microsoft.Win32;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

namespace MicManager
{
    public partial class MainWindow : Window
    {
        private AudioEngine _audioEngine;
        private List<RouteViewModel> _uiRoutes;

        public MainWindow()
        {
            InitializeComponent();
            _audioEngine = new AudioEngine();

            // 0 is typically your default hardware microphone
            int micIndex = 0;

            var virtualCables = new Dictionary<string, int>();

            // Adjust these strings to match the exact names of your installed Virtual Cables
            int cableAIndex = GetDeviceIndex("CABLE Input");
            if (cableAIndex != -1) virtualCables.Add("Discord (Cable A)", cableAIndex);

            int cableBIndex = GetDeviceIndex("CABLE-B Input");
            if (cableBIndex != -1) virtualCables.Add("Game/OBS (Cable B)", cableBIndex);

            if (virtualCables.Count > 0)
            {
                _audioEngine.StartRouting(micIndex, virtualCables);

                _uiRoutes = new List<RouteViewModel>();
                foreach (var route in _audioEngine.Routes)
                {
                    _uiRoutes.Add(new RouteViewModel(route));
                }

                AppList.ItemsSource = _uiRoutes;
            }
            else
            {
                MessageBox.Show("No Virtual Cables found! Please install them and verify their names in Sound Settings.");
            }
        }

        private int GetDeviceIndex(string name)
        {
            for (int i = 0; i < WaveOut.DeviceCount; i++)
            {
                if (WaveOut.GetCapabilities(i).ProductName.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                    return i;
            }
            return -1;
        }

        private void PlaySound_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Audio Files (*.mp3;*.wav)|*.mp3;*.wav"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _audioEngine.PlaySoundboardFile(openFileDialog.FileName);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _audioEngine?.Stop();
            base.OnClosed(e);
        }
    }

    public class RouteViewModel : INotifyPropertyChanged
    {
        private AudioRoute _route;
        private float _volume;

        public RouteViewModel(AudioRoute route)
        {
            _route = route;
            _volume = 100f;
        }

        public string AppName => _route.CableName;

        public bool IsMuted
        {
            get => _route.IsMuted;
            set
            {
                if (_route.IsMuted != value)
                {
                    _route.IsMuted = value;
                    if (value) _volume = 0f;

                    OnPropertyChanged(nameof(IsMuted));
                    OnPropertyChanged(nameof(Volume));
                }
            }
        }

        public float Volume
        {
            get => _volume;
            set
            {
                if (_volume != value)
                {
                    _volume = value;
                    _route.VolumeControl.Volume = value / 100f;

                    if (value > 0 && IsMuted)
                    {
                        _route.IsMuted = false;
                        OnPropertyChanged(nameof(IsMuted));
                    }
                    else if (value == 0 && !IsMuted)
                    {
                        _route.IsMuted = true;
                        OnPropertyChanged(nameof(IsMuted));
                    }

                    OnPropertyChanged(nameof(Volume));
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}