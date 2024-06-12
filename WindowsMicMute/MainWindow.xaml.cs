using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Microsoft.Win32;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace WindowsMicMute
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const int GwlExStyle = -20;
        private const int WsExAppWindow = 0x00040000, WsExToolWindow = 0x00000080;

        private MMDevice? _device;
        private WaveInEvent? _waveIn;

        public bool IsMute
        {
            get => _isMute;
            set => SetField(ref _isMute, value);
        }

        public float AudioLevel
        {
            get => _audioLevel;
            set => SetField(ref _audioLevel, value);
        }

        public double AppWidth
        {
            get => _appWidth;
            set => SetField(ref _appWidth, value);
        }

        public double AppHeight
        {
            get => _appHeight;
            set => SetField(ref _appHeight, value);
        }

        private bool _isMute;
        private float _audioLevel;
        private double _appWidth = 50;
        private double _appHeight = 50;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }


        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var helper = new WindowInteropHelper(this).Handle;
                //Performing some magic to hide the form from Alt+Tab
                _ = SetWindowLong(helper, GwlExStyle,
                    (GetWindowLong(helper, GwlExStyle) | WsExToolWindow) & ~WsExAppWindow);

                Width = AppWidth;
                Height = AppHeight;

                var enumerator = new MMDeviceEnumerator();
                _device = enumerator.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);

                if (_device == null)
                {
                    IsMute = true;
                }

                _waveIn = new WaveInEvent();
                _waveIn.DataAvailable += OnDataAvailable;
                _waveIn.StartRecording();

                if (_device != null)
                {
                    IsMute = _device.AudioEndpointVolume.Mute;
                }

                InstallOnStartUp();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        private void OnDataAvailable(object? sender, WaveInEventArgs args)
        {
            //From https://github.com/naudio/NAudio/blob/master/Docs/RecordingLevelMeter.md
            float max = 0;
            for (int index = 0; index < args.BytesRecorded; index += 2)
            {
                var sample = (short)((args.Buffer[index + 1] << 8) | args.Buffer[index + 0]);
                var sample32 = sample / 32768f;
                if (sample32 < 0) sample32 = -sample32;
                if (sample32 > max) max = sample32;
            }

            AudioLevel = max * 100;
        }

        private void ToggleMute_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (_device != null)
                {
                    IsMute = _device.AudioEndpointVolume.Mute;
                    _device.AudioEndpointVolume.Mute = !IsMute;
                    IsMute = _device.AudioEndpointVolume.Mute;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        private void OnMouseLefButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }


        private static void InstallOnStartUp()
        {
            try
            {
                // Open the registry key where startup applications are listed
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                if (key != null)
                {
                    var curAssembly = Assembly.GetExecutingAssembly();
                    var exePath = curAssembly.Location.Replace(".dll", ".exe");
                    var exeName = Path.GetFileName(exePath);
                    var existed = key.GetValue(exeName);

                    // If the key does not exist, set it to start the current executable on startup
                    if (existed == null)
                    {
                        key.SetValue(exeName, exePath);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log or handle the exception as needed
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }


        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);

            _device?.Dispose();
            if (_waveIn != null)
            {
                _waveIn.DataAvailable -= OnDataAvailable;
                _waveIn.StopRecording();
                _waveIn.Dispose();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}