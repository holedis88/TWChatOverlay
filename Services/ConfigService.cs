using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using TWChatOverlay.Models;

namespace TWChatOverlay.Services
{
    /// <summary>
    /// 프로그램 설정을 JSON 파일로 저장하고 불러오는 기능
    /// </summary>
    public static class ConfigService
    {
        private static readonly string FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };

        /// <summary>
        /// 설정 객체를 파일로 저장
        /// </summary>
        public static void Save(ChatSettings settings)
        {
            if (settings == null) return;

            try
            {
                string json = JsonSerializer.Serialize(settings, _options);
                File.WriteAllText(FilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"설정 저장 중 오류 발생: {ex.Message}");
            }
        }

        /// <summary>
        /// 파일로부터 설정을 불러옴. 파일이 없거나 오류 발생 시 기본 설정을 반환
        /// </summary>
        public static ChatSettings Load()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    string json = File.ReadAllText(FilePath);
                    return JsonSerializer.Deserialize<ChatSettings>(json, _options) ?? new ChatSettings();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"설정 로드 중 오류 발생: {ex.Message}");
            }
            return new ChatSettings();
        }
    }
}