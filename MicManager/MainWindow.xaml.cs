using Microsoft.Win32;
using System.Windows;
using NAudio.Wave;

namespace MicManager
{
    public partial class MainWindow : Window
    {
        private AudioEngine _audioEngine;
        private AppController _appController;

        public MainWindow()
        {
            InitializeComponent();
            _audioEngine = new AudioEngine();
            _appController = new AppController();

            // Note: In a real app, you would add combo boxes to let the user select these IDs.
            // Device 0 is usually the default mic. You have to find the Virtual Cable Input index.
            int micIndex = 0;
            int virtualCableIndex = GetDeviceIndex("CABLE Input");

            if (virtualCableIndex != -1)
            {
                _audioEngine.StartRouting(micIndex, virtualCableIndex);
            }
            else
            {
                MessageBox.Show("VB-Audio Virtual Cable not found! Please install it.");
            }
        }

        private int GetDeviceIndex(string name)
        {
            for (int i = 0; i < WaveOut.DeviceCount; i++)
            {
                if (WaveOut.GetCapabilities(i).ProductName.Contains(name))
                    return i;
            }
            return -1;
        }

        private void RefreshApps_Click(object sender, RoutedEventArgs e)
        {
            // Fetch apps currently listening to the Virtual Cable and bind to UI
            var apps = _appController.GetAppsListeningToCable();
            AppList.ItemsSource = apps;
        }

        private void PlaySound_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Audio Files (*.mp3;*.wav)|*.mp3;*.wav";
            if (openFileDialog.ShowDialog() == true)
            {
                _audioEngine.PlaySoundboardFile(openFileDialog.FileName);
            }
        }

        protected override void OnClosed(System.EventArgs e)
        {
            _audioEngine.Stop();
            base.OnClosed(e);
        }
    }
}