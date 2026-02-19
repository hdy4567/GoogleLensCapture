using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Diagnostics;

namespace GoogleLensCapture
{
    public partial class App : System.Windows.Application
    {
        private GlobalHotkey? _hotkey;
        private NotifyIcon? _notifyIcon;
        private const string StartupKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "GoogleLensCapture";

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var mainWindow = new MainWindow();
            this.MainWindow = mainWindow;
            mainWindow.Visibility = Visibility.Hidden;
            mainWindow.ShowInTaskbar = false;

            SetupTrayIcon();

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

        private void SetupTrayIcon()
        {
            _notifyIcon = new NotifyIcon();
            _notifyIcon.Icon = SystemIcons.Application; // 기본 아이콘 사용
            _notifyIcon.Text = "Google Lens Screen Capture";
            _notifyIcon.Visible = true;

            // 컨텍스트 메뉴 설정
            var contextMenu = new ContextMenuStrip();

            var startupMenuItem = new ToolStripMenuItem("윈도우 시작 시 자동 실행");
            startupMenuItem.Checked = IsStartupEnabled();
            startupMenuItem.Click += (s, e) =>
            {
                bool newState = !startupMenuItem.Checked;
                SetStartup(newState);
                startupMenuItem.Checked = IsStartupEnabled();
            };

            contextMenu.Items.Add(startupMenuItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("종료", null, (s, e) => Shutdown());
            _notifyIcon.ContextMenuStrip = contextMenu;

            // 더블 클릭 시 캡처 트리거 (선택 사항)
            _notifyIcon.DoubleClick += (s, e) => ScreenCaptureHandler.HandleCapture();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _notifyIcon?.Dispose();
            _hotkey?.Dispose();
            base.OnExit(e);
        }

        private void SetStartup(bool enable)
        {
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(StartupKey, true))
                {
                    if (key != null)
                    {
                        if (enable)
                        {
                            string? exePath = Process.GetCurrentProcess().MainModule?.FileName;
                            if (exePath != null)
                                key.SetValue(AppName, $"\"{exePath}\"");
                        }
                        else
                        {
                            key.DeleteValue(AppName, false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"시작 프로그램 설정 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool IsStartupEnabled()
        {
            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(StartupKey, false))
            {
                return key?.GetValue(AppName) != null;
            }
        }
    }
}
