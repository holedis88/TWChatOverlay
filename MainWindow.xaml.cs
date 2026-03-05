using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using TWChatOverlay.Models;
using TWChatOverlay.Services;
using WinColor = System.Drawing.Color;
using WinForms = System.Windows.Forms;

namespace TWChatOverlay
{
    public partial class MainWindow : Window
    {
        #region Fields & Properties

        // 주요 서비스 객체들
        private ExperienceService _expService;
        private HotKeyService _hotKeyService;
        private WindowStickyService _stickyService;
        private ChatSettings _settings;
        private LogService _logService;

        private readonly List<LogParser.ParseResult> _allParsedLogs = new();
        private string _currentTabTag = "All";

        public static readonly DependencyProperty CurrentFontProperty =
            DependencyProperty.Register("CurrentFont", typeof(FontFamily), typeof(MainWindow));

        public FontFamily CurrentFont
        {
            get => (FontFamily)GetValue(CurrentFontProperty);
            set => SetValue(CurrentFontProperty, value);
        }

        #endregion

        #region Initialization

        public MainWindow()
        {
            InitializeComponent();

            _settings = ConfigService.Load();
            this.DataContext = _settings;

            _expService = new ExperienceService(_settings);
            _logService = new LogService();
            _logService.OnNewLogRead += (html) => Dispatcher.Invoke(() => AppendNewLogs(html));

            ApplyInitialSettings();

            Dispatcher.BeginInvoke(new Action(() =>
            {
                InitializeNativeServices();
            }), DispatcherPriority.Loaded);
        }

        /// <summary>
        /// 윈도우 핸들이나 하드웨어 가속이 필요한 네이티브 서비스들을 초기화
        /// </summary>
        private void InitializeNativeServices()
        {
            try
            {
                IntPtr handle = new WindowInteropHelper(this).EnsureHandle();

                _hotKeyService = new HotKeyService(handle);
                _hotKeyService.HotKeyPressed += (id) => { if (id == HotKeyService.EXIT_HOTKEY_ID) ConfirmExit(); };
                _hotKeyService.Register(HotKeyService.EXIT_HOTKEY_ID, HotKeyService.MOD_ALT, HotKeyService.VK_F1);

                _stickyService = new WindowStickyService(this, _settings);
                _stickyService.Start();
                _expService.Start();
                _logService.Start();

                _settings.PropertyChanged += OnSettingsPropertyChanged;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"서비스 시작 중 오류 발생: {ex.Message}");
            }
        }

        #endregion

        #region Processing

        /// <summary>
        /// 서비스로부터 읽어온 새 로그 HTML을 파싱하고 UI에 표시 여부를 결정
        /// </summary>
        private void AppendNewLogs(string html)
        {
            if (string.IsNullOrWhiteSpace(html)) return;

            // 1) 로그 파싱
            var parseResult = LogParser.ParseLine(html, _settings);
            if (!parseResult.IsSuccess) return;

            if (parseResult.GainedExp > 0) _expService.AddExp(parseResult.GainedExp);

            if (!parseResult.IsHighlight && _settings.UseAlertColor)
                parseResult.Brush = _settings.GetBrush(parseResult.Category);

            _allParsedLogs.Add(parseResult);
            if (_allParsedLogs.Count > 400) _allParsedLogs.RemoveAt(0);

            if (parseResult.IsHighlight || LogParser.IsMatchTab(parseResult, _currentTabTag, _settings))
            {
                AddToUI(parseResult);
            }
        }

        /// <summary>
        /// 파싱된 로그 데이터를 출력
        /// </summary>
        private void AddToUI(LogParser.ParseResult log)
        {
            if (LogDisplay == null) return;

            Paragraph p = new Paragraph(new Run(log.FormattedText))
            {
                Foreground = log.Brush,
                FontSize = _settings.FontSize,
                FontFamily = this.CurrentFont,
                Margin = new Thickness(0, 0, 0, 1),
                LineHeight = 1
            };

            if (log.IsHighlight)
            {
                if (_settings.UseAlertColor)
                {
                    p.Background = new SolidColorBrush(Color.FromArgb(80, 255, 165, 0));
                    p.FontWeight = FontWeights.Bold;
                }
                if (_settings.UseAlertSound) System.Media.SystemSounds.Asterisk.Play();
            }

            var blocks = LogDisplay.Document.Blocks;
            blocks.Add(p);

            if (blocks.Count > 200) blocks.Remove(blocks.FirstBlock);
            LogDisplay.ScrollToEnd();
        }

        /// <summary>
        /// 탭 변경이나 설정 변경 시 기존 로그를 모두 지우고 현재 조건에 맞는 로그로 갱신
        /// </summary>
        private void RefreshLogDisplay()
        {
            if (LogDisplay == null) return;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                LogDisplay.BeginChange();
                try
                {
                    LogDisplay.Document.Blocks.Clear();

                    var filteredLogs = _allParsedLogs
                        .Where(log => LogParser.IsMatchTab(log, _currentTabTag, _settings))
                        .TakeLast(200);

                    foreach (var log in filteredLogs)
                    {
                        AddToUI(log);
                    }
                }
                finally
                {
                    LogDisplay.EndChange();
                    LogDisplay.ScrollToEnd();
                }
            }), DispatcherPriority.Background);
        }

        #endregion

        #region Settings

        /// <summary>
        /// 설정 데이터 모델의 값이 변경될 때 UI를 갱신
        /// </summary>
        private void OnSettingsPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (e.PropertyName == nameof(_settings.FontFamily) || e.PropertyName == nameof(_settings.FontSize))
                {
                    ApplyInitialSettings();
                    RefreshLogDisplay();
                }
                else if (e.PropertyName != null && e.PropertyName.StartsWith("Show"))
                {
                    RefreshLogDisplay();
                }

                ConfigService.Save(_settings);
            });
        }

        /// <summary>
        /// 프로그램 시작 및 설정 변경 시 기본적인 UI 속성변경
        /// </summary>
        private void ApplyInitialSettings()
        {
            if (_settings == null || MainBorder == null) return;

            this.Width = _settings.WindowWidth;
            this.Height = _settings.WindowHeight;

            FontFamily nextFont = FontService.GetFont(_settings.FontFamily);
            this.CurrentFont = nextFont;

            if (LogDisplay != null)
            {
                LogDisplay.FontFamily = nextFont;
                LogDisplay.FontSize = _settings.FontSize;
            }
            if (SettingsDisplay != null) SettingsDisplay.FontFamily = nextFont;

            MainBorder.Background = new SolidColorBrush(Color.FromArgb(255, 30, 30, 30));
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// 상단 라디오 버튼 클릭 시 텍스트박스를 제어하고 로그를 새로고침
        /// </summary>
        private void Tab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not RadioButton btn || btn.Tag == null) return;

            _currentTabTag = btn.Tag.ToString();

            LogDisplay.Visibility = (_currentTabTag is "Settings" or "Addon") ? Visibility.Collapsed : Visibility.Visible;
            SettingsDisplay.Visibility = (_currentTabTag == "Settings") ? Visibility.Visible : Visibility.Collapsed;
            AddonDisplay.Visibility = (_currentTabTag == "Addon") ? Visibility.Visible : Visibility.Collapsed;

            if (LogDisplay.Visibility == Visibility.Visible) RefreshLogDisplay();
        }

        /// <summary>
        /// 설정에서 색상 선택 버튼 클릭 시 윈도우 컬러 다이얼로그 생성
        /// </summary>
        private void ColorPick_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button btn) return;

            using var dialog = new WinForms.ColorDialog();
            if (btn.Background is SolidColorBrush currentBrush)
            {
                var c = currentBrush.Color;
                dialog.Color = WinColor.FromArgb(c.A, c.R, c.G, c.B);
            }

            if (dialog.ShowDialog() == WinForms.DialogResult.OK)
            {
                string hex = $"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}";
                _settings.UpdateColor(btn.Tag?.ToString() ?? "", hex);

                foreach (var log in _allParsedLogs)
                {
                    log.Brush = _settings.GetBrush(log.Category);
                }

                SettingsDisplay.DataContext = null;
                SettingsDisplay.DataContext = _settings;
                RefreshLogDisplay();
            }
        }

        private void ResetExp_Click(object sender, RoutedEventArgs e) => _expService.Reset();

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            ConfigService.Save(_settings);
            MessageBox.Show("설정이 파일에 저장되었습니다.");
        }

        private void InitSettings_Click(object sender, RoutedEventArgs e)
        {
            if (_settings == null) return;
            _settings.ResetToDefault();
            SettingsDisplay.DataContext = null;
            SettingsDisplay.DataContext = _settings;
            ApplyInitialSettings();
            MessageBox.Show("설정이 초기화되었습니다.");
        }

        private void KeywordBox_TextChanged(object sender, TextChangedEventArgs e) => ConfigService.Save(_settings);

        private void Slider_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ConfigService.Save(_settings);
            ApplyInitialSettings();
        }

        #endregion

        #region Resizing

        private void TopResize_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double newHeight = this.Height - e.VerticalChange;
            if (newHeight > this.MinHeight)
            {
                this.Top += e.VerticalChange;
                this.Height = newHeight;
                _settings.WindowHeight = newHeight;
            }
        }

        private void LeftResize_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double newWidth = this.Width - e.HorizontalChange;
            if (newWidth > this.MinWidth)
            {
                this.Left += e.HorizontalChange;
                this.Width = newWidth;
                _settings.WindowWidth = newWidth;
            }
        }

        private void RightResize_DragDelta(object sender, DragDeltaEventArgs e)
        {
            double newWidth = this.Width + e.HorizontalChange;
            if (newWidth > this.MinWidth)
            {
                this.Width = newWidth;
                _settings.WindowWidth = newWidth;
            }
        }

        #endregion

        #region Exit
        private void ConfirmExit()
        {
            var result = WinForms.MessageBox.Show(
                "프로그램을 종료하시겠습니까?", "종료 확인",
                WinForms.MessageBoxButtons.YesNo, WinForms.MessageBoxIcon.Question,
                WinForms.MessageBoxDefaultButton.Button2, WinForms.MessageBoxOptions.ServiceNotification);

            if (result == WinForms.DialogResult.Yes) ExitApp_Click(null, null);
        }

        private void ExitApp_Click(object? sender, RoutedEventArgs? e)
        {
            if (_settings != null) ConfigService.Save(_settings);

            _logService?.Stop();
            _expService?.Stop();
            _stickyService?.Stop();

            Application.Current.Shutdown();
        }

        protected override void OnClosed(EventArgs e)
        {
            _hotKeyService?.Unregister(HotKeyService.EXIT_HOTKEY_ID);
            _hotKeyService?.Dispose();
            base.OnClosed(e);
        }

        #endregion
    }
}