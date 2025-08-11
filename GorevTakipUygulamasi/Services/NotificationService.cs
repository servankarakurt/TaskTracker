using System.Text;
using System.Text.Json;

namespace GorevTakipUygulamasi.Services
{
    public class NotificationService
    {
        private readonly HttpClient _httpClient;
        private readonly string _logicAppUrl;

        public NotificationService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logicAppUrl = configuration["LogicApp:NotificationUrl"];
        }

        public async Task SendTaskCompletionNotificationAsync(string taskTitle, string taskDescription, string userEmail)
        {
            try
            {
                // ✅ DEBUG: Gelen verileri kontrol et
                Console.WriteLine($"📧 Debug - taskTitle: '{taskTitle}'");
                Console.WriteLine($"📧 Debug - taskDescription: '{taskDescription}'");
                Console.WriteLine($"📧 Debug - userEmail: '{userEmail}'");
                Console.WriteLine($"📧 Debug - userEmail boş mu: {string.IsNullOrWhiteSpace(userEmail)}");
                Console.WriteLine($"📧 Debug - Logic App URL: '{_logicAppUrl}'");

                // ✅ Boş email kontrolü ekle
                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    Console.WriteLine("⚠️ HATA: userEmail boş veya null!");
                    return;
                }

                // ✅ Logic App URL kontrolü
                if (string.IsNullOrWhiteSpace(_logicAppUrl))
                {
                    Console.WriteLine("⚠️ HATA: Logic App URL boş!");
                    return;
                }

                var notificationData = new
                {
                    taskTitle = taskTitle ?? "Başlıksız Görev",
                    taskDescription = taskDescription ?? "",
                    userEmail = userEmail,
                    userName = "Kullanıcı", // Geçici olarak sabit değer
                    completedDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm"),
                    taskId = 0 // Geçici olarak 0
                };

                // ✅ JSON'u da kontrol et
                var json = JsonSerializer.Serialize(notificationData, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                Console.WriteLine($"📤 Gönderilecek JSON: {json}");

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                Console.WriteLine("📡 HTTP isteği gönderiliyor...");
                var response = await _httpClient.PostAsync(_logicAppUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("✅ Bildirim başarıyla gönderildi.");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"❌ Bildirim hatası: {response.StatusCode}");
                    Console.WriteLine($"❌ Hata detayı: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"💥 Bildirim servisi hatası: {ex.Message}");
                Console.WriteLine($"💥 Stack Trace: {ex.StackTrace}");
            }
        }
    }
}