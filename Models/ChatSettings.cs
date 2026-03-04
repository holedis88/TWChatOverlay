using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Windows.Media;

namespace TWChatOverlay.Models
{
    /// <summary>
    /// 애플리케이션의 설정 클래스
    /// </summary>
    public class ChatSettings : INotifyPropertyChanged
    {
        #region Fields

        private string _normalColor = "#FFFFFF";
        private string _teamColor = "#00BFFF";
        private string _clubColor = "#00FF00";
        private string _systemColor = "#BA55D3";
        private string _shoutColor = "#FF64FF";
        private string _keywordInput = "";
        private string _fontFamily = "나눔고딕";

        private bool _useAlertColor = true;
        private bool _useAlertSound = true;
        private bool _showExpTracker = false;

        private double _fontSize = 15.0;
        private double _lineMargin = 25.0;

        private long _totalExp = 0;

        private List<string> _parsedKeywords = new();
        private DateTime _startTime = DateTime.Now;
        #endregion

        #region Properties

        public bool ShowNormal { get; set; } = true;
        public bool ShowShout { get; set; } = true;
        public bool ShowTeam { get; set; } = true;
        public bool ShowWhisper { get; set; } = true;
        public bool ShowSystem { get; set; } = true;
        public bool ShowClub { get; set; } = true;
        public bool UseKeywordAlert { get; set; } = true;

        public List<string> AvailableFonts { get; } = new() { "나눔고딕", "굴림", "사용자 설정" };
        public double WindowWidth { get; set; } = 538.0;
        public double WindowHeight { get; set; } = 200.0;
        public double FontSize { get => _fontSize; set { _fontSize = value; OnPropertyChanged(); } }
        public double LineMargin { get => _lineMargin; set { _lineMargin = value; OnPropertyChanged(); } }
        public string FontFamily { get => _fontFamily; set { _fontFamily = value; OnPropertyChanged(); } }
        public string NormalColor { get => _normalColor; set { _normalColor = value; OnPropertyChanged(); } }
        public string TeamColor { get => _teamColor; set { _teamColor = value; OnPropertyChanged(); } }
        public string ClubColor { get => _clubColor; set { _clubColor = value; OnPropertyChanged(); } }
        public string SystemColor { get => _systemColor; set { _systemColor = value; OnPropertyChanged(); } }
        public string ShoutColor { get => _shoutColor; set { _shoutColor = value; OnPropertyChanged(); } }
        public bool UseAlertColor { get => _useAlertColor; set { _useAlertColor = value; OnPropertyChanged(); } }
        public bool UseAlertSound { get => _useAlertSound; set { _useAlertSound = value; OnPropertyChanged(); } }
        public bool ShowExpTracker { get => _showExpTracker; set { _showExpTracker = value; OnPropertyChanged(); } }
        public string KeywordInput
        {
            get => _keywordInput;
            set
            {
                _keywordInput = value;
                OnPropertyChanged();
                UpdateKeywords();
            }
        }

        [JsonIgnore]
        public List<string> ParsedKeywords => _parsedKeywords;


        [JsonIgnore]
        public long TotalExp
        {
            get => _totalExp;
            set
            {
                if (_totalExp == value) return;
                _totalExp = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TotalExpDisplay));
            }
        }

        [JsonIgnore]
        public string TotalExpDisplay
        {
            get
            {
                string currentExp = FormatExp(_totalExp);
                TimeSpan elapsed = DateTime.Now - _startTime;
                double hours = elapsed.TotalHours;

                if (hours < 0.0027 || _totalExp == 0) return $"{currentExp} | 0.0/h";

                long expPerHour = (long)(_totalExp / hours);
                return $"{currentExp} | {FormatExp(expPerHour)}/h";
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// 세션 시작 시간을 현재로 초기화
        /// </summary>
        public void ResetStartTime() => _startTime = DateTime.Now;

        /// <summary>
        /// 입력된 키워드 문자열을 파싱하여 리스트화
        /// </summary>
        private void UpdateKeywords()
        {
            _parsedKeywords = _keywordInput.Split(new[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                           .Where(k => k.StartsWith("@"))
                                           .ToList();
        }

        /// <summary>
        /// 경험치 숫자를 한국어 문자열로 변환
        /// </summary>
        private string FormatExp(long value)
        {
            if (value >= 1_000_000_000_000)
            {
                long jo = value / 1_000_000_000_000;
                double eok = (value % 1_000_000_000_000) / 100_000_000.0;
                return $"{jo}조 {Math.Floor(eok * 10) / 10.0:F1}억";
            }
            if (value >= 100_000_000)
            {
                double eok = value / 100_000_000.0;
                return $"{Math.Floor(eok * 10) / 10.0:F1}억";
            }
            return value.ToString("N0");
        }

        /// <summary>
        /// 모든 설정을 기본값으로 되돌림
        /// </summary>
        public void ResetToDefault()
        {
            FontFamily = AvailableFonts.FirstOrDefault() ?? "나눔고딕";
            FontSize = 15.0;
            LineMargin = 25.0;
            WindowWidth = 538.0;
            WindowHeight = 200.0;

            NormalColor = "#FFFFFF";
            TeamColor = "#00BFFF";
            ClubColor = "#00FF00";
            ShoutColor = "#BA55D3";
            SystemColor = "#FF64FF";

            ShowNormal = ShowShout = ShowTeam = ShowWhisper = ShowSystem = ShowClub = true;

            UseAlertColor = UseAlertSound = false;
            KeywordInput = "";

            ShowExpTracker = false;
            TotalExp = 0;
            ResetStartTime();
        }

        /// <summary>
        /// 특정 태그에 해당하는 카테고리의 색상을 업데이트
        /// </summary>
        public void UpdateColor(string tag, string hex)
        {
            switch (tag)
            {
                case "Normal": NormalColor = hex; break;
                case "System": SystemColor = hex; break;
                case "Team": TeamColor = hex; break;
                case "Club": ClubColor = hex; break;
                case "Shout": ShoutColor = hex; break;
            }
        }

        /// <summary>
        /// 카테고리 별 색상 변경
        /// </summary>
        public SolidColorBrush GetBrush(ChatCategory category)
        {
            string hex = category switch
            {
                ChatCategory.System or ChatCategory.System2 or ChatCategory.System3 => SystemColor,
                ChatCategory.Team => TeamColor,
                ChatCategory.Club => ClubColor,
                ChatCategory.Shout => ShoutColor,
                _ => NormalColor
            };

            try
            {
                return (SolidColorBrush)new BrushConverter().ConvertFromString(hex) ?? Brushes.White;
            }
            catch
            {
                return Brushes.White;
            }
        }

        /// <summary>
        /// 외부에서 강제로 경험치 텍스트를 갱신할 때 호출
        /// </summary>
        public void RefreshExpDisplay() => OnPropertyChanged(nameof(TotalExpDisplay));

        #endregion

        #region Interface

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        #endregion
    }
}