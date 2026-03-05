using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace TWChatOverlay.Services
{
    public class HotKeyService : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public const int EXIT_HOTKEY_ID = 9001;
        public const uint MOD_ALT = 0x0001;
        public const uint VK_F1 = 0x70;

        private const int WM_HOTKEY = 0x0312;

        private readonly IntPtr _handle;
        private readonly HwndSource _source;
        public event Action<int> HotKeyPressed;

        public HotKeyService(IntPtr handle)
        {
            _handle = handle;
            _source = HwndSource.FromHwnd(_handle);
            _source.AddHook(HwndHook);
        }

        public bool Register(int id, uint modifiers, uint vk)
        {
            return RegisterHotKey(_handle, id, modifiers, vk);
        }

        public void Unregister(int id)
        {
            UnregisterHotKey(_handle, id);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                HotKeyPressed?.Invoke(id);
                handled = true;
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            _source?.RemoveHook(HwndHook);
        }
    }
}