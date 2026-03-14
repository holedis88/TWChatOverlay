using System;
using System.Collections.Generic;
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
        /// MainWindow에서 이벤트를 연결한 후 명시적으로 호출해야 합니다.
        /// </summary>
        public void Initialize()
        {
            UpdatePath();
        }

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
                LoadInitialLogs(1000);
                var fileInfo = new FileInfo(_logPath);
                _lastPosition = fileInfo.Length;
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
                if (stream.Length == 0) return;

                long position = stream.Length - 1;
                int count = 0;
                List<byte> lineBuffer = new List<byte>();
                List<string> foundLines = new List<string>();

                while (position >= 0 && count < lineCount)
                {
                    stream.Seek(position, SeekOrigin.Begin);
                    int b = stream.ReadByte();

                    if (b == 10)
                    {
                        if (lineBuffer.Count > 0)
                        {
                            lineBuffer.Reverse();
                            string line = Encoding.GetEncoding(949).GetString(lineBuffer.ToArray());
                            foundLines.Add(line);
                            lineBuffer.Clear();
                            count++;
                        }
                    }
                    else if (b != 13)
                    {
                        lineBuffer.Add((byte)b);
                    }
                    position--;
                }

                if (lineBuffer.Count > 0 && count < lineCount)
                {
                    lineBuffer.Reverse();
                    foundLines.Add(Encoding.GetEncoding(949).GetString(lineBuffer.ToArray()));
                }

                foundLines.Reverse();

                foreach (var line in foundLines)
                {
                    OnNewLogRead?.Invoke(line.Trim());
                }
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

        #region Processing

        /// <summary>
        /// 읽어온 원문 HTML을 <br> 태그 단위로 쪼개어 이벤트를 발생
        /// </summary>
        private void ProcessRawContent(string content, int takeLastCount = -1)
        {
            /*
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
            */
            if (string.IsNullOrWhiteSpace(content)) return;

            var lines = BrTagRegex.Split(content)
                                  .Where(l => !string.IsNullOrWhiteSpace(l))
                                  .ToList();

            if (takeLastCount > 0 && lines.Count > takeLastCount)
            {
                lines = lines.Skip(lines.Count - takeLastCount).ToList();
            }

            foreach (var line in lines)
            {
                OnNewLogRead?.Invoke(line.Trim());
            }
        }

        #endregion
    }
}