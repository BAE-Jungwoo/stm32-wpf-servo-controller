using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace MotorController
{
    internal sealed class MainViewModel : INotifyPropertyChanged
    {
        private IMotorDriver? _driver;
        private bool _isConnecting;
        private MotorState? _currentState;
        private string _errorText = string.Empty;
        private string _portName = "COM1";

        public string PortName
        {
            get => _portName;
            set
            {
                if (_portName != value)
                {
                    _portName = value;
                    OnPropertyChanged(nameof(PortName));
                }
            }
        }

        public string ErrorText
        {
            get => _errorText;
            private set
            {
                if (_errorText != value)
                {
                    _errorText = value;
                    OnPropertyChanged(nameof(ErrorText));
                }
            }
        }

        public bool IsConnected => _driver != null;

        public MotorState? CurrentState
        {
            get => _currentState;
            private set
            {
                if (!Equals(_currentState, value))
                {
                    _currentState = value;
                    OnPropertyChanged(nameof(CurrentState));
                }
            }
        }

        public ICommand ConnectCommand { get; }
        public ICommand DisconnectCommand { get; }

        public ICommand Activate { get; }
        public ICommand Deactivate { get; }
        public ICommand Accelerate { get; }
        public ICommand Decelerate { get; }

        public MainViewModel()
        {
            ConnectCommand = new AsyncRelayCommand(ConnectAsync, () => !_isConnecting && !IsConnected);
            DisconnectCommand = new RelayCommand(Disconnect, () => IsConnected);

            Activate = new AsyncRelayCommand(async () =>
            {
                var state = await _driver.ActivateAsync();
                OnStateReceived(state);
            }, () => IsConnected);

            Deactivate = new AsyncRelayCommand(async () =>
            {
                var state = await _driver.DeactivateAsync();
                OnStateReceived(state);
            }, () => IsConnected);

            Accelerate = new AsyncRelayCommand(async () =>
            {
                var state = await _driver.AccelerateAsync();
                OnStateReceived(state);
            }, () => IsConnected);

            Decelerate = new AsyncRelayCommand(async () =>
            {
                var state = await _driver.DecelerateAsync();
                OnStateReceived(state);
            }, () => IsConnected);
        }

        private async Task ConnectAsync()
        {
            Debug.WriteLine($"ConnectAsync start: attempting to open port '{PortName}'");
            _isConnecting = true;
            RaiseAllCanExecuteChanged();

            try
            {
                var driver = await Task.Run(() => new UartMotorDriver(PortName));
                _driver = driver;
                OnPropertyChanged(nameof(IsConnected));
                ErrorText = string.Empty;
            }
            catch (Exception ex)
            {
                _driver = null;
                OnPropertyChanged(nameof(IsConnected));
                ErrorText = ex.Message;
                System.Diagnostics.Debug.WriteLine($"Connect failed: {ex}");
            }
            finally
            {
                _isConnecting = false;
                RaiseAllCanExecuteChanged();
            }
        }

        private void Disconnect()
        {
            Debug.WriteLine("Disconnect invoked");
            if (_driver is IDisposable d)
            {
                try { d.Dispose(); Debug.WriteLine("Disconnect: disposed driver"); } catch (Exception ex) { Debug.WriteLine($"Disconnect dispose failed: {ex}"); }
            }

            _driver = null;
            OnPropertyChanged(nameof(IsConnected));
            ErrorText = string.Empty;
            RaiseAllCanExecuteChanged();
        }

        private void OnStateReceived(MotorState state)
        {
            // Ensure UI-thread updates
            var app = Application.Current;
            void apply() => CurrentState = state;

            if (app?.Dispatcher?.CheckAccess() == true)
                apply();
            else
                app?.Dispatcher?.BeginInvoke((Action)apply);
            Debug.WriteLine($"OnStateReceived: speed={state.Speed}");
        }

        private void RaiseAllCanExecuteChanged()
        {
            (ConnectCommand as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (DisconnectCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (Activate as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (Deactivate as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (Accelerate as AsyncRelayCommand)?.RaiseCanExecuteChanged();
            (Decelerate as AsyncRelayCommand)?.RaiseCanExecuteChanged();
        }

        private void OnPropertyChanged(string name)
        {
            var app = Application.Current;
            if (app?.Dispatcher != null && !app.Dispatcher.CheckAccess())
            {
                app.Dispatcher.BeginInvoke((Action)(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name))));
            }
            else
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
