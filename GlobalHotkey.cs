using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace GoogleLensCapture
{
    public class GlobalHotkey : IDisposable
    {
        // Windows API 상수
        public const int MOD_CTRL = 0x0002;
        public const int MOD_ALT = 0x0001;
        public const int MOD_SHIFT = 0x0004;
        public const int MOD_WIN = 0x0008;

        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 9999;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private IntPtr _windowHandle;
        private HwndSource _hwndSource;

        public event Action? OnHotkey;

        public GlobalHotkey(System.Windows.Window window)
        {
            if (window == null) throw new ArgumentNullException(nameof(window));

            // 현재 애플리케이션 윈도우 핸들 획득
            var helper = new System.Windows.Interop.WindowInteropHelper(window);
            _windowHandle = helper.EnsureHandle();

            // 윈도우 메시지 후킹
            _hwndSource = HwndSource.FromHwnd(_windowHandle);
            _hwndSource.AddHook(WndProc);
        }

        public void RegisterHotkey(int fsModifiers, int vk)
        {
            if (!RegisterHotKey(_windowHandle, HOTKEY_ID, fsModifiers, vk))
            {
                throw new InvalidOperationException("핫키 등록 실패. 다른 프로그램에서 사용 중일 수 있습니다.");
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY && wParam.ToInt32() == HOTKEY_ID)
            {
                OnHotkey?.Invoke();
                handled = true;
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            UnregisterHotKey(_windowHandle, HOTKEY_ID);
            _hwndSource?.RemoveHook(WndProc);
        }
    }
}
