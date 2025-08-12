using GorevTakipUygulamasi.Models;
using Microsoft.Extensions.Options;
using GorevTakipUygulamasi.Configuration;
using System.Text;
using System.Text.Json;

namespace GorevTakipUygulamasi.Services.LogicApp
{
    public class LogicAppService : ILogicAppService
    {
        private readonly HttpClient _httpClient;
        private readonly LogicAppSettings _settings;
        private readonly ILogger<LogicAppService> _logger;

        public LogicAppService(
            HttpClient httpClient,
            IOptions<LogicAppSettings> settings,
            ILogger<LogicAppService> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task<bool> CancelReminderAsync(Guid reminderId)
        {
            try
            {
                var cancelRequest = new { ReminderId = reminderId.ToString() };
                var json = JsonSerializer.Serialize(cancelRequest, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_settings.CancelReminderUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Hatırlatıcı iptali Logic App'e gönderildi: {ReminderId}", reminderId);
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Logic App iptal hatası: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logic App'e hatırlatıcı iptali gönderilirken hata: {ReminderId}", reminderId);
                return false;
            }
        }

        public async Task<bool> ScheduleReminderAsync(ReminderItem reminder)
        {
            try
            {
                var notification = new ReminderNotificationDto
                {
                    ReminderId = reminder.Id.ToString(),
                    UserId = reminder.UserId,
                    UserEmail = await GetUserEmailAsync(reminder.UserId),
                    Title = reminder.Title,
                    Description = reminder.Description,
                    ScheduledDateTime = reminder.Date.ToDateTime(reminder.Time),
                    NotificationType = "Email"
                };

                var json = JsonSerializer.Serialize(notification, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_settings.ScheduleReminderUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Hatırlatıcı Logic App'e gönderildi: {ReminderId}", reminder.Id);
                    return true;
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Logic App hatası: {StatusCode} - {Error}", response.StatusCode, errorContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logic App'e hatırlatıcı gönderilirken hata: {ReminderId}", reminder.Id);
                return false;
            }
        }

        public async Task<bool> SendImmediateEmailAsync(ReminderNotificationDto notification)
        {
            try
            {
                var json = JsonSerializer.Serialize(notification, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_settings.SendEmailUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Anlık email Logic App'e gönderildi: {ReminderId}", notification.ReminderId);
                    return true;
                }
                else
                {
                    _logger.LogError("Logic App email hatası: {StatusCode}", response.StatusCode);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logic App'e anlık email gönderilirken hata: {ReminderId}", notification.ReminderId);
                return false;
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var testData = new { Test = "Connection", Timestamp = DateTime.UtcNow };
                var json = JsonSerializer.Serialize(testData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var response = await _httpClient.PostAsync(_settings.TestConnectionUrl, content);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logic App bağlantı testi hatası");
                return false;
            }
        }

        private async Task<string> GetUserEmailAsync(string userId)
        {
            // Bu metod user service'den email alacak
            // Şimdilik dummy değer dönüyoruz
            await Task.CompletedTask; // Async warning'i gidermek için
            return "user@example.com";
        }
    }
}