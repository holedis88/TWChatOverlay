using System;
using System.Windows.Threading;
using TWChatOverlay.Models;

namespace TWChatOverlay.Services
{
    public class ExperienceService
    {
        private readonly ChatSettings _settings;
        private readonly DispatcherTimer _expTimer;

        public ExperienceService(ChatSettings settings)
        {
            _settings = settings;

            _expTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(3000) };
            _expTimer.Tick += (s, e) => _settings.RefreshExpDisplay();
        }

        public void Start() => _expTimer.Start();
        public void Stop() => _expTimer.Stop();

        /// <summary>
        /// 경험치를 추가하고 UI에 반영합니다.
        /// </summary>
        public void AddExp(long gained)
        {
            if (gained <= 0) return;
            _settings.TotalExp += gained;
        }

        /// <summary>
        /// 경험치와 시작 시간을 초기화합니다.
        /// </summary>
        public void Reset()
        {
            _settings.TotalExp = 0;
            _settings.ResetStartTime();
        }
    }
}