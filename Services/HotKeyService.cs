using System;
using System.Windows.Interop;

namespace TWChatOverlay.Services
{
    public class HotKeyService : IDisposable
    {
        public const int EXIT_HOTKEY_ID = 9001;
        public const int TOGGLE_OVERLAY_ID = 9002;
        public const int TOGGLE_ADDON_ID = 9003;

        public const uint MOD_ALT = 0x0001;
        public const uint VK_F1 = 0x70; // F1 키
        public const uint VK_F2 = 0x71; // F2 키
        public const uint VK_F3 = 0x72; // F3 키

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
            return NativeMethods.RegisterHotKey(_handle, id, modifiers, vk);
        }

        public void Unregister(int id)
        {
            NativeMethods.UnregisterHotKey(_handle, id);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_HOTKEY)
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