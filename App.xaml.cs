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

            _hotkey = new GlobalHotkey(mainWindow);
            _hotkey.RegisterHotkey(GlobalHotkey.MOD_CTRL | GlobalHotkey.MOD_SHIFT, (int)System.Windows.Forms.Keys.S);
            _hotkey.OnHotkey += Hotkey_OnHotkey;

            System.Windows.MessageBox.Show(
                "Google Lens Capture Tool 실행 중\n\n단축키: Ctrl + Shift + S\n\n종료하려면 작업 관리자에서 프로세스를 종료하세요.",
                "시작",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        private void Hotkey_OnHotkey()
        {
            ScreenCaptureHandler.HandleCapture();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _hotkey?.Dispose();
            base.OnExit(e);
        }
    }
}
