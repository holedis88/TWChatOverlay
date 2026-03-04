using System;
using System.IO;
using System.Linq;
using System.Windows.Media;

namespace TWChatOverlay.Services
{
    public static class FontService
    {
        private static readonly string FontDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Font");
        private static readonly string UserFontPath = Path.Combine(FontDirectory, "UserDefine.ttf");

        /// <summary>
        /// 설정된 폰트 이름에 따라 적절한 FontFamily 객체를 반환합니다.
        /// </summary>
        public static FontFamily GetFont(string fontFamilyName)
        {
            // 1. 사용자 설정 폰트인 경우
            if (fontFamilyName == "사용자 설정")
            {
                if (File.Exists(UserFontPath))
                {
                    try
                    {
                        var fontFamilies = Fonts.GetFontFamilies(new Uri(UserFontPath));
                        if (fontFamilies.Count > 0)
                        {
                            return fontFamilies.First();
                        }
                    }
                    catch
                    {
                        // 폰트 파일이 깨졌거나 읽을 수 없는 경우 기본값으로 이동
                    }
                }
            }
            // 2. 시스템 폰트 이름이 지정된 경우
            else if (!string.IsNullOrEmpty(fontFamilyName))
            {
                return new FontFamily(fontFamilyName);
            }

            // 3. 기본값 반환 (맑은 고딕)
            return new FontFamily("Malgun Gothic");
        }
    }
}