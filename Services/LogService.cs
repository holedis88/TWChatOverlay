using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Threading;

namespace TWChatOverlay.Services
{
    /// <summary>
    /// 테일즈위버 로그(HTML)를 실시간으로 감시하고 읽어오는 서비스
    /// </summary>
    public class LogService
    {
        #region Fields & Properties

        private string _logPath = null!;
        private long _lastPosition = 0;
        private readonly object _lockObj = new object();
        private readonly DispatcherTimer _pollingTimer;

        public event Action<string>? OnNewLogRead;
        private static readonly Regex BrTagRegex = new Regex(@"</?br\s*>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        #endregion

        #region Constructor & Lifecycle

        public LogService()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            UpdatePath();
            _pollingTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _pollingTimer.Tick += (s, e) =>
            {
                CheckDateAndPath();
                ReadLog();
            };
        }

        public void Start() => _pollingTimer.Start();
        public void Stop() => _pollingTimer.Stop();

        #endregion

        #region Path Management

        /// <summary>
        /// 날짜가 변경되었는지 확인하고 필요시 경로를 업데이트
        /// </summary>
        private void CheckDateAndPath()
        {
            string today = DateTime.Now.ToString("yyyy_MM_dd");
            string expectedPath = $@"C:\Nexon\TalesWeaver\ChatLog\TWChatLog_{today}.html";

            if (_logPath != expectedPath)
            {
                UpdatePath();
            }
        }

        /// <summary>
        /// 현재 날짜에 맞는 로그 경로를 설정하고 초기 위치를 지정
        /// </summary>
        private void UpdatePath()
        {
            string today = DateTime.Now.ToString("yyyy_MM_dd");
            _logPath = $@"C:\Nexon\TalesWeaver\ChatLog\TWChatLog_{today}.html";

            if (File.Exists(_logPath))
            {
                LoadInitialLogs(300);
                _lastPosition = new FileInfo(_logPath).Length;
            }
            else
            {
                _lastPosition = 0;
            }
        }

        #endregion

        #region Method

        /// <summary>
        /// 초기 구동 시 기존 로그의 마지막 부분을 가져옴
        /// </summary>
        private void LoadInitialLogs(int lineCount)
        {
            try
            {
                using var stream = new FileStream(_logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream, Encoding.GetEncoding(949));

                string allContent = reader.ReadToEnd();
                ProcessRawContent(allContent, lineCount);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"초기 로그 로드 실패: {ex.Message}");
            }
        }

        /// <summary>
        /// 실시간으로 추가된 로그만 Seek를 이용해 빠르게 읽음
        /// </summary>
        public void ReadLog()
        {
            lock (_lockObj)
            {
                try
                {
                    if (!File.Exists(_logPath)) return;

                    using var stream = new FileStream(_logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    if (stream.Length < _lastPosition) _lastPosition = 0;
                    if (stream.Length <= _lastPosition) return;
                    stream.Seek(_lastPosition, SeekOrigin.Begin);
                    using var reader = new StreamReader(stream, Encoding.GetEncoding(949));
                    string newContent = reader.ReadToEnd();
                    _lastPosition = stream.Position;

                    ProcessRawContent(newContent);
                }
                catch (IOException) { }
            }
        }

        #endregion

        #region 5. 데이터 처리 (Content Processing)

        /// <summary>
        /// 읽어온 원문 HTML을 <br> 태그 단위로 쪼개어 이벤트를 발생
        /// </summary>
        private void ProcessRawContent(string content, int takeLastCount = -1)
        {
            if (string.IsNullOrWhiteSpace(content)) return;

            string[] lines = BrTagRegex.Split(content);
            var validLines = lines.Where(l => !string.IsNullOrWhiteSpace(l));

            if (takeLastCount > 0)
            {
                validLines = validLines.Reverse().Take(takeLastCount).Reverse();
            }

            foreach (var line in validLines)
            {
                OnNewLogRead?.Invoke(line.Trim());
            }
        }

        #endregion
    }
}