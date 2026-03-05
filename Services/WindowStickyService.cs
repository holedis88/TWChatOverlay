using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using TWChatOverlay.Models;

namespace TWChatOverlay.Services
{
    public class WindowStickyService
    {
        private readonly Window _overlayWindow;
        private readonly ChatSettings _settings;
        private readonly DispatcherTimer _stickyTimer;
        private IntPtr _gameHwnd = IntPtr.Zero;

        private double _dpiX = 1.0;
        private double _dpiY = 1.0;

        public WindowStickyService(Window window, ChatSettings settings)
        {
            _overlayWindow = window;
            _settings = settings;

            _stickyTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _stickyTimer.Tick += (s, e) => UpdatePosition();
        }

        public void Start()
        {
            UpdateDpi();
            _stickyTimer.Start();
        }

        public void Stop() => _stickyTimer.Stop();

        private void UpdateDpi()
        {
            // PresentationSource가 null일 경우를 대비해 Handle로 찾기
            var hwnd = new WindowInteropHelper(_overlayWindow).Handle;
            var source = PresentationSource.FromVisual(_overlayWindow);

            if (source?.CompositionTarget != null)
            {
                _dpiX = source.CompositionTarget.TransformToDevice.M11;
                _dpiY = source.CompositionTarget.TransformToDevice.M22;
            }
            else if (hwnd != IntPtr.Zero)
            {
                // 윈도우 10 이상에서 핸들로 DPI 가져오기 (fallback)
                uint dpi = NativeMethods.GetDpiForWindow(hwnd);
                _dpiX = _dpiY = dpi / 96.0;
            }

            // 방어 코드 수정: 0 이하일 때만 1.0(100%)으로 초기화
            if (_dpiX <= 0) _dpiX = 1.0;
            if (_dpiY <= 0) _dpiY = 1.0;
        }

        private void UpdatePosition()
        {
            if (_gameHwnd == IntPtr.Zero || !NativeMethods.IsWindow(_gameHwnd))
            {
                _gameHwnd = OverlayHelper.FindTalesWeaverWindow();
            }

#if DEBUG
            if (_overlayWindow.Visibility != Visibility.Visible)
                _overlayWindow.Visibility = Visibility.Visible;
#else
            if (_gameHwnd == IntPtr.Zero)
            {
                if (_overlayWindow.Visibility != Visibility.Collapsed)
                    _overlayWindow.Visibility = Visibility.Collapsed;
                return;
            }

            IntPtr foregroundHwnd = OverlayHelper.GetForegroundWindow();
            IntPtr myHwnd = new WindowInteropHelper(_overlayWindow).Handle;

            if (foregroundHwnd != _gameHwnd && foregroundHwnd != myHwnd)
            {
                if (_overlayWindow.Visibility != Visibility.Collapsed)
                    _overlayWindow.Visibility = Visibility.Collapsed;
                return;
            }
            else
            {
                if (_overlayWindow.Visibility != Visibility.Visible)
                    _overlayWindow.Visibility = Visibility.Visible;
            }
#endif

            if (_gameHwnd != IntPtr.Zero)
            {
                UpdateDpi();

                var rect = OverlayHelper.GetActualRect(_gameHwnd);

                double gameWidth = (rect.Right - rect.Left) / _dpiX;
                double gameHeight = (rect.Bottom - rect.Top) / _dpiY;
                double gameLeft = rect.Left / _dpiX;
                double gameTop = rect.Top / _dpiY;

                double marginBottom = _settings.LineMargin;
                double marginLeft = _settings.LineMarginLeft;

                double maxHorizontal = gameWidth / 2.0;
                if (Math.Abs(_settings.LineMarginLeft) > maxHorizontal)
                {
                    _settings.LineMarginLeft = _settings.LineMarginLeft > 0 ? maxHorizontal : -maxHorizontal;
                }
                if (_settings.LineMargin > gameHeight - 100) _settings.LineMargin = gameHeight - 100;

                double windowWidth = _overlayWindow.ActualWidth > 0 ? _overlayWindow.ActualWidth : _settings.WindowWidth;
                double windowHeight = _overlayWindow.ActualHeight > 0 ? _overlayWindow.ActualHeight : _settings.WindowHeight;

                double targetLeft = gameLeft + (gameWidth / 2.0) - (windowWidth / 2.0) + (marginLeft / _dpiX);
                double targetTop = gameTop + gameHeight - windowHeight - (marginBottom / _dpiY);

                if (Math.Abs(_overlayWindow.Left - targetLeft) > 0.1) _overlayWindow.Left = targetLeft;
                if (Math.Abs(_overlayWindow.Top - targetTop) > 0.1) _overlayWindow.Top = targetTop;
            }
        }

        /// <summary>
        /// 외부(MainWindow)에서 호출하여 즉시 위치를 갱신
        /// </summary>
        public void UpdatePositionImmediately()
        {
            _overlayWindow.Dispatcher.BeginInvoke(new Action(() =>
            {
                UpdatePosition();
            }), System.Windows.Threading.DispatcherPriority.Render);
        }

        private static class NativeMethods
        {
            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern uint GetDpiForWindow(IntPtr hwnd);

            [System.Runtime.InteropServices.DllImport("user32.dll")]
            [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
            public static extern bool IsWindow(IntPtr hWnd);
        }
    }
}