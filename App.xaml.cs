using System;
using System.Windows;

namespace GoogleLensCapture
{
    public partial class App : System.Windows.Application
    {
        private GlobalHotkey? _hotkey;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var mainWindow = new MainWindow();
            this.MainWindow = mainWindow;
            mainWindow.Visibility = Visibility.Hidden;
            mainWindow.ShowInTaskbar = false;

            try
            {
                _hotkey = new GlobalHotkey(mainWindow);

                _hotkey.OnHotkey += () =>
                {
                    ScreenCaptureHandler.HandleCapture();
                };

                _hotkey.RegisterHotkey(GlobalHotkey.MOD_CTRL | GlobalHotkey.MOD_SHIFT, 'S');
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to register hotkey: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _hotkey?.Dispose();
            base.OnExit(e);
        }
    }
}
