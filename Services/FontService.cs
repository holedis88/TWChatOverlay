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
                        return new FontFamily("Malgun Gothic");
                    }
                }
            }
            else if (!string.IsNullOrEmpty(fontFamilyName))
            {
                return new FontFamily(fontFamilyName);
            }

            return new FontFamily("Malgun Gothic");
        }
    }
}