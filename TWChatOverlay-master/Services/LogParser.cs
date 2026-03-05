using System.Text.RegularExpressions;
using System.Windows.Media;
using TWChatOverlay.Models;

namespace TWChatOverlay
{
    /// <summary>
    /// 테일즈위버 로그(HTML)를 분석하여 데이터로 변환
    /// </summary>
    public static class LogParser
    {
        #region Data Structure

        public class ParseResult
        {
            public string FormattedText { get; set; } = "";
            public SolidColorBrush Brush { get; set; } = Brushes.White;
            public ChatCategory Category { get; set; } = ChatCategory.Unknown;
            public bool IsSuccess { get; set; } = false;
            public bool IsHighlight { get; set; } = false;
            public long GainedExp { get; set; } = 0;
        }

        #endregion

        #region Regex Optimization

        private static readonly Regex FontTagRegex = new Regex(
            @"<font[^>]*color=[""']?#?(?<color>[a-fA-F0-9]+|white)[""']?[^>]*>(?<content>.*?)</font>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex ExpRegex = new Regex(
            @"경험치가\s*(?<exp>[\d,]+)\s*올랐습니다",
            RegexOptions.Compiled);

        #endregion

        #region Methods

        /// <summary>
        /// HTML 로그 한 줄을 파싱하여 결과를 반환
        /// </summary>
        public static ParseResult ParseLine(string html, ChatSettings settings)
        {
            var result = new ParseResult();
            var fontMatches = FontTagRegex.Matches(html);

            if (fontMatches.Count >= 2)
            {
                string timeRaw = fontMatches[0].Groups["content"].Value.Trim();
                string chatColor = fontMatches[1].Groups["color"].Value.ToLower();
                string chatContent = fontMatches[1].Groups["content"].Value;

                if (chatColor == "white") chatColor = "ffffff";

                var (category, brush) = GetCategoryByColor(chatColor);
                result.Category = category;
                result.Brush = brush;
                result.FormattedText = $"{timeRaw} {chatContent}";
                result.IsSuccess = true;

                var expMatch = ExpRegex.Match(chatContent);
                if (expMatch.Success)
                {
                    string expStr = expMatch.Groups["exp"].Value.Replace(",", "");
                    if (long.TryParse(expStr, out long expValue))
                    {
                        result.GainedExp = expValue;
                    }
                }

                if (settings != null && (settings.UseAlertColor || settings.UseAlertSound))
                {
                    CheckHighlights(result, chatContent, settings);
                }
            }

            return result;
        }

        /// <summary>
        /// 현재 로그 결과가 사용자가 선택한 탭의 조건과 일치하는지 확인
        /// </summary>
        public static bool IsMatchTab(ParseResult log, string tabTag, ChatSettings settings)
        {
            return tabTag switch
            {
                "Basic" => IsVisible(log.Category, settings),
                "Team" => log.Category == ChatCategory.Team,
                "Club" => log.Category == ChatCategory.Club,
                "Shout" => log.Category == ChatCategory.Shout,
                "System" => log.Category is ChatCategory.System or ChatCategory.System2 or ChatCategory.System3,

                // "All" 탭이나 기타 탭 처리 (필요시)
                "All" => true,
                _ => false
            };
        }


        /// <summary>
        /// 색상 코드를 기반으로 채팅 카테고리와 브러시 색상을 매칭
        /// </summary>
        private static (ChatCategory category, SolidColorBrush brush) GetCategoryByColor(string colorCode)
        {
            return colorCode switch
            {
                "c8ffc8" => (ChatCategory.NormalSelf, new SolidColorBrush(Color.FromRgb(200, 255, 200))),
                "ffffff" => (ChatCategory.Normal, Brushes.White),
                "c896c8" => (ChatCategory.Shout, new SolidColorBrush(Color.FromRgb(200, 150, 200))),
                "94ddfa" => (ChatCategory.Club, new SolidColorBrush(Color.FromRgb(148, 221, 250))),
                "f7b73c" => (ChatCategory.Team, new SolidColorBrush(Color.FromRgb(247, 183, 60))),
                "ff64ff" => (ChatCategory.System, new SolidColorBrush(Color.FromRgb(255, 100, 255))),
                "00ffff" => (ChatCategory.System2, Brushes.Cyan),
                "ff6464" => (ChatCategory.System3, new SolidColorBrush(Color.FromRgb(255, 100, 100))),
                _ => (ChatCategory.Unknown, Brushes.White)
            };
        }

        /// <summary>
        /// 채팅 내용에 키워드가 포함되어 있는지 확인하여 알림 여부를 결정
        /// </summary>
        private static void CheckHighlights(ParseResult result, string content, ChatSettings settings)
        {
            var keywords = settings.ParsedKeywords;
            if (keywords == null || keywords.Count == 0) return;

            foreach (var kw in keywords)
            {
                if (content.Contains(kw))
                {
                    result.IsHighlight = true;
                    break;
                }
            }
        }

        /// <summary>
        /// 설정값에 따라 해당 카테고리가 현재 활성화 상태인지 확인
        /// </summary>
        public static bool IsVisible(ChatCategory category, ChatSettings s)
        {
            return category switch
            {
                ChatCategory.NormalSelf or ChatCategory.Normal => s.ShowNormal,
                ChatCategory.Shout => s.ShowShout,
                ChatCategory.Club => s.ShowClub,
                ChatCategory.Team => s.ShowTeam,
                ChatCategory.System or ChatCategory.System2 or ChatCategory.System3 => s.ShowSystem,
                _ => true
            };
        }

        #endregion
    }
}